#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Statistics;
using PIMSim.Configs;

#endregion

namespace PIMSim.Procs
{
    public class InstructionWindow
    {
        #region Private Variables

        /// <summary>
        /// Queued instructions.
        /// </summary>
        private List<Instruction> ins;

        #endregion

        #region Public Variables

        /// <summary>
        /// ID
        /// </summary>
        public int id;

        /// <summary>
        /// max queue depth.
        /// </summary>
        public int size;

        #endregion

        #region Statistics Variables

        //for statistics
        public UInt64 total_loaded = 0;
        public UInt64 total_evicted = 0;
        public UInt64 stall = 0;

        public UInt64 total_read_latency = 0;
        public UInt64 total_write_latency = 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Functions
        /// </summary>
        /// <param name="size_">Max queue depth. </param>
        /// <param name="id_">ID</param>
        public InstructionWindow(int size_,int id_)
        {
            id = id_;
            size = size_;
            ins = new List<Instruction>(size);
        }

        /// <summary>
        /// Added instructions to INS_W.
        /// </summary>
        /// <param name="ins_">added instructions</param>
        /// <param name="cycle_">added cycle</param>
        public void add_ins(Instruction ins_,UInt64 cycle_)
        {
            if (Config.DEBUG_PROC)
                DEBUG.WriteLine("-- InsWd : Added Insts : " + ins_.ToString()+" ");
            ins_.served_cycle = cycle_;
            ins.Add(ins_);
            total_loaded++;
        }
        
        /// <summary>
        /// If current insts is completely processed.
        /// </summary>
        /// <returns></returns>
        public bool if_stall()
        {
            //if the earliest ins had not be handled
            if (ins.Count() <= 0)
                return false;
            return ins[0].ready;
        }

        /// <summary>
        /// Check if the same instructions had been loaded.
        /// </summary>
        /// <param name="block_addr_">target block address</param>
        /// <returns></returns>
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


        /// <summary>
        /// If queue is full
        /// </summary>
        /// <returns></returns>
        public bool full()
        {
            if (ins.Count > size)
            {
                stall++;
            }
            return ins.Count > size;
        }
        /// <summary>
        /// Queue empty
        /// </summary>
        /// <returns></returns>
        public bool empty()
        {
            return ins.Count == 0;
        }

        /// <summary>
        /// After memory operations are done, set it ready.
        /// </summary>
        /// <param name="block_addr_">target block address</param>
        /// <param name="cycle_">current cycle</param>
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

        /// <summary>
        /// set last item of queue.
        /// </summary>
        /// <param name="ready_"></param>
        public void setLast(bool ready_)
        {
            ins[ins.Count - 1].ready = ready_;
        }

        /// <summary>
        /// get ready info
        /// </summary>
        /// <param name="block_addr_"></param>
        /// <returns></returns>
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

        /// <summary>
        /// remove items
        /// </summary>
        /// <param name="i"></param>
        public void delete(int i=0)
        {
            ins.RemoveAt(i);

        }

        /// <summary>
        /// evicte oldest ins from instruction window
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
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

        #endregion
    }
}
