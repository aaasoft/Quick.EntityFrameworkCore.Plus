using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 拥有初始化数据模型
    /// </summary>
    public interface IHasInitDataModel
    {
        /// <summary>
        /// 获取初始化数据
        /// </summary>
        /// <returns></returns>
        object[] GetInitData();
    }
}
