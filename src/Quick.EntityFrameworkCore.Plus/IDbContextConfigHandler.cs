using Microsoft.EntityFrameworkCore;
using Quick.Fields;

namespace Quick.EntityFrameworkCore.Plus
{
    public interface IDbContextConfigHandler
    {
        string Name { get; }
        void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
        ///QuickFields的请求
        FieldForGet[] QuickFields_Request(FieldsForPostContainer container = null);
        /// <summary>
        /// 表名处理
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        string TableNameProcess(string arg);
        /// <summary>
        /// 验证参数
        /// </summary>
        void Validate();
        /// <summary>
        /// 测试
        /// </summary>
        void Test();
        /// <summary>
        /// 确保数据库删除
        /// </summary>
        /// <param name="getDbContextFunc">获取数据库上下文方法</param>
        void DatabaseEnsureDeleted(Func<DbContext> getDbContextFunc);
        /// <summary>
        /// 确保数据库
        /// </summary>
        /// <param name="getDbContextFunc"></param>
        /// <param name="logger"></param>
        void DatabaseEnsureCreatedAndUpdated(Func<DbContext> getDbContextFunc, Action<string> logger = null);
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="dbContextType">数据库上下文类型</param>
        /// <returns></returns>
        DbContext CreateDbContextInstance(Type dbContextType);
    }
}
