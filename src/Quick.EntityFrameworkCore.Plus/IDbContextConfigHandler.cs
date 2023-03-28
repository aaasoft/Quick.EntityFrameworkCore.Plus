using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    public interface IDbContextConfigHandler
    {
        string Name { get; }
        void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
        FieldForGet[] GetFields();
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
        /// <param name="dbContextType">数据库上下文类型</param>
        void DatabaseEnsureDeleted(Type dbContextType);
        /// <summary>
        /// 确保数据库创建
        /// </summary>
        /// <param name="dbContextType">数据库上下文类型</param>
        void DatabaseEnsureCreated(Type dbContextType);
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="dbContextType">数据库上下文类型</param>
        /// <returns></returns>
        DbContext CreateDbContextInstance(Type dbContextType);
    }
}
