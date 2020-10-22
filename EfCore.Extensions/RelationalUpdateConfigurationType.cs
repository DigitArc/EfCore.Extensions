using System;

namespace EfCore.Extensions
{
    public class RelationalUpdateConfigurationType
    {
        public Type Type { get; set; }
        public bool RemoveOnDatabase { get; set; }
    }
}