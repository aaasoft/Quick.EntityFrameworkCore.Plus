using Microsoft.EntityFrameworkCore;
using Quick.Fields;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quick.EntityFrameworkCore.Plus
{
    internal class EmptyDbContextConfigHandler : AbstractDbContextConfigHandler
    {
        public override string Name => "空";
        protected override string[] GetTableColumns(DbConnection dbConnection, string tableName) => [];
    }
}
