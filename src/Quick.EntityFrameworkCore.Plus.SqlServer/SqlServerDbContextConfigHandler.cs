using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;

namespace Quick.EntityFrameworkCore.Plus.SqlServer
{
    public class SqlServerDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "Microsoft SQL Server";

        public string Host { get; set; }
        public int Port { get; set; } = 1433;
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public override FieldForGet[] GetFields() =>
        [
            new FieldForGet()
            {
                Id="Tab",
                Type= FieldType.ContainerTab,
                Children=[
                    new FieldForGet()
                    {
                        Id="Common",
                        Type = FieldType.ContainerGroup,
                        Name="常规",
                        Children=[
                            new FieldForGet(){ Id=nameof(Host), Name="主机", Input_AllowBlank=false, Type = FieldType.InputText, Value=Host },
                            new FieldForGet(){ Id=nameof(Port), Name="端口", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=Port.ToString() },
                            new FieldForGet(){ Id=nameof(Database), Name="数据库", Input_AllowBlank=false, Type = FieldType.InputText, Value=Database },
                            new FieldForGet(){ Id=nameof(User), Name="用户名", Input_AllowBlank=false, Type = FieldType.InputText, Value=User },
                            new FieldForGet(){ Id=nameof(Password), Name="密码", Input_AllowBlank=false, Type = FieldType.InputPassword, Value=Password }
                        ]
                    },
                    new FieldForGet()
                    {
                        Id="Advance",
                        Type = FieldType.ContainerGroup,
                        Name="高级",
                        Children=[
                            new FieldForGet(){ Id=nameof(CommandTimeout), Name="命令超时",Description="单位：秒", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=CommandTimeout.ToString() }
                        ]
                    }
                ]
            }
        ];

        public override void SetFields(FieldForGet[] fields)
        {
            var container = new FieldsForGetContainer() { Fields = fields };
            Host = container.GetFieldValue("Tab", "Common", nameof(Host));
            Port = int.Parse(container.GetFieldValue("Tab", "Common", nameof(Port)));
            Database = container.GetFieldValue("Tab", "Common", nameof(Database));
            User = container.GetFieldValue("Tab", "Common", nameof(User));
            Password = container.GetFieldValue("Tab", "Common", nameof(Password));
            base.SetFields(fields);
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
