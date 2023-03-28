using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Quick.Fields;

namespace Quick.EntityFrameworkCore.Plus.MySql
{
    public class MySqlDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "MySQL";
        public string Host { get; set; }
        public int Port { get; set; } = 3306;
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public override FieldForGet[] GetFields() => new FieldForGet[]
        {
            new FieldForGet(){ Id=nameof(Host), Name="主机", Input_AllowBlank=false, Type = FieldType.InputText, Value=Host },
            new FieldForGet(){ Id=nameof(Port), Name="端口", Input_AllowBlank=false, Type = FieldType.InputNumber, Value=Port.ToString() },
            new FieldForGet(){ Id=nameof(Database), Name="数据库", Input_AllowBlank=false, Type = FieldType.InputText, Value=Database },
            new FieldForGet(){ Id=nameof(User), Name="用户名", Input_AllowBlank=false, Type = FieldType.InputText, Value=User },
            new FieldForGet(){ Id=nameof(Password), Name="密码", Input_AllowBlank=false, Type = FieldType.InputPassword, Value=Password }
        };

        public override void Test()
        {
            var configHandler = new MySqlDbContextConfigHandler()
            {
                Host = Host,
                Port = Port,
                User = User,
                Password = Password,
                Database = "mysql"
            };
            using (var dbContext = new TestDbContext(configHandler))
                dbContext.Test();
        }

        public override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder()
            {
                Server = Host,
                Port = Convert.ToUInt32(Port),
                Database = Database,
                UserID = User,
                Password = Password,
                CharacterSet = DbConsts.MYSQL_DEFAULT_CHARSET
            };
            optionsBuilder.UseMySQL(connectionStringBuilder.GetConnectionString(true), options =>
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
    }
}
