using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NPOI.POIFS.Properties;
using Quick.EntityFrameworkCore.Plus.Utils;
using Quick.Fields;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace Quick.EntityFrameworkCore.Plus
{
    public abstract class AbstractDbContextConfigHandler : IDbContextConfigHandler
    {
        public const string Quick_EntityFrameworkCore_Plus_AbstractDbContextConfigHandler_IsReadOnly = nameof(Quick_EntityFrameworkCore_Plus_AbstractDbContextConfigHandler_IsReadOnly);
        private const string BTN_TEST = nameof(BTN_TEST);
        private const string BTN_INIT = nameof(BTN_INIT);
        private const string BTN_BACKUP_D3B = nameof(BTN_BACKUP_D3B);
        private const string BTN_BACKUP_XLSX = nameof(BTN_BACKUP_XLSX);
        private const string BTN_RESTORE = nameof(BTN_RESTORE);
        private const string BTN_DELETE = nameof(BTN_DELETE);

        private const string EXECUTE_INIT = nameof(EXECUTE_INIT);
        private const string EXECUTE_RESTORE = nameof(EXECUTE_RESTORE);
        private const string EXECUTE_DELETE = nameof(EXECUTE_DELETE);

        public abstract string Name { get; }
        /// <summary>
        /// 命令超时时间（单位：秒）
        /// </summary>
        public int CommandTimeout { get; set; } = 60;
        public static string BackupDir = "Backup";
        public static string BackupFilePrefix = "数据库备份";
        private UnitStringConverting storageUSC = UnitStringConverting.StorageUnitStringConverting;

        public virtual DbContext CreateDbContextInstance(Type dbContextType)
        {
            return (DbContext)Activator.CreateInstance(dbContextType, this);
        }

        public virtual void DatabaseEnsureCreated() { }

        protected abstract string[] GetTableColumns(DbConnection dbConnection, string tableName);

        public virtual void DatabaseEnsureCreatedAndUpdated(Func<DbContext> getDbContextFunc, Action<string> logger = null)
        {
            //确保数据库已创建
            DatabaseEnsureCreated();

            var columnChangedEntityTypeList = new List<IEntityType>();
            using (var dbContext = getDbContextFunc())
            {
                //如果是表结构第一次创建，则创建成功后直接返回
                if (dbContext.Database.EnsureCreated())
                    return;

                var dbConnection = dbContext.Database.GetDbConnection();
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();
                foreach (var entityType in dbContext.Model.GetEntityTypes().ToArray())
                {
                    var tableName = entityType.GetTableName();
                    var tableDefColumns = entityType.GetProperties().Select(t => t.GetColumnName()).ToArray();
                    var tableCurrentColumns = GetTableColumns(dbConnection, tableName);
                    var tableDefColumnsString = string.Join(",", tableDefColumns.OrderBy(t => t));
                    var tableCurrentColumnsString = string.Join(",", tableCurrentColumns.OrderBy(t => t));
                    if (tableDefColumnsString != tableCurrentColumnsString)
                    {
                        logger?.Invoke($"发现表[{tableName}]的结构不匹配，定义列：[{string.Join(",", tableDefColumns)}]，当前列：[{string.Join(",", tableCurrentColumns)}]。");
                        columnChangedEntityTypeList.Add(entityType);
                    }
                }
            }
            ;
            //如果存在字段修改过的实体类型
            if (columnChangedEntityTypeList.Count > 0)
            {
                logger?.Invoke($"即将自动更新表结构。。。");
                var dbContextBackup = new DbContextBackup.D3b.D3bDbContextBackupContext();
                var backupFile = $"DbBackup_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.d3b";
                //备份
                using (var dbContext = getDbContextFunc())
                    dbContextBackup.Backup(dbContext, backupFile, TableNameProcess);
                //删除表结构
                DatabaseEnsureDeleted(getDbContextFunc);
                //创建表结构
                DatabaseEnsureCreated();

                using (var dbContext = getDbContextFunc())
                {
                    dbContext.Database.EnsureCreated();
                    dbContextBackup.Restore(dbContext, backupFile);
                }
                logger?.Invoke($"表结构更新完成。数据库备份文件：{backupFile}");
            }
        }

        public virtual void DatabaseEnsureDeleted(Func<DbContext> getDbContextFunc)
        {
            using (var dbContext = getDbContextFunc())
                dbContext.Database.EnsureDeleted();
        }

        public virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

        protected abstract IDbContextConfigHandler GetTestDbContextConfigHandler();

        public virtual void Test()
        {
            using (var dbContext = new TestDbContext(GetTestDbContextConfigHandler()))
                dbContext.Test();
        }

        public virtual void Validate() { }

        public virtual string TableNameProcess(string tableName) => tableName;

        protected FieldForGet getCommonGroup(FieldsForPostContainer request, bool isReadOnly, params FieldForGet[] otherFields)
        {
            var list = new List<FieldForGet>();
            if (request != null)
            {
                //测试
                if (request.IsFieldIdsMatch(BTN_TEST))
                {
                    try
                    {
                        Test();
                        list.Add(new FieldForGet()
                        {
                            Name = "成功",
                            Description = $"测试连接成功！",
                            Type = FieldType.MessageBox
                        });
                    }
                    catch (Exception ex)
                    {
                        list.Add(new FieldForGet()
                        {
                            Name = "错误",
                            Description = $"测试连接时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                            Type = FieldType.MessageBox
                        });
                    }
                }
                //按钮_初始化
                else if (request.IsFieldIdsMatch(BTN_INIT))
                {
                    list.Add(new()
                    {
                        Type = FieldType.ContainerRow,
                        Children = [
                            new(){
                            Id = EXECUTE_INIT,
                            Name = "初始化",
                            Description = $"初始化数据库会删除之前的表，再创建新的表结构，最后填充初始化数据，确认要继续?",
                            Type = FieldType.MessageBox,
                            PostOnChanged = true,
                            MessageBox_CanCancel =true
                        }]
                    });
                }
                //执行_初始化
                else if (request.IsFieldIdsMatch(EXECUTE_INIT))
                {
                    var messageBoxResult = request.GetFieldValue(request.FieldIds);
                    if (messageBoxResult == FieldForGet.MESSAGEBOX_VALUE_OK)
                    {
                        try
                        {
                            var DbContextType = typeof(ConfigDbContext);
                            DatabaseEnsureDeleted(() => CreateDbContextInstance(DbContextType));
                            DatabaseEnsureCreatedAndUpdated(() => CreateDbContextInstance(DbContextType));

                            using (var dbContext = CreateDbContextInstance(DbContextType))
                            {
                                var entityTypes = dbContext.Model.GetEntityTypes().ToArray();
                                var i = 1;
                                foreach (var entityType in entityTypes)
                                {
                                    i++;
                                    var modelType = entityType.ClrType;
                                    if (typeof(IHasInitDataModel).IsAssignableFrom(modelType))
                                    {
                                        var model = (IHasInitDataModel)Activator.CreateInstance(modelType);
                                        var items = model.GetInitData();
                                        dbContext.AddRange(items);
                                    }
                                }
                                //保存修改
                                dbContext.SaveChanges();
                            }
                            list.Add(new FieldForGet()
                            {
                                Name = "成功",
                                Description = "初始化成功！",
                                Type = FieldType.MessageBox,
                                MessageBox_CanCancel = false,
                                PostOnChanged = true
                            });
                        }
                        catch (Exception ex)
                        {
                            list.Add(new FieldForGet()
                            {
                                Name = "错误",
                                Description = $"初始化时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                                Type = FieldType.MessageBox,
                                MessageBox_CanCancel = false,
                                PostOnChanged = true
                            });
                        }
                    }
                }
                //备份为d3b文件
                else if (request.IsFieldIdsMatch(BTN_BACKUP_D3B))
                {
                    try
                    {
                        var backupFile = $"{BackupFilePrefix}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.d3b";
                        var dbContextBackupContext = new DbContextBackup.D3b.D3bDbContextBackupContext();
                        dbContextBackupContext.Backup(new ConfigDbContext(this), Path.Combine(BackupDir, backupFile), TableNameProcess);
                        list.Add(new FieldForGet()
                        {
                            Name = "成功",
                            Description = $"备份完成，文件：{backupFile}",
                            Type = FieldType.MessageBox,
                            MessageBox_CanCancel = false,
                            PostOnChanged = true
                        });
                    }
                    catch (Exception ex)
                    {
                        list.Add(new FieldForGet()
                        {
                            Name = "错误",
                            Description = $"备份时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                            Type = FieldType.MessageBox,
                            MessageBox_CanCancel = false,
                            PostOnChanged = true
                        });
                    }
                }
                //备份为xlsx文件
                else if (request.IsFieldIdsMatch(BTN_BACKUP_XLSX))
                {
                    try
                    {
                        var backupFile = $"{BackupFilePrefix}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                        var dbContextBackupContext = new DbContextBackup.Excel.XlsxDbContextBackupContext();
                        dbContextBackupContext.Backup(new ConfigDbContext(this), Path.Combine(BackupDir, backupFile), TableNameProcess);
                        list.Add(new FieldForGet()
                        {
                            Name = "成功",
                            Description = $"备份完成，文件：{backupFile}",
                            Type = FieldType.MessageBox,
                            MessageBox_CanCancel = false,
                            PostOnChanged = true
                        });
                    }
                    catch (Exception ex)
                    {
                        list.Add(new FieldForGet()
                        {
                            Name = "错误",
                            Description = $"备份时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                            Type = FieldType.MessageBox,
                            MessageBox_CanCancel = false,
                            PostOnChanged = true
                        });
                    }
                }
            }


            if (otherFields != null)
                list.AddRange(otherFields);

            list.Add(new FieldForGet()
            {
                Id = BTN_TEST,
                Name = "测试",
                Type = FieldType.Button,
                MarginRight = 1,
                MarginBottom = 1
            });
            if (!isReadOnly)
            {
                list.Add(new FieldForGet()
                {
                    Id = BTN_INIT,
                    Name = "初始化",
                    Type = FieldType.Button,
                    MarginRight = 1,
                    MarginBottom = 1
                });
            }
            list.Add(new FieldForGet()
            {
                Id = BTN_BACKUP_D3B,
                Name = "备份[d3b]",
                Type = FieldType.Button,
                MarginRight = 1,
                MarginBottom = 1
            });
            list.Add(new FieldForGet()
            {
                Id = BTN_BACKUP_XLSX,
                Name = "备份[xlsx]",
                Type = FieldType.Button,
                MarginRight = 1,
                MarginBottom = 1
            });
            return new()
            {
                Type = FieldType.ContainerGroup,
                Name = "常规",
                Children = list.ToArray()
            };
        }

        protected FieldForGet getAdvanceGroup(bool isReadOnly, params FieldForGet[] otherFields)
        {
            var list = new List<FieldForGet>();
            if (otherFields != null)
                list.AddRange(otherFields);
            list.Add(new()
            {
                Id = nameof(CommandTimeout),
                Name = "命令超时",
                Description = "单位：秒",
                Input_AllowBlank = false,
                Type = FieldType.InputNumber,
                Input_ReadOnly = isReadOnly,
                Value = CommandTimeout.ToString()
            });
            return new()
            {
                Type = FieldType.ContainerGroup,
                Name = "高级",
                Children = list.ToArray()
            };
        }

        protected bool GetIsReadOnly(FieldsForPostContainer request) => request != null && !string.IsNullOrEmpty(request.GetFieldValue(Quick_EntityFrameworkCore_Plus_AbstractDbContextConfigHandler_IsReadOnly));

        protected FieldForGet getRestoreGroup(FieldsForPostContainer request, bool isReadOnly, params FieldForGet[] otherFields)
        {
            var backupDi = new DirectoryInfo(BackupDir);
            if (!backupDi.Exists)
                backupDi.Create();

            var list = new List<FieldForGet>();
            if (request != null)
            {
                //按钮_还原
                if (request.IsFieldIdsMatch("*", BTN_RESTORE))
                {
                    var file = request.FieldIds[0];
                    list.Add(new()
                    {
                        Id = file,
                        Type = FieldType.ContainerRow,
                        Children = [
                            new(){
                            Id = EXECUTE_RESTORE,
                            Name = "还原",
                            Description = $"确定要使用备份文件[{Path.GetFileName(file)}]进行还原?",
                            Type = FieldType.MessageBox,
                            PostOnChanged = true,
                            MessageBox_CanCancel =true
                        }]
                    });
                }
                //执行_还原
                else if (request.IsFieldIdsMatch("*", EXECUTE_RESTORE))
                {
                    var messageBoxResult = request.GetFieldValue(request.FieldIds);
                    if (messageBoxResult == FieldForGet.MESSAGEBOX_VALUE_OK)
                    {
                        var backupFile = request.FieldIds[0];
                        try
                        {
                            //先删除表结构，再创建
                            DatabaseEnsureDeleted(() => new ConfigDbContext(this));
                            DatabaseEnsureCreatedAndUpdated(() => new ConfigDbContext(this));
                            DbContextBackup.DbContextBackupContext dbContextBackupContext = null;
                            //开始还原
                            var backupFileExtension = Path.GetExtension(backupFile).ToLower();
                            switch (backupFileExtension)
                            {
                                case ".d3b":
                                    dbContextBackupContext = new DbContextBackup.D3b.D3bDbContextBackupContext();
                                    break;
                                case ".xlsx":
                                    dbContextBackupContext = new DbContextBackup.Excel.XlsxDbContextBackupContext();
                                    break;
                                default:
                                    throw new FormatException($"未知备份文件格式[{backupFileExtension}]");
                            }
                            dbContextBackupContext.Restore(new ConfigDbContext(this), backupFile);
                            list.Add(new FieldForGet()
                            {
                                Name = "成功",
                                Description = $"还原完成",
                                Type = FieldType.MessageBox
                            });
                        }
                        catch (Exception ex)
                        {
                            list.Add(new FieldForGet()
                            {
                                Name = "错误",
                                Description = $"还原时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                                Type = FieldType.MessageBox
                            });
                        }
                    }
                }
                //按钮_删除
                else if (request.IsFieldIdsMatch("*", BTN_DELETE))
                {
                    var file = request.FieldIds[0];
                    list.Add(new()
                    {
                        Id = file,
                        Type = FieldType.ContainerRow,
                        Children = [
                            new(){
                            Id = EXECUTE_DELETE,
                            Name = "删除",
                            Description = $"确定要删除备份文件[{Path.GetFileName(file)}]?",
                            Type = FieldType.MessageBox,
                            PostOnChanged = true,
                            MessageBox_CanCancel =true
                        }]
                    });
                }
                //执行_删除
                else if (request.IsFieldIdsMatch("*", EXECUTE_DELETE))
                {
                    var messageBoxResult = request.GetFieldValue(request.FieldIds);
                    if (messageBoxResult == FieldForGet.MESSAGEBOX_VALUE_OK)
                    {
                        var backupFile = request.FieldIds[0];
                        try
                        {
                            File.Delete(backupFile);
                        }
                        catch (Exception ex)
                        {
                            list.Add(new FieldForGet()
                            {
                                Name = "错误",
                                Description = $"删除时出错，原因：{ExceptionUtils.GetExceptionString(ex)}",
                                Type = FieldType.MessageBox
                            });
                        }
                    }
                }
            }
            if (otherFields != null)
                list.AddRange(otherFields);

            var operateButtonList = new List<FieldForGet>();
            if (!isReadOnly)
            {
                operateButtonList.Add(new() { Id = BTN_RESTORE, Type = FieldType.Button, Name = "还原" });
            }
            operateButtonList.Add(new() { Id = BTN_DELETE, Type = FieldType.Button, Name = "删除", Theme = FieldTheme.Danger });

            var rowList = new List<FieldForGet>();
            foreach (var backupFile in backupDi.GetFiles("*.d3b").Concat(backupDi.GetFiles("*.xlsx")))
            {
                rowList.Add(new()
                {
                    Id = backupFile.FullName,
                    Type = FieldType.ContainerTableTr,
                    Children =
                    [
                        new (){Type = FieldType.ContainerTableTd,Value = backupFile.Name},
                        new (){Type = FieldType.ContainerTableTd,Value = storageUSC.GetString(backupFile.Length,1,true)+"B"},
                        new ()
                        {
                            Type = FieldType.ContainerTableTd,
                            Children =
                            [
                                new ()
                                {
                                    Type = FieldType.ButtonGroup,
                                    Children=operateButtonList.ToArray()
                                }
                            ]
                        }
                    ]
                });
            }

            list.Add(new()
            {
                Type = FieldType.ContainerTable,
                ContainerTable_Hoverable = true,
                ContainerTable_Bordered = true,
                Children =
                [
                    new ()
                    {
                        Type = FieldType.ContainerTableHead,
                        Theme = FieldTheme.Light,
                        Children =
                        [
                            new ()
                            {
                                Type = FieldType.ContainerTableTr,
                                Children =
                                [
                                    new (){ Type = FieldType.ContainerTableTh,Value="备份文件"},
                                    new (){ Type = FieldType.ContainerTableTh,Value="文件大小"},
                                    new (){ Type = FieldType.ContainerTableTh,Value="操作"}
                                ]
                            }
                        ]
                    },
                    new ()
                    {
                        Type = FieldType.ContainerTableBody,
                        Children = rowList.ToArray()
                    }
                ]
            });

            return new()
            {
                Type = FieldType.ContainerGroup,
                Name = "还原",
                Children = list.ToArray()
            };
        }

        protected void OnQuickFields_Request(FieldsForPostContainer request)
        {
            if (int.TryParse(request.GetFieldValue(nameof(CommandTimeout)), out var tmpCommandTimeout))
                CommandTimeout = tmpCommandTimeout;
        }

        public abstract FieldForGet[] QuickFields_Request(FieldsForPostContainer request = null);
    }
}
