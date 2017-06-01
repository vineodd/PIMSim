using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.Statics;

namespace SimplePIM.Procs
{
    public class CacheEntity
    {
        public UInt64 block_addr;
        public UInt64 timestamp;
        public bool dirty;
        public int pid;
        public bool valid;

        public CacheEntity(UInt64 block_, UInt64 timestamp_, bool dirty_, int pid_,bool via_)
        {
            block_addr = block_;
            timestamp = timestamp_;
            dirty = dirty_;
            pid = pid_;
            valid = via_;
        }
    }
    public class Cache
    {
        public UInt64 cycle;        //artificial unit of time to timestamp blocks for LRU replacement
        public int max_set;       //total sets

        public UInt64 hits = 0;      //number of cache hits
        public UInt64 miss = 0;     //number of cache misses

        public CacheEntity[,] cache;

        public bool ifbusy = false;
        public readonly static UInt64 NULL = UInt64.MaxValue;
        public int assoc = 0;
        public CacheReplacePolicy replace_policy;

        public Cache()
        {
            replace_policy = (new LRU() as CacheReplacePolicy);

            

            cycle = 0;
            int set_size = 0;
            
            
                assoc = Config.l1cache_assoc;
                set_size = Config.block_size * assoc;
                max_set = Config.l1cache_size / set_size;
                assoc = Config.l1cache_assoc;
            

            cache = new CacheEntity[assoc, max_set];
    

            for(int i = 0; i < assoc; i++)
            {
                for(int j = 0; j < max_set; j++)
                {
                    cache[i, j] = new CacheEntity(NULL, 0, false, 0, false);
                }
            }

        }
        public UInt64 add(ulong block_addr_, RequestType reqt_, int pid_)
        {
            //make sure that block to add was not in the cache

            UInt64 res_addr = NULL;
            cycle++;
            int index = (int)(block_addr_ % (uint)max_set);

            int res_ass = -1;
            bool res = replace_policy.Calculate_Rep(assoc, index, cache,ref res_ass);
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
                
                if (cache[res_ass, index].dirty)
                    res_addr= cache[res_ass, index].block_addr;
              
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
        public bool search_block(UInt64 block_addr_,RequestType reqt_)
        {
            cycle++;
            
            UInt64 index = block_addr_ % (uint)max_set;

            for(int i = 0; i < assoc; i++)
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

    }
}
