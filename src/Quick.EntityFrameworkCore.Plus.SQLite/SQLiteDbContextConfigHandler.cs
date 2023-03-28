using Microsoft.EntityFrameworkCore;
using Quick.Fields;

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
    }
}
