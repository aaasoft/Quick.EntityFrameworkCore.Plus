using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 数据库配置信息
    /// </summary>
    public class DbUtils
    {
        private static Dictionary<string, string> dbTypeDict;
        private static Dictionary<string, Func<IDbContextConfigHandler>> configHandlerFactoryDict;
        private static Dictionary<string, JsonTypeInfo> configHandlerTypeInfoDict;

        public static JsonNode SerializerConfigHandler(IDbContextConfigHandler configHandler)
        {
            var configHandlerTypeName = configHandler.GetType().FullName;
            if (!configHandlerTypeInfoDict.TryGetValue(configHandlerTypeName, out var configHandlerJsonTypeInfo))
                throw new ArgumentException($"DbType参数值错误，类型[{configHandlerTypeName}]未知！");
            return JsonSerializer.SerializeToNode(configHandler, configHandlerJsonTypeInfo);
        }

        public static Dictionary<string, string> GetDbTypeDict() => dbTypeDict;

        public static IDbContextConfigHandler GetDbContextConfigHandler(string dbType, JsonNode dbConfig = null)
        {
            if (!dbTypeDict.ContainsKey(dbType)
            || !configHandlerTypeInfoDict.TryGetValue(dbType, out var configHandlerJsonTypeInfo)
            || !configHandlerFactoryDict.TryGetValue(dbType, out var factory))
                throw new ArgumentException($"DbType参数值错误，类型[{dbType}]未知！");

            if (dbConfig == null)
                return factory.Invoke();
            return (IDbContextConfigHandler)JsonSerializer.Deserialize(dbConfig, configHandlerJsonTypeInfo);
        }

        public static void Init(params DbContextConfigHandlerInfo[] dbContextConfigHandlerInfos)
        {
            configHandlerFactoryDict = new();
            dbTypeDict = new();
            configHandlerTypeInfoDict = new();

            foreach (var item in dbContextConfigHandlerInfos)
            {
                configHandlerFactoryDict[item.Id] = item.Factory;
                dbTypeDict[item.Id] = item.Name;
                configHandlerTypeInfoDict[item.Id] = item.TypeInfo;
            }
        }
    }
}
