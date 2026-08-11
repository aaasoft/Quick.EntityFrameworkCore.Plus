using System.Text.Json.Serialization.Metadata;

namespace Quick.EntityFrameworkCore.Plus
{
    public class DbContextConfigHandlerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Func<IDbContextConfigHandler> Factory { get; set; }
        public JsonTypeInfo TypeInfo { get; set; }
    }
}
