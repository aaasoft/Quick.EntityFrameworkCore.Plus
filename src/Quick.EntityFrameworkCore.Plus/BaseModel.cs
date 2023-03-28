using System.ComponentModel.DataAnnotations;

namespace Quick.EntityFrameworkCore.Plus
{
    /// <summary>
    /// 基础模型类
    /// </summary>
    public abstract class BaseModel
    {
        /// <summary>
        /// 编号
        /// </summary>
        [Key]
        [MaxLength(100)]
        public virtual string Id { get; set; }

        public override int GetHashCode()
        {
            return this.GetHashCode(
                t => t.Id);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj,
                t => t.Id);
        }
    }
}
