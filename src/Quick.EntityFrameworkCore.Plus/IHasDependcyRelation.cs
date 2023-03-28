using Quick.EntityFrameworkCore.Plus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    public interface IHasDependcyRelation
    {
        ModelDependcyInfo[] GetDependcyRelation();
    }
}
