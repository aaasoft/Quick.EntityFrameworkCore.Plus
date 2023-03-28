using System;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 依赖关系异常
    /// </summary>
    public class DependcyException : Exception
    {
        public DependcyException(string message)
            : base(message)
        {
        }
    }
}
