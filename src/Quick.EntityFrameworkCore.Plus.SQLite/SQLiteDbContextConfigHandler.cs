using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System.Data;
using System.Data.Common;

namespace Quick.EntityFrameworkCore.Plus.SQLite
{
    public class SQLiteDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public const string CONFIG_DB_FILE = "Config.db";
        public const string EVENT_DB_FILE = "Event.db";

        public override string Name => "SQLite";
        public string FileName { get; set; }

        public override FieldForGet[] GetFields() => new FieldForGet[]
        {
            new FieldForGet(){ Id=nameof(FileName), Name="数据库文件", Input_AllowBlank=false, Type = FieldType.InputText, Value=FileName }
        };

        public SQLiteDbContextConfigHandler() { }
        public SQLiteDbContextConfigHandler(string fileName)
        {
            FileName = fileName;
        }

        public override void Test()
        {
            var configHandler = new SQLiteDbContextConfigHandler()
            {
                FileName = FileName
            };
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Test();
        }

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var file = FileName;
            optionsBuilder.UseSqlite($"Data Source={file}");
        }

        public override void Validate()
        {
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
