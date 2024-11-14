using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    public static class ModelUtils
    {
        /// <summary>
        /// 更新模型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="existModel">已经存在的模型</param>
        /// <param name="newModel">新模型</param>
        /// <returns>是否修改</returns>
        public static bool Update<T>(T existModel, T newModel)
        {
            return Update(typeof(T), existModel, newModel);
        }

        /// <summary>
        /// 更新模型
        /// </summary>
        /// <param name="type">模型的类型</param>
        /// <param name="existModel">已经存在的模型</param>
        /// <param name="newModel">新模型</param>
        /// <returns>是否修改</returns>
        public static bool Update(Type type, object existModel, object newModel)
        {
            bool hasModify = false;
            foreach (var pi in type.GetProperties())
            {
                //不处理设置了JsonIgnore特性的属性
                if (pi.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;
                //不处理设置了Notmapped特性的属性
                if (pi.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;
                //如果属性未改变，则复制属性的值
                if (pi.GetValue(newModel) == null)
                    pi.SetValue(newModel, pi.GetValue(existModel));
                else
                    hasModify = true;
            }
            return hasModify;
        }

        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool Equals<T>(this T self, object obj, params Func<T, object>[] parameters)
            where T : class
        {
            T toCompare = obj as T;
            if (toCompare == null)
            {
                return false;
            }
            foreach (var parameter in parameters)
                if (!Object.Equals(parameter(self), parameter(toCompare)))
                    return false;
            return true;
        }

        /// <summary>
        /// 计算哈希值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int GetHashCode<T>(this T self, params Func<T, object>[] parameters)
        {
            return GetHashCode(parameters.Select(parameter => parameter(self)).ToArray());
        }

        /// <summary>
        /// 计算哈希值
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int GetHashCode(params object[] parameters)
        {
            int hashCode = 13;
            if (parameters != null && parameters.Length > 0)
                foreach (var parameter in parameters)
                    if (parameter != null)
                        hashCode = (hashCode * 7) + parameter.GetHashCode();
            return hashCode;
        }
    }
}
