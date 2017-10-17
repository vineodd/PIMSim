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
    /// Cache information class.
    /// This class records the information of a cache line.
    /// </summary>
    public class CacheEntity
    {
        #region Public Variables

        /// <summary>
        /// Block address
        /// </summary>
        public UInt64 block_addr;

        /// <summary>
        /// Served Cycle
        /// </summary>
        public UInt64 timestamp;

        /// <summary>
        /// Dirty bit
        /// </summary>
        public bool dirty;

        /// <summary>
        /// ID of Processors.
        /// Used in Shared Cache.
        /// </summary>
        public int pid;

        /// <summary>
        /// Valid bit
        /// </summary>
        public bool valid;

        #endregion

        #region Public Methods

        public CacheEntity(UInt64 block_, UInt64 timestamp_, bool dirty_, int pid_, bool via_)
        {
            block_addr = block_;
            timestamp = timestamp_;
            dirty = dirty_;
            pid = pid_;
            valid = via_;
        }

        #endregion
    }

    /// <summary>
    /// Cache Defination.
    /// </summary>
    public class Cache
    {
        #region Static Varables

        public readonly static UInt64 NULL = UInt64.MaxValue;

        #endregion

        #region Public Variables

        /// <summary>
        /// timestamp for LRU
        /// </summary>
        public UInt64 cycle;

        /// <summary>
        ///  total sets
        /// </summary>
        public int max_set;

        /// <summary>
        /// number of cache hits
        /// </summary>
        public UInt64 hits = 0;

        /// <summary>
        /// number of cache misses
        /// </summary>
        public UInt64 miss = 0;

        /// <summary>
        /// Cache entiry information collection.
        /// </summary>
        public CacheEntity[,] cache;

        /// <summary>
        /// Cache associativity
        /// </summary>
        public int assoc = 0;

        /// <summary>
        /// Cacheline Replcae Policy.
        /// </summary>
        public CacheReplacePolicy replace_policy;

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Function
        /// </summary>
        public Cache(bool pim)
        {
            replace_policy = (new LRU() as CacheReplacePolicy);

            cycle = 0;
            int set_size = 0;

            if (!pim)
            {
                assoc = Config.l1cache_assoc;
                set_size = Config.block_size * assoc;
                max_set = Config.l1cache_size / set_size;
                assoc = Config.l1cache_assoc;
            }
            else
            {
                assoc = PIMConfigs. l1cache_assoc;
                set_size = Config.block_size * assoc;
                max_set = PIMConfigs.l1cache_size / set_size;
                assoc = PIMConfigs.l1cache_assoc;
            }

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
            bool res = replace_policy.Calculate_Rep(assoc, index, cache, ref res_ass);
            if (Config.DEBUG_CACHE)
                DEBUG.WriteLine("-- Shared Cache : Load data : [" + reqt_ + "] [0x" + block_addr_.ToString("X") + "]");
            if (!res)
            {
                //empty entries
                cache[res_ass, index].block_addr = block_addr_;
                cache[res_ass, index].pid = pid_;
                cache[res_ass, index].timestamp = cycle;
                if (reqt_ == RequestType.WRITE)
                    cache[res_ass, index].dirty = true;
                else
                    cache[res_ass, index].dirty = false;
                if (Config.DEBUG_CACHE)
                    DEBUG.WriteLine("-- Shared Cache : Repleacement : Found empty Entry.");

            }
            else
            {
                //replacement
                if (cache[res_ass, index].dirty)
                    res_addr = cache[res_ass, index].block_addr;

                cache[res_ass, index].block_addr = block_addr_;
                cache[res_ass, index].pid = pid_;
                cache[res_ass, index].timestamp = cycle;
                if (reqt_ == RequestType.WRITE)
                    cache[res_ass, index].dirty = true;
                else
                    cache[res_ass, index].dirty = false;

                if (Config.DEBUG_CACHE)
                    DEBUG.WriteLine("-- Shared Cache : Repleacement : Found LRU Entry.");
            }

            return res_addr;
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
                if (cache[i, index].block_addr == block_addr_)
                {
                    //cache hit
                    hits++;
                    cache[i, index].timestamp = cycle;
                    if (reqt_ == RequestType.WRITE)
                        cache[i, index].dirty = true;
                    if (Config.DEBUG_CACHE)
                        DEBUG.WriteLine("-- L1Cache : Hit : [" + reqt_ + "] [0x" + block_addr_.ToString("X") + "]");
                    return true;
                }
            }

            //found none in cache
            miss++;
            if (Config.DEBUG_CACHE)
                DEBUG.WriteLine("-- L1Cache : Miss : [" + reqt_ + "] [0x" + block_addr_.ToString("X") + "]");
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
