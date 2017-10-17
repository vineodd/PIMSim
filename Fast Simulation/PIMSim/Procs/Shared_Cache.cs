#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Statistics;

#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// Shared Cahce Defination.
    /// </summary>
    public class Shared_Cache
    {

        #region Static Variables
        private readonly static UInt64 NULL = UInt64.MaxValue;
        #endregion

        #region Private Variables

        /// <summary>
        /// Evict policy
        /// </summary>
        private CacheReplacePolicy replace_policy;

        /// <summary>
        /// Cache Information
        /// </summary>
        private CacheEntity[,] cache;

        /// <summary>
        /// time stamp for evicting
        /// </summary>
        private UInt64 cycle;

        /// <summary>
        /// total sets
        /// </summary>
        private int max_set;


        private int assoc = 0;

        #endregion

        #region Statistics Variables

        public UInt64 hits = 0;      //number of cache hits
        public UInt64 miss = 0;     //number of cache misses

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Functions
        /// </summary>
        public Shared_Cache()
        {
            replace_policy = (new LRU() as CacheReplacePolicy);

            cycle = 0;
            int set_size = 0;
            assoc = Config.shared_cache_assoc;
            set_size = Config.block_size * assoc;
            max_set = Config.shared_cache_size / set_size;
            assoc = Config.shared_cache_assoc;
            cache = new CacheEntity[assoc, max_set];
            for (int i = 0; i < assoc; i++)
            {
                for (int j = 0; j < max_set; j++)
                {
                    cache[i, j] = new CacheEntity(NULL, 0, false, 0, false);
                }
            }
        }

        /// <summary>
        /// Search for a cacheline in cache.
        /// </summary>
        /// <param name="block_addr_">target block address</param>
        /// <param name="reqt_">related request type</param>
        /// <returns></returns>
        public bool search_block(UInt64 block_addr_, RequestType reqt_)
        {
            cycle++;

            UInt64 index = block_addr_ % (uint)max_set;

            for (int i = 0; i < assoc; i++)
            {
                if (cache[i, index].block_addr == block_addr_&& cache[i, index].valid == true)
                {
                    //cache hit
                    hits++;
                    cache[i, index].timestamp = cycle;
                    if (reqt_ == RequestType.WRITE)
                        cache[i, index].dirty = true;
                    if (Config.DEBUG_CACHE)
                        DEBUG.WriteLine("-- Shared Cache : Hit : [" + reqt_ + "] [0x" + block_addr_.ToString("X") + "]");
                    return true;
                }
            }

            //found none in cache
            miss++;
            if (Config.DEBUG_CACHE)
                DEBUG.WriteLine("-- Shared Cache : Miss : [" + reqt_ + "] [0x" + block_addr_.ToString("X") + "]");

            return false;
        }

        /// <summary>
        /// Remove target cache line.
        /// </summary>
        /// <param name="block_addr_">target block address</param>
        /// <returns></returns>
        public bool remove(UInt64 block_addr_)
        {
            cycle++;
            UInt64 index = block_addr_ % (uint)max_set;
            for (int i = 0; i < assoc; i++)
            {
                if (cache[i, index].block_addr == block_addr_)
                {

                    cache[i, index].block_addr = NULL;

                    cache[i, index].dirty = false;
                    cache[i, index].pid = 0;
                    cache[i, index].timestamp = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a cacheline into cache.
        /// </summary>
        /// <param name="block_addr_">target block address</param>
        /// <param name="reqt_">related request type</param>
        /// <param name="pid_">ID of process</param>
        /// <returns></returns>
        public UInt64 add(ulong block_addr_, RequestType reqt_, int pid_)
        {
            //make sure that block to add was not in the cache
            UInt64 res_addr = NULL;
            cycle++;
            int index = (int)(block_addr_ % (uint)max_set);

            int res_ass = -1;
            bool res = replace_policy.Calculate_Rep_Shared(assoc, index, cache, ref res_ass);
            if (res)
            {
                if (cache[0, index].dirty)
                    res_addr = cache[0, index].block_addr;
                for(int  i = 0; i < assoc - 1; i++){
                    cache[i, index].block_addr = cache[i+1, index].block_addr;
                    cache[i, index].dirty = cache[i + 1, index].dirty;
                    cache[i, index].pid = cache[i + 1, index].pid;
                }
                cache[assoc - 1, index].block_addr = block_addr_;
                if (reqt_ == RequestType.WRITE)
                    cache[assoc - 1, index].dirty = true;
                else
                    cache[assoc - 1, index].dirty = false;
                cache[assoc - 1, index].pid = pid_;
            }
            else
            {
                for (int i = res_ass; i < assoc - 1; i++)
                {
                    cache[i, index].block_addr = cache[i + 1, index].block_addr;
                    cache[i, index].dirty = cache[i + 1, index].dirty;
                    cache[i, index].pid = cache[i + 1, index].pid;
                    cache[i, index].valid = cache[i + 1, index].valid;
                }
                cache[assoc - 1, index].block_addr = block_addr_;
                if (reqt_ == RequestType.WRITE)
                    cache[assoc - 1, index].dirty = true;
                else
                    cache[assoc - 1, index].dirty = false;
                cache[assoc - 1, index].pid = pid_;
                cache[assoc - 1, index].valid = true;
            }
            return res_addr;
         

        }


        public bool ifdirty(UInt64 block_addr_)
        {
            cycle++;
            UInt64 index = block_addr_ % (uint)max_set;
            for (int i = 0; i < assoc; i++)
            {
                if (cache[i, index].block_addr == block_addr_)
                {

                    if (cache[i, index].dirty)

                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
        #endregion
    }
}
