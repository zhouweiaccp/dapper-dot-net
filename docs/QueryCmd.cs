using Dapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImportData
{
    public class QueryCmd
    {
        public string Sql { get; set; }
        public DynamicParameters DynamicParameters { get; set; }
    }
}
