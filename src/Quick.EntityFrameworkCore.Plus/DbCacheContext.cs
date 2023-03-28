using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    public class DbCacheContext<TDbContext>
        where TDbContext : DbContext, new()
    {
        //不要缓存的类型哈希集
        private HashSet<Type> donotCacheTypeHashSet = new HashSet<Type>();
        //只有缓存的类型哈希集
        private HashSet<Type> onlyCacheTypeHashSet = new HashSet<Type>();

        /// <summary>
        /// 缓存字典
        /// </summary>
        protected ConcurrentDictionary<Type, IDictionary> cacheDict = new ConcurrentDictionary<Type, IDictionary>();

        protected Dictionary<Type, List<Delegate>> addedHandlerDict = new Dictionary<Type, List<Delegate>>();
        protected Dictionary<Type, List<Delegate>> updatedHandlerDict = new Dictionary<Type, List<Delegate>>();
        protected Dictionary<Type, List<Delegate>> removedHandlerDict = new Dictionary<Type, List<Delegate>>();

        public void UseDbContext(Action<TDbContext> action)
        {
            lock (typeof(TDbContext))
            {
                using (var dbContext = new TDbContext())
                {
                    action.Invoke(dbContext);
                }
            }
        }

        /// <summary>
        /// 设置忽略的类型
        /// </summary>
        /// <param name="types"></param>
        public void SetDoNotCacheTypes(params Type[] types)
        {
            donotCacheTypeHashSet = types.ToHashSet();
        }

        /// <summary>
        /// 设置仅缓存类型的缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void SetOnlyCacheTypeCaches<T>(IEnumerable<T> items)
        {
            var type = typeof(T);
            IDictionary dict = null;
            if (cacheDict.TryGetValue(type, out dict))
            {
                dict.Clear();
            }
            else
            {
                dict = new Dictionary<T, T>();
                cacheDict[type] = dict;
            }
            foreach (var item in items)
                dict[item] = item;
            lock (onlyCacheTypeHashSet)
                if (!onlyCacheTypeHashSet.Contains(type))
                    onlyCacheTypeHashSet.Add(type);
        }

        /// <summary>
        /// 加载缓存
        /// </summary>
        public void LoadCache()
        {
            UseDbContext(dbContext =>
            {
                var dbContextType = typeof(DbContext);
                var dbContextSetMethod = dbContextType.GetMethod(nameof(dbContext.Set), new Type[0]);
                foreach (var entityType in dbContext.Model.GetEntityTypes())
                {
                    var clazz = entityType.ClrType;
                    //如果类型在不要缓存集合里面，则忽略
                    if (donotCacheTypeHashSet.Contains(clazz))
                        continue;
                    //如果类型在仅缓存集合里面，则忽略
                    if (onlyCacheTypeHashSet.Contains(clazz))
                        continue;
                    var dbContextSetMethodImpl = dbContextSetMethod.MakeGenericMethod(clazz);
                    var queryable = (IQueryable)dbContextSetMethodImpl.Invoke(dbContext, null);
                    Dictionary<object, object> dict = new Dictionary<object, object>();
                    foreach (var obj in queryable)
                        dict[obj] = obj;
                    cacheDict[clazz] = dict;
                    //如果类型有依赖关系
                    if (typeof(IHasDependcyRelation).IsAssignableFrom(clazz))
                    {
                        var item = (IHasDependcyRelation)Activator.CreateInstance(clazz);
                        //加载依赖关系
                        DependcyManager.Instance.Add(item.GetDependcyRelation());
                    }
                }
            });
        }


        public void RegisterModelAddedHandler<T>(Action<T> handler)
        {
            var type = typeof(T);
            lock (addedHandlerDict)
            {
                List<Delegate> list = null;
                if (addedHandlerDict.ContainsKey(type))
                    list = addedHandlerDict[type];
                else
                    list = addedHandlerDict[type] = new List<Delegate>();
                list.Add(handler);
            }
        }

        public void UnregisterModelAddedHandler<T>(Action<T> handler)
        {
            var type = typeof(T);
            lock (addedHandlerDict)
            {
                List<Delegate> list = null;
                if (!addedHandlerDict.ContainsKey(type))
                    return;
                list = addedHandlerDict[type];
                if (list.Contains(handler))
                    list.Remove(handler);
            }
        }

        public void RegisterModelUpdatedHandler<T>(Action<T, T> handler)
        {
            var type = typeof(T);
            lock (updatedHandlerDict)
            {
                List<Delegate> list = null;
                if (updatedHandlerDict.ContainsKey(type))
                    list = updatedHandlerDict[type];
                else
                    list = updatedHandlerDict[type] = new List<Delegate>();
                list.Add(handler);
            }
        }

        public void UnregisterModelUpdatedHandler<T>(Action<T, T> handler)
        {
            var type = typeof(T);
            lock (updatedHandlerDict)
            {
                List<Delegate> list = null;
                if (!updatedHandlerDict.ContainsKey(type))
                    return;
                    list = updatedHandlerDict[type];
                if (list.Contains(handler))
                    list.Remove(handler);
            }
        }

        public void RegisterModelRemovedHandler<T>(Action<T> handler)
        {
            var type = typeof(T);
            lock (removedHandlerDict)
            {
                List<Delegate> list = null;
                if (removedHandlerDict.ContainsKey(type))
                    list = removedHandlerDict[type];
                else
                    list = removedHandlerDict[type] = new List<Delegate>();
                list.Add(handler);
            }
        }

        public void UnregisterModelRemovedHandler<T>(Action<T> handler)
        {
            var type = typeof(T);
            lock (removedHandlerDict)
            {
                List<Delegate> list = null;
                if (!removedHandlerDict.ContainsKey(type))
                    return;
                list = removedHandlerDict[type];
                if (list.Contains(handler))
                    list.Remove(handler);
            }
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Find<T>(T key)
        {
            var type = typeof(T);
            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return default(T);
            if (!dict.Contains(key))
                return default(T);
            return (T)dict[key];
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] Query<T>()
        {
            var type = typeof(T);
            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return null;
            lock (dict)
                return dict.Values.Cast<T>().ToArray();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        public T[] Query<T>(Func<T, bool> condition)
        {
            var type = typeof(T);
            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return null;
            lock (dict)
                return dict.Values.Cast<T>().Where(condition).ToArray();
        }

        public void Add(object model)
        {
            Add(model.GetType(), model);
        }

        public void Add<T>(T model)
        {
            Add(typeof(T), model);
        }

        public void Add(Type type,object model)
        {
            if (onlyCacheTypeHashSet.Contains(type))
                throw new ApplicationException($"仅缓存类型[{type.FullName}]数据不能添加。");

            //先保存到数据库中
            UseDbContext(dbContext =>
            {
                dbContext.Add(model);
                dbContext.SaveChanges();
            });
            
            if (donotCacheTypeHashSet.Contains(type))
                return;
            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return;
            //再添加到缓存中
            lock (dict)
            {
                //如果存在，则先移除
                if (dict.Contains(model))
                    dict.Remove(model);
                dict.Add(model, model);
            }
            //通知添加
            if(addedHandlerDict.ContainsKey(type))
            {
                var list = addedHandlerDict[type];
                foreach (var handler in list)
                    handler.DynamicInvoke(model);
            }
        }

        public void AddRange<T>(IEnumerable<T> rows)
        {
            AddRange(typeof(T), rows.Select(t => (object)t).ToArray());
        }

        public void AddRange(Type type, object[] rows)
        {
            if (onlyCacheTypeHashSet.Contains(type))
                throw new ApplicationException($"仅缓存类型[{type.FullName}]数据不能添加。");

            //先保存到数据库中
            UseDbContext(dbContext =>
            {
                dbContext.AddRange(rows);
                dbContext.SaveChanges();
            });

            if (donotCacheTypeHashSet.Contains(type))
                return;
            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return;
            //再添加到缓存中
            lock (dict)
            {
                foreach (var model in rows)
                {
                    //如果存在，则先移除
                    if (dict.Contains(model))
                        dict.Remove(model);
                    dict.Add(model, model);
                }
            }
            //通知添加
            if (addedHandlerDict.ContainsKey(type))
            {
                var list = addedHandlerDict[type];
                foreach (var handler in list)
                    foreach (var model in rows)
                        handler.DynamicInvoke(model);
            }
        }

        public void RemoveRange<T>(T[] rows, bool recursive = false, DbContext dbContext = null)
            where T : class
        {
            var type = typeof(T);
            if (onlyCacheTypeHashSet.Contains(type))
                throw new ApplicationException($"仅缓存类型[{type.FullName}]数据不能删除。");

            //如果是级联删除，且没有传入dbContext
            if (recursive && dbContext == null)
            {
                UseDbContext(dbContext => RemoveRange(rows, recursive, dbContext));
                return;
            }

            //先检查依赖关系
            foreach (var dependcy in DependcyManager.Instance.GetTargetTypeBeenDependcy(typeof(T)))
            {
                foreach (var row in rows)
                    dependcy.OnDelete(row, recursive);
            }

            //先从数据库中删除
            Action<DbContext> deleteAction = t =>
            {
                foreach (var row in rows)
                    t.Remove(row);
                t.SaveChanges();
            };
            if (dbContext == null)
                UseDbContext(dbContext => deleteAction(dbContext));
            else
                deleteAction(dbContext);

            //然后删除缓存
            foreach (var obj in rows)
                Remove(obj, recursive, false);

            //通知删除
            if (removedHandlerDict.ContainsKey(type))
            {
                var list = removedHandlerDict[type];
                foreach (var handler in list)
                    foreach (var model in rows)
                        handler.DynamicInvoke(model);
            }
        }

        public void Update<T>(T model)
        {
            Update(typeof(T), model);
        }

        public void Update(Type type, object model)
        {
            if (onlyCacheTypeHashSet.Contains(type))
                throw new ApplicationException($"仅缓存类型[{type.FullName}]数据不能更新。");

            //先更新到数据库中
            UseDbContext(dbContext =>
            {
                dbContext.Update(model);
                dbContext.SaveChanges();
            });
            
            if (donotCacheTypeHashSet.Contains(type))
                return;

            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return;
            object preModel = null;
            //再更新到缓存中
            lock (dict)
            {
                //如果存在，则先移除
                if (dict.Contains(model))
                {
                    preModel = dict[model];
                    dict.Remove(preModel);
                }
                //再添加
                dict.Add(model, model);
            }
            //通知更新
            if (updatedHandlerDict.ContainsKey(type))
            {
                var list = updatedHandlerDict[type];
                foreach (var handler in list)
                    handler.DynamicInvoke(preModel, model);
            }
        }

        public void Remove<T>(T model, bool recursive = false, bool deleteFromDb = true)
        {
            Remove(typeof(T), model, recursive, deleteFromDb);
        }

        public void Remove(Type type, object model,bool recursive=false, bool deleteFromDb = true)
        {
            if (onlyCacheTypeHashSet.Contains(type))
                throw new ApplicationException($"仅缓存类型[{type.FullName}]数据不能删除。");

            IDictionary dict = null;
            if (!cacheDict.TryGetValue(type, out dict))
                return;

            //先检查依赖关系
            foreach (var dependcy in DependcyManager.Instance.GetTargetTypeBeenDependcy(type))
            {
                dependcy.OnDelete(model, recursive);
            }
            //先从数据库中删除
            if (deleteFromDb)
            {
                if (!donotCacheTypeHashSet.Contains(type))
                {
                    UseDbContext(dbContext =>
                    {
                        dbContext.Remove(model);
                        dbContext.SaveChanges();
                    });
                }
            }
            //再更新到缓存中
            lock (dict)
            {
                if (dict.Contains(model))
                    dict.Remove(model);
            }
            //通知删除
            if (removedHandlerDict.ContainsKey(type))
            {
                var list = removedHandlerDict[type];
                foreach (var handler in list)
                    handler.DynamicInvoke(model);
            }
        }
    }
}
