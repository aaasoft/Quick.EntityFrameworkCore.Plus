using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace Quick.EntityFrameworkCore.Plus.SQLite
{
    public static class DbContextExtends
    {
        public static void EnsureDatabaseCreatedAndUpdated(this DbContext dbContext, Action<string> logger = null)
        {
            //如果是表结构第一次创建，则创建成功后直接返回
            if (dbContext.Database.EnsureCreated())
                return;
            using (var dbConnection = dbContext.Database.GetDbConnection())
            {
                dbConnection.Open();
                var isSchemaChanged = false;
                foreach (var entityType in dbContext.Model.GetEntityTypes().ToArray())
                {
                    var tableName = entityType.GetTableName();
                    var tableDefColumns = entityType.GetProperties().Select(t => t.Name).ToArray();
                    List<string> tmpList = new List<string>();
                    using (var cmd = dbConnection.CreateCommand())
                    {
                        cmd.CommandText = $"pragma table_info([{tableName}])";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var columnName = reader.GetString("name");
                                tmpList.Add(columnName);
                            }
                        }
                    }
                    var tableCurrentColumns = tmpList.ToArray();
                    var tableDefColumnsString = string.Join(",", tableDefColumns.OrderBy(t => t));
                    var tableCurrentColumnsString = string.Join(",", tableCurrentColumns.OrderBy(t => t));
                    if (tableDefColumnsString != tableCurrentColumnsString)
                    {
                        logger?.Invoke($"发现表[{tableName}]的结构不匹配，定义列：[{string.Join(",", tableDefColumns)}]，当前列：[{string.Join(",", tableCurrentColumns)}]。");
                        isSchemaChanged = true;
                        break;
                    }
                }
                if (isSchemaChanged)
                {
                    logger?.Invoke($"即将自动更新表结构。。。");
                    var dbContextBackup = new DbContextBackup.DbContextBackupContext();
                    using (var ms = new MemoryStream())
                    {
                        //备份
                        dbContextBackup.Backup(dbContext, ms);
                        //还原
                        ms.Position = 0;
                        dbContextBackup.Restore(dbContext, ms);
                    }
                    logger?.Invoke($"表结构更新完成。");
                }
            }
        }
    }
}
