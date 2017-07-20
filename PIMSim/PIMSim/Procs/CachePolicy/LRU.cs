#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Statistics;
#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// LRU implement
    /// </summary>
    public class LRU : CacheReplacePolicy
    {
        #region Public Methods

        /// <summary>
        /// l1Cache replace implement
        /// </summary>
        /// <param name="assoc">cache assoc</param>
        /// <param name="index">cache index</param>
        /// <param name="cache_">cache</param>
        /// <param name="ret_assoc">counted replace assoc index</param>
        /// <returns></returns>
        public override bool Calculate_Rep(int assoc, int index,CacheEntity[,] cache_,ref int ret_assoc)
        {
            int min_index = -1;
            UInt64 timestamp = UInt64.MaxValue;
            for(int i = 0; i < assoc; i++)
            {
                if (cache_[i, index].block_addr == NULL)
                {
                    ret_assoc = i;
                    
                    return false;
                }
                if(cache_[i, index].timestamp < timestamp)
                {
                    min_index = i;
                    timestamp = cache_[i, index].timestamp;
                }

            }
            if (min_index != -1)
            {
                ret_assoc = min_index;
                return true;
            }
            //found no suitable replacement
            DEBUG.WriteLine("ERROR : No suitable replacement found.");
            return false;

        }

        /// <summary>
        /// share cache replace implement
        /// </summary>
        /// <param name="assoc"></param>
        /// <param name="index"></param>
        /// <param name="cache_"></param>
        /// <param name="ret_assoc"></param>
        /// <returns></returns>
        public override bool Calculate_Rep_Shared(int assoc, int index, CacheEntity[,] cache_, ref int ret_assoc)
        {
            ret_assoc = -1;
            for (int i = assoc-1; i >=0; i--)
            {
                if (!cache_[i, index].valid)
                {
                    ret_assoc= i;
                    return false;
                }

            }

            return true;
        }
        #endregion
    }

}
