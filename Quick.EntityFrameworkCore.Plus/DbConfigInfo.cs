using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Quick.Fields;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 数据库配置信息
    /// </summary>
    public class DbConfigInfo
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; }
        /// <summary>
        /// 数据库连接参数
        /// </summary>
        public JsonNode DbConnectionParameter { get; set; }

        public FieldForGet GetDbConfigGroup(FieldsForPostContainer request, bool isReadOnly,
            string dbKey, string dbName,
            Func<IDbContextConfigHandler, DbContext> dbContextFactory,
            Func<Type, JsonSerializerContext> getModelsJsonSerializerContextFunc,
            Func<IDbContextConfigHandler> getDbContextConfigHandler,
            Action<IDbContextConfigHandler> setDbContextConfigHandler
        )
        {
            var dbConfigRequest = new FieldsForPostContainer();
            //准备Children
            var dbConfigRequestFieldList = new List<FieldForPost>();
            if (isReadOnly)
            {
                dbConfigRequestFieldList.Add
                (
                    new()
                    {
                        Id = AbstractDbContextConfigHandler.Quick_EntityFrameworkCore_Plus_AbstractDbContextConfigHandler_IsReadOnly,
                        Value = isReadOnly.ToString()
                    }
                );
            }
            var dbConfigHandler = getDbContextConfigHandler();
            if (request != null)
            {
                var prefixFieldIds = new string[] { dbKey, nameof(DbConnectionParameter) };
                //准备FieldIds
                if (request.IsFieldIdsMatch(prefixFieldIds))
                {
                    AbstractDbContextConfigHandler.CreateDbContextForOperateFunc = dbContextFactory;
                    AbstractDbContextConfigHandler.BackupFilePrefix = dbName;
                    dbConfigRequest.FieldIds = request.FieldIds.Skip(prefixFieldIds.Length).ToArray();
                }
                else
                {
                    //设置FieldIds不为null，代表是Post请求
                    dbConfigRequest.FieldIds = [];
                }
                var otherChildren = request.GetField(prefixFieldIds)?.Children;
                if (otherChildren != null)
                    dbConfigRequestFieldList.AddRange(otherChildren);

                dbConfigHandler = DbUtils.GetDbContextConfigHandler(DbType, getModelsJsonSerializerContextFunc);
                setDbContextConfigHandler(dbConfigHandler);
            }
            dbConfigRequest.Fields = dbConfigRequestFieldList.ToArray();
            var list = new List<FieldForGet>
        {
            new ()
            {
                Id=nameof(DbType),
                Name="数据库类型",
                Type= FieldType.InputSelect,
                InputSelect_Options = DbUtils.GetDbTypeDict(),
                PostOnChanged=true,
                Value = DbType,
                Input_ReadOnly = isReadOnly
            }
        };
            list.AddRange(
            [
                new ()
            {
                Id=nameof(DbConnectionParameter),
                Type = FieldType.ContainerRow,
                Children=
                [
                    new ()
                    {
                        Type = FieldType.HtmlDiv,
                        ColumnWidth = 0,
                        Children =  dbConfigHandler.QuickFields_Request(dbConfigRequest)
                    }
                ]
            },
            new FieldForGet()
            {
                Type = FieldType.ContainerRow,
                Margin = 1
            }
            ]);
            return new FieldForGet()
            {
                Id = dbKey,
                Type = FieldType.ContainerGroup,
                Name = dbName,
                Children = list.ToArray()
            };
        }
    }
}
