using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    public class TestDbContext : DbContext
    {
        private IDbContextConfigHandler handler;
        public TestDbContext(IDbContextConfigHandler handler)
        {
            this.handler = handler;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            handler.OnConfiguring(optionsBuilder);
        }

        public void Test()
        {
            Database.ExecuteSqlRaw("select 1;");
        }
    }
}