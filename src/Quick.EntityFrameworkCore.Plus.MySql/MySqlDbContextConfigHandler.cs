using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Quick.Fields;
using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace Quick.EntityFrameworkCore.Plus.MySql
{
    [JsonSerializable(typeof(MySqlDbContextConfigHandler))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class MySqlDbContextConfigHandlerSerializerContext : JsonSerializerContext { }

    public class MySqlDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "MySQL";
        public string Host { get; set; }
        public int Port { get; set; } = 3306;
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public MySqlSslMode SslMode { get; set; } = MySqlSslMode.None;

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
                if (Enum.TryParse<MySqlSslMode>(request.GetFieldValue(nameof(SslMode)), out var tmpSslMode))
                    SslMode = tmpSslMode;
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
                getAdvanceGroup(isReadOnly,
                    new FieldForGet()
                    {
                        Id=nameof(SslMode),
                        Name="SSL模式",
                        Input_AllowBlank=false,
                        Type = FieldType.InputSelect,
                        Value=SslMode.ToString(),
                        InputSelect_Options = new Dictionary<string,string>()
                        {
                            [nameof(MySqlSslMode.None)] = "不使用SSL",
                            [nameof(MySqlSslMode.Preferred)] = "首选，如果服务端支持则使用SSL",
                            [nameof(MySqlSslMode.Required)] = "必需，始终使用 SSL。如果服务端不支持SSL，则拒绝连接。",
                            [nameof(MySqlSslMode.VerifyCA)] = "CA验证，始终使用SSL，验证证书颁发机构，但允许名称不匹配。",
                            [nameof(MySqlSslMode.VerifyFull)] = "完整验证，始终使用SSL，如果主机名不正确，则验证失败",
                        },
                        Input_ReadOnly = isReadOnly
                    }
                ),
            };
            if (EnableOperateButtons)
            {
                list.Add(getRestoreGroup(request, isReadOnly));
            }
            return [new() { Type = FieldType.ContainerTab, Children = list.ToArray() }];
        }

        protected override IDbContextConfigHandler GetTestDbContextConfigHandler() => new MySqlDbContextConfigHandler()
        {
            Host = Host,
            Port = Port,
            User = User,
            Password = Password,
            Database = "mysql",
            CommandTimeout = CommandTimeout,
            SslMode = SslMode
        };

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder()
            {
                Server = Host,
                Port = Convert.ToUInt32(Port),
                Database = Database,
                UserID = User,
                Password = Password,
                CharacterSet = DbConsts.MYSQL_DEFAULT_CHARSET,
                SslMode = SslMode
            };
            var connectionString = connectionStringBuilder.ConnectionString;
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, serverVersion, options =>
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
                cmd.CommandText = $"SELECT COLUMN_NAME FROM `information_schema`.`COLUMNS` WHERE TABLE_SCHEMA='{Database}' AND TABLE_NAME='{tableName}'";
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
