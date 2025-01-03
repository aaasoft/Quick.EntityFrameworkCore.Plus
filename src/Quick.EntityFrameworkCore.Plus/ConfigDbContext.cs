using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 配置数据库上下文
    /// </summary>
    public partial class ConfigDbContext : DbContext
    {
        private static Action<ModelBuilder> ModelBuilderHandler;
        public static DbCacheContext<ConfigDbContext> CacheContext { get; } = new DbCacheContext<ConfigDbContext>();
        /// <summary>
        /// 配置处理器
        /// </summary>
        public static IDbContextConfigHandler ConfigHandler { get; set; }

        public static void Init(IDbContextConfigHandler configHandler, Action<ModelBuilder> modelBuilderHandler)
        {
            ConfigHandler = configHandler;
            ModelBuilderHandler = modelBuilderHandler;
        }

        private IDbContextConfigHandler instanceConfigHandler = null;
        public ConfigDbContext() { }
        public ConfigDbContext(IDbContextConfigHandler instanceConfigHandler)
        {
            this.instanceConfigHandler = instanceConfigHandler;
        }

        private IDbContextConfigHandler GetCurrentConfigHandler()
        {
            //先取实例的变量
            var currentConfigHandler = instanceConfigHandler;
            //如果没有，再取静态变量
            if (currentConfigHandler == null)
                currentConfigHandler = ConfigHandler;
            if (currentConfigHandler == null)
                throw new ArgumentNullException(nameof(currentConfigHandler));
            return currentConfigHandler;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var currentConfigHandler = GetCurrentConfigHandler();
            currentConfigHandler.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ModelBuilderHandler(modelBuilder);
        }
    }
}
