using Dm;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;

namespace Quick.EntityFrameworkCore.Plus.Dm
{
    public class DmDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "达梦";
        public string Host { get; set; }
        public int Port { get; set; } = 5236;
        private string _Database;
        public string Database
        {
            get { return _Database; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    _Database = value;
                else
                    _Database = value.ToUpper();
            }
        }
        public string User { get; set; }
        public string Password { get; set; }

        public override FieldForGet[] GetFields() =>
        [
            new FieldForGet(){ Id=nameof(Host), Name="主机", Input_AllowBlank=false, Type = FieldType.InputText, Value=Host },
            new FieldForGet(){ Id=nameof(Port), Name="端口", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=Port.ToString() },
            new FieldForGet(){ Id=nameof(Database), Name="数据库", Input_AllowBlank=false, Type = FieldType.InputText, Value=Database },
            new FieldForGet(){ Id=nameof(User), Name="用户名", Input_AllowBlank=false, Type = FieldType.InputText, Value=User },
            new FieldForGet(){ Id=nameof(Password), Name="密码", Input_AllowBlank=false, Type = FieldType.InputPassword, Value=Password }
        ];
        public override void SetFields(FieldForGet[] fields)
        {
            var container = new FieldsForGetContainer() { Fields = fields };
            Host = container.GetFieldValue(nameof(Host));
            Port = int.Parse(container.GetFieldValue(nameof(Port)));
            Database = container.GetFieldValue(nameof(Database));
            User = container.GetFieldValue(nameof(User));
            Password = container.GetFieldValue(nameof(Password));
        }

        public override void Test()
        {
            var configHandler = new DmDbContextConfigHandler()
            {
                Host = Host,
                Port = Port,
                User = User,
                Password = Password,
                Database = "SYSDBA"
            };
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Test();
        }

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new DmConnectionStringBuilder()
            {
                Server = Host,
                Port = Port,
                Schema = Database,
                User = User,
                Password = Password
            };

            optionsBuilder.UseDm(connectionStringBuilder.ConnectionString, options =>
            {
                options.CommandTimeout(3600);
            });
        }

        public override void Validate()
        {
            if (string.IsNullOrEmpty(Host))
                throw new ArgumentNullException(nameof(Host), "必须输入主机！");
            if (string.IsNullOrEmpty(Database))
                throw new ArgumentNullException(nameof(Database), "必须输入数据库！");
            if (Port <= 0 || Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "端口的范围是1~65535");
        }

        public override void DatabaseEnsureDeleted(Type dbContextType)
        {
            var configHandler = new DmDbContextConfigHandler()
            {
                Host = Host,
                Port = Port,
                User = User,
                Password = Password,
                Database = "SYSDBA"
            };

            //删除库
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Database.ExecuteSql($"drop schema if exists \"{Database}\" cascade;");
        }

        public override void DatabaseEnsureCreated(Type dbContextType)
        {
            var configHandler = new DmDbContextConfigHandler()
            {
                Host = Host,
                Port = Port,
                User = User,
                Password = Password,
                Database = "SYSDBA"
            };

            //创建库
            using (var dbContext = new TestDbContext(configHandler))
            {
                dbContext.Database.ExecuteSql($"create schema \"{Database}\";");
                dbContext.SaveChanges();
            }

            //创建表结构
            using (var dbContext = CreateDbContextInstance(dbContextType))
            {
                dbContext.Database.EnsureCreated();
                dbContext.SaveChanges();
            }
        }

        protected override string[] GetTableColumns(DbConnection dbConnection, string tableName)
        {
            List<string> columnList = new List<string>();
            using (var cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = $"select COLUMN_NAME from all_tab_columns where owner='{Database}' and Table_Name='{tableName}'";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString("COLUMN_NAME");
                        columnList.Add(columnName);
                    }
                }
            }
            return columnList.ToArray();
        }
    }
}
