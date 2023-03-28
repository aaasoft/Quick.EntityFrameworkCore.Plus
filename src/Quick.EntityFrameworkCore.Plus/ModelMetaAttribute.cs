using System;
using System.ComponentModel;
using System.Reflection;

namespace Quick.EntityFrameworkCore.Plus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelMetaAttribute : DisplayNameAttribute
    {
        public ModelMetaAttribute(string displayName) : base(displayName) { }
    }
}
