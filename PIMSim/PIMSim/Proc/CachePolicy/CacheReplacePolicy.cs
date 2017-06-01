using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.Procs
{
    public abstract class CacheReplacePolicy
    {
        public static UInt64 NULL = UInt64.MaxValue;
        public abstract bool Calculate_Rep(int assoc, int index, CacheEntity[,] cache_, ref int ret_assoc);
        public abstract bool Calculate_Rep_Shared(int assoc, int index, CacheEntity[,] cache_, ref int ret_assoc);
    }
}
