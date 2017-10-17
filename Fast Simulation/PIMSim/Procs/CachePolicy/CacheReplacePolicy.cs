#region Reference 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// Cache Replace policy
    /// </summary>
    public abstract class CacheReplacePolicy
    {
        #region Static Variables

        public static UInt64 NULL = UInt64.MaxValue;

        #endregion

        #region Abstract Methods
        public abstract bool Calculate_Rep(int assoc, int index, CacheEntity[,] cache_, ref int ret_assoc);
        public abstract bool Calculate_Rep_Shared(int assoc, int index, CacheEntity[,] cache_, ref int ret_assoc);
        #endregion
    }
}
