using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SimplePIM.Procs
{
    public class LRU : CacheReplacePolicy
    {
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
            Console.WriteLine("ERROR : No suitable replacement found.");
            return false;

        }

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
    }

}
