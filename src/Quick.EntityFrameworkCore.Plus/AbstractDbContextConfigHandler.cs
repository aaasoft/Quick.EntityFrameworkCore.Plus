using Microsoft.EntityFrameworkCore;
using Quick.Fields;

namespace Quick.EntityFrameworkCore.Plus
{
    public abstract class AbstractDbContextConfigHandler : IDbContextConfigHandler
    {
        public abstract string Name { get; }

        public virtual DbContext CreateDbContextInstance(Type dbContextType)
        {
            return (DbContext)Activator.CreateInstance(dbContextType, this);
        }

        public virtual void DatabaseEnsureCreated(Type dbContextType)
        {
            using (var dbContext = CreateDbContextInstance(dbContextType))
                dbContext.Database.EnsureCreated();
        }

        public virtual void DatabaseEnsureDeleted(Type dbContextType)
        {
            using (var dbContext = CreateDbContextInstance(dbContextType))
                dbContext.Database.EnsureDeleted();
        }

        public virtual FieldForGet[] GetFields() => new FieldForGet[0];

        public virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

        public virtual void Test() { }

        public virtual void Validate() { }
    }
}
