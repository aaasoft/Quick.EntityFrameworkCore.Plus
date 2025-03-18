using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace Quick.EntityFrameworkCore.Plus.SqlServer
{
    [JsonSerializable(typeof(SqlServerDbContextConfigHandler))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class SqlServerDbContextConfigHandlerSerializerContext : JsonSerializerContext { }

    public class SqlServerDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        [JsonIgnore]
        public override string Name => "Microsoft SQL Server";

        public string Host { get; set; }
        public int Port { get; set; } = 1433;
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public override FieldForGet[] QuickFields_Request(FieldsForPostContainer request = null)
        {
            var isReadOnly = IsReadOnly(request);
            if (IsPost(request))
            {
                Host = request.GetFieldValue(nameof(Host));
                if (int.TryParse(request.GetFieldValue(nameof(Port)), out var tmpPort))
                    Port = tmpPort;
                Database = request.GetFieldValue(nameof(Database));
                User = request.GetFieldValue(nameof(User));
                Password = request.GetFieldValue(nameof(Password));
                OnQuickFields_Request(request);
            }
            var list = new List<FieldForGet>
            {
                getCommonGroup(request, isReadOnly,
                    new (){ Id=nameof(Host), Name="主机", Input_AllowBlank=false, Type = FieldType.InputText, Value=Host,Input_ReadOnly = isReadOnly },
                    new (){ Id=nameof(Port), Name="端口", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=Port.ToString(),Input_ReadOnly = isReadOnly },
                    new (){ Id=nameof(Database), Name="数据库", Input_AllowBlank=false, Type = FieldType.InputText, Value=Database,Input_ReadOnly = isReadOnly },
                    new (){ Id=nameof(User), Name="用户名", Input_AllowBlank=false, Type = FieldType.InputText, Value=User,Input_ReadOnly = isReadOnly },
                    new (){ Id=nameof(Password), Name="密码", Input_AllowBlank=false, Type = FieldType.InputPassword, Value=Password,Input_ReadOnly = isReadOnly }
                ),
                getAdvanceGroup(isReadOnly),
            };
            if (EnableOperateButtons)
            {
                list.Add(getRestoreGroup(request, isReadOnly));
            }
            return [new() { Type = FieldType.ContainerTab, Children = list.ToArray() }];
        }


        protected override IDbContextConfigHandler GetTestDbContextConfigHandler() => new SqlServerDbContextConfigHandler()
        {
            Host = Host,
            Port = Port,
            User = User,
            Password = Password,
            Database = Database,
            CommandTimeout = CommandTimeout
        };

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder()
            {
                DataSource = $"{Host},{Port}",
                InitialCatalog = Database,
                UserID = User,
                Password = Password,
                TrustServerCertificate = true
            };
            optionsBuilder.UseSqlServer(connectionStringBuilder.ConnectionString, options =>
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

        protected override string[] GetTableColumns(DbConnection dbConnection, string tableName)
        {
            List<string> columnList = new List<string>();
            using (var cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = $"SELECT COLUMN_NAME FROM \"INFORMATION_SCHEMA\".\"COLUMNS\" WHERE TABLE_CATALOG='{Database}' AND TABLE_NAME='{tableName}'";
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
