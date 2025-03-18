using Dm;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace Quick.EntityFrameworkCore.Plus.Dm
{
    [JsonSerializable(typeof(DmDbContextConfigHandler))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class DmDbContextConfigHandlerSerializerContext : JsonSerializerContext { }

    public class DmDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        private const string SYSTEM_SCHEMA = "SYSDBA";
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
        public override string TableNameProcess(string tableName)
        {
            return $"\"{tableName}\"";
        }

        public override FieldForGet[] QuickFields_Request(FieldsForPostContainer request = null)
        {
            var isReadOnly = IsReadOnly(request);
            if (IsPost(request))
            {
                OnQuickFields_Request(request);

                Host = request.GetFieldValue(nameof(Host));
                if (int.TryParse(request.GetFieldValue(nameof(Port)), out var tmpPort))
                    Port = tmpPort;
                Database = request.GetFieldValue(nameof(Database));
                User = request.GetFieldValue(nameof(User));
                Password = request.GetFieldValue(nameof(Password));
            }
            return
            [
                new ()
                {
                    Type= FieldType.ContainerTab,
                    Children=[
                        getCommonGroup(request,isReadOnly,
                            new (){ Id=nameof(Host), Name="主机", Input_AllowBlank=false, Type = FieldType.InputText, Value=Host,Input_ReadOnly = isReadOnly },
                            new (){ Id=nameof(Port), Name="端口", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=Port.ToString(),Input_ReadOnly = isReadOnly },
                            new (){ Id=nameof(Database), Name="数据库", Input_AllowBlank=false, Type = FieldType.InputText, Value=Database,Input_ReadOnly = isReadOnly },
                            new (){ Id=nameof(User), Name="用户名", Input_AllowBlank=false, Type = FieldType.InputText, Value=User,Input_ReadOnly = isReadOnly },
                            new (){ Id=nameof(Password), Name="密码", Input_AllowBlank=false, Type = FieldType.InputPassword, Value=Password,Input_ReadOnly = isReadOnly }
                        ),
                        getAdvanceGroup(isReadOnly),
                        getRestoreGroup(request, isReadOnly)
                    ]
                }
            ];
        }

        protected override IDbContextConfigHandler GetTestDbContextConfigHandler() => new DmDbContextConfigHandler()
        {
            Host = Host,
            Port = Port,
            User = User,
            Password = Password,
            Database = SYSTEM_SCHEMA,
            CommandTimeout = CommandTimeout
        };

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new DmConnectionStringBuilder()
            {
                Server = Host,
                Port = Port,
                User = User,
                Password = Password,
                Schema = Database,
                CommandTimeout = CommandTimeout
            };
            optionsBuilder.UseDm(connectionStringBuilder.ConnectionString, options =>
            {
                options.CommandTimeout(CommandTimeout);
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

        public override void DatabaseEnsureDeleted(Func<DbContext> getDbContextFunc)
        {
            //删除库
            using (var dbContext = new TestDbContext(GetTestDbContextConfigHandler()))
            {
                var sql = $"drop schema if exists \"{Database}\" cascade;";
                dbContext.Database.ExecuteSqlRaw(sql);
            }
        }

        public override void DatabaseEnsureCreated()
        {
            //创建库
            using (var dbContext = new TestDbContext(GetTestDbContextConfigHandler()))
            {
                var sql = $"create schema \"{Database}\";";
                dbContext.Database.ExecuteSqlRaw(sql);
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
