using System;
using System.Collections.Generic;
using System.Linq;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 依赖管理器
    /// </summary>
    public class DependcyManager
    {
        public static DependcyManager Instance { get; } = new DependcyManager();
        
        private List<ModelDependcyInfo> modelDependcyList = new List<ModelDependcyInfo>();

        public void Add(params ModelDependcyInfo[] t)
        {
            modelDependcyList.AddRange(t);
        }

        public void Remove(ModelDependcyInfo t)
        {
            modelDependcyList.Remove(t);
        }

        public void Clear()
        {
            modelDependcyList.Clear();
        }

        /// <summary>
        /// 获取源类型的依赖关系
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ModelDependcyInfo[] GetSourceTypeDependcy(Type type)
        {
            return modelDependcyList.Where(t => t.SourceType == type).ToArray();
        }

        /// <summary>
        /// 获取目标类型的被依赖关系
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ModelDependcyInfo[] GetTargetTypeBeenDependcy(Type type)
        {
            return modelDependcyList.Where(t => t.TargetType == type).ToArray();
        }

        /// <summary>
        /// 根据依赖关系排序
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Type[] OrderTypes(IEnumerable<Type> types)
        {
            var typeList = types.ToList();

            var typeRefrenceDict = types.ToDictionary(
                type => type,
                type => GetSourceTypeDependcy(type)
                                .Select(t => t.TargetType)
                                .Where(t => t != type)
                                .ToArray()
                );

            //根据依赖关系排序
            while (true)
            {
                //顺序是否改变过
                Boolean isOrderChanged = false;
                for (int i = 0; i < typeList.Count; i++)
                {
                    var type = typeList[i];
                    var typeRefrences = typeRefrenceDict[type];
                    if (typeRefrences == null
                        || typeRefrences.Length == 0)
                        continue;
                    //如果此插件依赖的插件在自己的后面
                    if (typeRefrences.Any(t => typeList.IndexOf(t) > i))
                    {
                        //将此插件移动到尾部
                        typeList.RemoveAt(i);
                        typeList.Insert(typeList.Count, type);
                        isOrderChanged = true;
                    }
                }
                //如果顺序没有改变，则跳出循环
                if (!isOrderChanged)
                    break;
            }
            return typeList.ToArray();
        }

        /// <summary>
        /// 检查依赖关系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="successItems">检查通过的对象们</param>
        /// <param name="failedItems">检查不通过的对象们</param>
        /// <param name="failedExceptions">失败的异常数组</param>
        public void CheckDependcy<T>(IEnumerable<T> rows, out T[] successItems, out T[] failedItems, out Exception[] failedExceptions)
        {
            List<T> successItemList = new List<T>();
            List<T> failedItemList = new List<T>();
            List<Exception> exceptionList = new List<Exception>();

            //先检查依赖关系
            foreach (var row in rows)
            {
                try
                {
                    foreach (var dependcy in DependcyManager.Instance.GetSourceTypeDependcy(typeof(T)))
                        dependcy.OnSaveOrUpdate(row);
                    successItemList.Add(row);
                }
                catch (DependcyException ex)
                {
                    failedItemList.Add(row);
                    exceptionList.Add(ex);
                }
            }

            successItems = successItemList.ToArray();
            failedItems = failedItemList.ToArray();
            failedExceptions = exceptionList.ToArray();
        }
    }
}
