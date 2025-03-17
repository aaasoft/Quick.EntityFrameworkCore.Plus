using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;

namespace Quick.EntityFrameworkCore.Plus.SQLite
{
    public class SQLiteDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "SQLite";

        public string DataSource { get; set; }
        public string JournalMode { get; set; } = "DELETE";

        public override FieldForGet[] QuickFields_Request(FieldsForPostContainer container = null)
        {
            if (container != null)
            {
                DataSource = container.GetFieldValue(nameof(DataSource));
                JournalMode = container.GetFieldValue(nameof(JournalMode));
                OnQuickFields_Request(container);
            }
            return [
                new ()
                {
                    Type= FieldType.ContainerTab,
                    Children=
                    [
                        new ()
                        {
                            Type = FieldType.ContainerGroup,
                            Name="常规",
                            Children=[
                                new FieldForGet(){ Id=nameof(DataSource), Name="数据源", Input_AllowBlank=false, Type = FieldType.InputText, Value=DataSource }
                            ]
                        },
                        getAdvanceGroup(
                        [
                            new ()
                            {
                                Id=nameof(JournalMode),
                                Name="日志模式", Input_AllowBlank=false, Type = FieldType.InputSelect, Value=JournalMode.ToString(),
                                InputSelect_Options = new Dictionary<string,string>()
                                {
                                    ["DELETE"] = "DELETE",
                                    ["TRUNCATE"] = "TRUNCATE",
                                    ["WAL"] = "WAL"
                                }
                            }
                        ])
                    ]
                }
            ];
        }


        public override DbContext CreateDbContextInstance(Type dbContextType)
        {
            var dbContext = base.CreateDbContextInstance(dbContextType);
            if (!string.IsNullOrEmpty(JournalMode))
            {
                var sql = $"PRAGMA journal_mode = {JournalMode}";
                dbContext.Database.ExecuteSqlRaw(sql);
            }
            return dbContext;
        }

        protected override IDbContextConfigHandler GetTestDbContextConfigHandler() => new SQLiteDbContextConfigHandler()
        {
            DataSource = DataSource,
            JournalMode = JournalMode,
            CommandTimeout = CommandTimeout
        };

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder()
            {
                DataSource = DataSource
            };
            optionsBuilder.UseSqlite(connectionStringBuilder.ConnectionString, options =>
            {
                options.CommandTimeout(CommandTimeout);
            });
        }

        public override void Validate()
        {
            var configHandler = new SQLiteDbContextConfigHandler()
            {
                DataSource = DataSource,
                JournalMode = JournalMode,
                CommandTimeout = CommandTimeout
            };
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Test();
        }

        protected override string[] GetTableColumns(DbConnection dbConnection, string tableName)
        {
            List<string> columnList = new List<string>();
            using (var cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = $"pragma table_info([{tableName}])";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString("name");
                        columnList.Add(columnName);
                    }
                }
            }
            return columnList.ToArray();
        }
    }
}
