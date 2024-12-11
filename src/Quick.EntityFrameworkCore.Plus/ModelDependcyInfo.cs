using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Quick.EntityFrameworkCore.Plus
{
    public class ModelDependcyInfo
    {
        /// <summary>
        /// 起始类型，比如控制器类型依赖地点类型，此属性为控制器类型
        /// </summary>
        public Type SourceType { get; set; }
        /// <summary>
        /// 目标类型，比如控制器类型依赖地点类型，此属性为地点类型
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// 当保存或更新时
        /// </summary>
        public Action<object> OnSaveOrUpdate { get; set; }

        /// <summary>
        /// 当删除时
        /// </summary>
        public Action<object, bool> OnDelete { get; set; }
        
        public ModelDependcyInfo(Type sourceType, Type targetType,
            Action<object> onAdd,
            Action<object, bool> onDelete)
        {
            this.SourceType = sourceType;
            this.TargetType = targetType;
            this.OnSaveOrUpdate = onAdd;
            this.OnDelete = onDelete;
        }
    }

    public class ModelDependcyInfo<TSource, TTarget> : ModelDependcyInfo
        where TSource : class
        where TTarget : class
    {
        protected Func<TSource, TTarget> getIdFunc;
        protected Func<TTarget, Expression<Func<TSource, bool>>> getDeleteCheckExpression;
        protected Action<TSource, TTarget> deleteCheckFaildAction;

        public ModelDependcyInfo(
            Func<TSource, TTarget> getTargetIdFunc,
            Func<TTarget, Expression<Func<TSource, bool>>> getDeleteCheckExpression,
            Action<TSource, TTarget> deleteCheckFaildAction = null
            )
            : base(
                  typeof(TSource),
                  typeof(TTarget),
                  null,
                  null)
        {
            this.getIdFunc = getTargetIdFunc;
            this.getDeleteCheckExpression = getDeleteCheckExpression;
            this.deleteCheckFaildAction = deleteCheckFaildAction;
            if (this.deleteCheckFaildAction == null)
                this.deleteCheckFaildAction = (source, target) =>
                    throw new DependcyException($"{source.ToString()}关联了{target.ToString()}");

            base.OnSaveOrUpdate = t => onSaveOrUpdate((TSource)t);
            base.OnDelete = (t, r) => onDelete((TTarget)t, r);
        }

        private void onSaveOrUpdate(TSource source)
        {
            var id = getIdFunc(source);
            if (id == null)
                return;
            var model = ConfigDbContext.CacheContext.Find(id);
            if (model != null)
                return;

            var type = typeof(TTarget);            
            var commentAttr = type.GetCustomAttribute<CommentAttribute>();
            if (commentAttr == null)
                throw new DependcyException($"未找到编号为[{id}]的实体。");
            throw new DependcyException($"未找到编号为[{id}]的{commentAttr.Comment}。");
        }

        private void onDelete(TTarget target, bool recursive)
        {
            //如果递归删除
            if (recursive)
            {
                var models = ConfigDbContext.CacheContext.Query<TSource>()
                    .Where(getDeleteCheckExpression(target)
                    .Compile())
                    .ToArray();
                if (models.Length == 0)
                    return;
                ConfigDbContext.CacheContext.RemoveRange(models, recursive);
            }
            else
            {
                var model = ConfigDbContext.CacheContext.Query<TSource>()
                    .FirstOrDefault(getDeleteCheckExpression(target).Compile());
                if (model == null)
                    return;
                deleteCheckFaildAction(model, target);
            }
        }
    }

    public class ModelDependcyInfo_Relation<TRelation, TTarget1, TTarget2> : ModelDependcyInfo<TRelation, TTarget1>
        where TRelation : class
        where TTarget1 : class
        where TTarget2 : class
    {
        public ModelDependcyInfo_Relation(
            Func<TRelation, TTarget1> getTarget1IdFunc,
            Func<TRelation, TTarget2> getTarget2IdFunc,
            Func<TTarget1, Expression<Func<TRelation, bool>>> getDeleteCheckExpression)
            : base(getTarget1IdFunc,
                  getDeleteCheckExpression,
                  null)
        {
            var target2Type = typeof(TTarget2);
            var attr = target2Type.GetCustomAttribute<CommentAttribute>();
            var target2ModelName = attr?.Comment;
            
            base.deleteCheckFaildAction = (source, target1) =>
                throw new DependcyException($"{target1.ToString()}关联了{ConfigDbContext.CacheContext.Find<TTarget2>(getTarget2IdFunc(source)).ToString() ?? "未知" + target2ModelName}");
        }
    }
}
