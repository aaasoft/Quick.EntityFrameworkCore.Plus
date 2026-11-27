using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Quick.Fields;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Quick.EntityFrameworkCore.Plus.MongoDB
{
    [JsonSerializable(typeof(MongoDbContextConfigHandler))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class MongoDbContextConfigHandlerSerializerContext : JsonSerializerContext { }

    public class MongoDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public static DbContextConfigHandlerInfo Info { get; } = new DbContextConfigHandlerInfo()
        {
            Id = typeof(MongoDbContextConfigHandler).FullName,
            Name = "MongoDB",
            Factory = () => new MongoDbContextConfigHandler(),
            TypeInfo = MongoDbContextConfigHandlerSerializerContext.Default.MongoDbContextConfigHandler
        };
        [JsonIgnore]
        public override string Name => Info.Name;

        public string Host { get; set; }
        public int Port { get; set; } = 27017;
        public string ReplicaSetName { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public override List<FieldForGet> QuickFields_Request(FieldsForPostContainer request = null)
        {
            var isReadOnly = IsReadOnly(request);
            if (IsPost(request))
            {
                Host = request.GetFieldValue(nameof(Host));
                if (int.TryParse(request.GetFieldValue(nameof(Port)), out var tmpPort))
                    Port = tmpPort;
                Database = request.GetFieldValue(nameof(Database));
                ReplicaSetName = request.GetFieldValue(nameof(ReplicaSetName));
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
                getAdvanceGroup(isReadOnly,
                    new FieldForGet(){ Id=nameof(ReplicaSetName), Name="副本集名称", Input_AllowBlank=false, Type = FieldType.InputText, Value=ReplicaSetName,Input_ReadOnly = isReadOnly })
            };
            if (EnableOperateButtons && EnableRestoreButtons)
            {
                list.Add(getRestoreGroup(request, isReadOnly));
            }
            return [new() { Type = FieldType.ContainerTab, Children = list }];
        }

        protected override DbContext CreateDbContext()
        {
            var dbContext = base.CreateDbContext();
            if (string.IsNullOrEmpty(ReplicaSetName))
                dbContext.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            return dbContext;
        }

        public override DbContext CreateDbContextInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type dbContextType)
        {
            var dbContext = base.CreateDbContextInstance(dbContextType);
            if (string.IsNullOrEmpty(ReplicaSetName))
                dbContext.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            return dbContext;
        }

        protected override IDbContextConfigHandler GetTestDbContextConfigHandler() => new MongoDbContextConfigHandler()
        {
            Host = Host,
            Port = Port,
            User = User,
            Password = Password,
            Database = "admin",
            ReplicaSetName = ReplicaSetName
        };

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMongoDB(new MongoClientSettings()
            {
                Server = new MongoServerAddress(Host, Port),
                Credential = MongoCredential.CreateCredential("admin", User, Password),
                ReplicaSetName = ReplicaSetName,
                ConnectTimeout = TimeSpan.FromSeconds(ConnectTimeout),
                ServerSelectionTimeout = TimeSpan.FromSeconds(ConnectTimeout)
            }, Database);
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
