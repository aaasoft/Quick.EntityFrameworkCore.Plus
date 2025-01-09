﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Quick.Fields;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace Quick.EntityFrameworkCore.Plus
{
    public abstract class AbstractDbContextConfigHandler : IDbContextConfigHandler
    {
        public abstract string Name { get; }
        /// <summary>
        /// 命令超时时间（单位：秒）
        /// </summary>
        public int CommandTimeout { get; set; } = 60;

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
            };
            //如果存在字段修改过的实体类型
            if (columnChangedEntityTypeList.Count > 0)
            {
                logger?.Invoke($"即将自动更新表结构。。。");
                var dbContextBackup = new DbContextBackup.DbContextBackupContext();
                using (var ms = new MemoryStream())
                {
                    //备份
                    using (var dbContext = getDbContextFunc())
                        dbContextBackup.Backup(dbContext, ms, TableNameProcess);
                    //删除表结构
                    DatabaseEnsureDeleted(getDbContextFunc);
                    //创建表结构
                    DatabaseEnsureCreated();
                    //还原
                    ms.Position = 0;
                    using (var dbContext = getDbContextFunc())
                    {
                        dbContext.Database.EnsureCreated();
                        dbContextBackup.Restore(dbContext, ms);
                    }
                }
                logger?.Invoke($"表结构更新完成。");
            }
        }

        public virtual void DatabaseEnsureDeleted(Func<DbContext> getDbContextFunc)
        {
            using (var dbContext = getDbContextFunc())
                dbContext.Database.EnsureDeleted();
        }

        public virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

        public virtual void Test() { }

        public virtual void Validate() { }

        public virtual FieldForGet[] GetFields() => [];
        public virtual void SetFields(FieldForGet[] fields)
        {
            var container = new FieldsForGetContainer() { Fields = fields };
            CommandTimeout = int.Parse(container.GetFieldValue("Tab", "Advance", nameof(CommandTimeout)));
        }
        public virtual string TableNameProcess(string tableName) => tableName;
    }
}
