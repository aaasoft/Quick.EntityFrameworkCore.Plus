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
                            new FieldForGet(){ Id=nameof(DataSource), Name="数据源", Input_AllowBlank=false, Type = FieldType.InputText, Value=DataSource }
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
            DataSource = container.GetFieldValue("Tab", "Common", nameof(DataSource));
            base.SetFields(fields);
        }

        public SQLiteDbContextConfigHandler() { }
        public SQLiteDbContextConfigHandler(string fileName)
        {
            DataSource = fileName;
        }

        public override void Test()
        {
            var configHandler = new SQLiteDbContextConfigHandler()
            {
                DataSource = DataSource,
                CommandTimeout = CommandTimeout
            };
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Test();
        }

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
