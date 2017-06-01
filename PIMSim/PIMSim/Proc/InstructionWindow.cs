using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;
using SimplePIM.Statics;
using SimplePIM.Configs;

namespace SimplePIM.Procs
{
    public class InstructionWindow
    {
        
        public int id;
        public int size;
        public List<Instruction> ins;


        //for statics
        public UInt64 total_loaded = 0;
        public UInt64 total_evicted = 0;
        public UInt64 stall = 0;

        public UInt64 total_read_latency = 0;
        public UInt64 total_write_latency = 0;

        public InstructionWindow(int size_,int id_)
        {
            id = id_;
            size = size_;
            ins = new List<Instruction>(size);

        }
        public void add_ins(Instruction ins_,UInt64 cycle_)
        {
            //ins window always keep all entries full
            //   ins.RemoveAt(0);
            if (Config.DEBUG_PROC)
                DEBUG.WriteLine("-- InsWd : Added Insts : " + ins_.ToString()+" ");
            ins_.served_cycle = cycle_;
            ins.Add(ins_);
            total_loaded++;

        }
 
        public bool if_stall()
        {
            //if the earliest ins had not be handled
            if (ins.Count() <= 0)
                return false;
            return ins[0].ready;
        }

        public bool if_exist(UInt64 block_addr_)
        {
            foreach (Instruction x in ins)
            {
                if (x.address == block_addr_ && x.is_mem)
                {
                    if(Config.DEBUG_PROC)
                    {
                        DEBUG.WriteLine("-- InsWd : Hit : [0x" + block_addr_.ToString("X") + "]");
                    }
                    return true;
                }
            }
            return false;
        }

        public bool full()
        {
            if (ins.Count > size)
            {
                stall++;
            }
            return ins.Count > size;
        }
        public bool empty()
        {
            return ins.Count == 0;
        }
        public void set_ready(UInt64 block_addr_,UInt64 cycle_)
        {
            foreach (Instruction x in ins)
            {
                if (x.block_addr == block_addr_ && x.is_mem)
                {
                    switch (x.type)
                    {
                        case InstructionType.READ:
                            total_read_latency += cycle_ - x.served_cycle;
                            break;
                        case InstructionType.WRITE:
                            total_write_latency += cycle_ - x.served_cycle;
                            break;
                        default:
                            break;
                    }
                    x.ready = true;

                }
            }


        }
        public void setLast(bool ready_)
        {
            ins[ins.Count - 1].ready = ready_;
        }
        public bool get_readyinfo(UInt64 block_addr_)
        {
            foreach (Instruction x in ins)
            {
                if (x.block_addr == block_addr_ && x.is_mem)
                {
                    
                    return x.ready;
                }
            }

            return false;
        }
        public void delete(int i=0)
        {
            ins.RemoveAt(i);
            
       
        }
        //evicte oldest ins from instruction window
        public int evicted(int n)
        {
            if (ins.Count() <= 0)
                return 0;
            int res = 0;
            for(int i=0;i< n; i++)
            {
                if (ins.Count() <= 0||!ins[i].ready)
                {
                    break;
                }
                delete();
                i--;
                total_evicted++;
                res++;


            }
            return res;
        }
    }
}
