#region using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;
using SimplePIM.Configs;
using SimplePIM.Memory;
using SimplePIM.General;
using SimplePIM.Memory.DDR;
using SimplePIM.Statics;
#endregion
namespace SimplePIM.PIM
{
    public class PIMProc : ComputationalUnit
    {

        public Cache L1Cache;
        public List<ProcRequest> MSHR;
        public List<ProcRequest> writeback_req;
        public int pid;

        public int IPC;
        public Mctrl mctrl;
        public ALU alu;
        private bool mctrl_ok = false;
        private bool mshr_ok = false;
        private InstructionBlock current_block = null;
        private Function current_function = null;

        public Queue<ProcRequest> cache_req_queue;
        public Queue<ProcRequest> mem_req_queue;
        private ProcRequest curr_req;
        private Instruction curr_ins;
        public PageConverter tlb;
        /// <summary>
        /// Try to find data in Shared Cache.
        /// If true, Shared cache encounters a miss. Try to add to mem_queue.
        /// </summary>
        private bool mctrl_retry = false;

        /// <summary>
        /// [Retry to add to MSHR]
        /// It sets true when MSHR is full.
        /// </summary>
        private bool mshr_retry = false;
        public InsPartition ins_p;
        // public TraceFetcher trace;
        public InstructionWindow ins_w;
        public Counter cal_restrict;
        public Counter mem_restrict;
        //for statics
        public UInt64 stall_cycle = 0;

        public void attach_tlb(ref PageConverter tlb_)
        {
            this.tlb = tlb_;
        }
        public void attach_memctrl(ref Mctrl ctrl_)
        {
            mctrl = ctrl_;
        }
        public bool add_to_mctrl(ProcRequest req_)
        {
            return mctrl.add_to_mctrl(req_);
        }

        public void handle_cache_req()
        {
            while (cache_req_queue.Count != 0)
            {
                ProcRequest req = cache_req_queue.Peek();
                if (req.ts_departure <= cycle)
                {
                    if (!L1Cache.search_block(req.block_addr, RequestType.READ))
                    {
                        L1Cache.add(req.block_addr, req.type, pid);
                    }
                    cache_req_queue.Dequeue();
                    MSHR.RemoveAll(x => x.block_addr == req.block_addr);
                    //  ins_w.can_operated(req.block_addr);
                    ins_w.set_ready(req.block_addr,this.cycle);
                }

            }
        }
        public bool write_b_stall()
        {
            return writeback_req.Count > Config.writeback_queue_size;
        }
        public bool handle_writeback_queue()
        {
            if (writeback_req.Count <= 0)
                return false;
            ProcRequest req = writeback_req[0];
            bool res = mctrl.add_to_mctrl(req);
            return res;
        }
        public PIMProc( ref InsPartition insp_, int pid_)
        {
            pid = pid_;
            ins_p = insp_;
            L1Cache = new Cache();
            //  Shared_Cache = new Cache(config, true);
            ins_w = new InstructionWindow(Config.ins_w_size, pid);

            IPC = Config.IPC;
            if(Config.pim_config.use_l1_cache)
                cache_req_queue = new Queue<ProcRequest>();
            MSHR = new List<ProcRequest>(Config.mshr_size);
            cal_restrict = new Counter(Config.IPC, Config.IPC);
            mem_restrict = new Counter(1, 1);
            alu = new ALU();
            tlb = new PageConverter();
            if (Config.pim_config.wb)
                writeback_req = new List<ProcRequest>();
            
        }
        public void reset_restrict()
        {
            cal_restrict = null;
            cal_restrict = new Counter(Config.IPC, Config.IPC);
            mem_restrict = null;
            mem_restrict = new Counter(1, 1);
        }
        public void update_ins_w()
        {
            if (ins_w.empty())
                return;
            int evicted = ins_w.evicted(Config.IPC);
            if (ins_w.if_stall() && evicted != Config.IPC)
            {
                stall_cycle++;
            }
        }
        public override void Step()
        {
            cycle++;
            reset_restrict();

            if (curr_ins == null || curr_req == null)
            {
                curr_ins = get_ins_from_insp();
                if (curr_req == null)
                    curr_req = new ProcRequest();
                curr_req.parse_ins(curr_ins);
            }
            if (!curr_ins.is_mem)
            {
                //current instruction is an alg ins or NOP
                while (cal_restrict.WaitOne())
                {
                    if (curr_ins.type == InstructionType.NOP)
                    {
                        continue;
                    }
                    else
                    {

                        alu.add_ins(curr_ins);

                    }
                }
            }
            alu.Step();
            if (Config.pim_config.use_l1_cache)
                handle_cache_req();
            update_ins_w();
            if (MSHR.Count != 0)
            {
                //Stat.procs[pid].memory_cycle.Collect();
                //memory_fraction_cycles++;
            }
            if (Config.wb)
            {
                //handle write-back queue
                if (writeback_req.Count > 0)
                {
                    //each step handles only one write-back req
                    bool res = handle_writeback_queue();
                    if (res)
                        writeback_req.RemoveAt(0);
                    res = write_b_stall();
                    if (res)
                    {
                        //too many writeback req to be handled
                        return;
                    }

                }

            }


            // if MSHR or MCTRL queue are full last cycyle , system has to process last request

            bool prcessed = false;
            if (mshr_ok || mctrl_ok)
            {
                if (ins_w.full())
                {
                    // Stat.procs[pid].stall_inst_wnd.Collect();
                    return;
                }


                //mshr/mctrl stall
                prcessed = handle_last();
                if (!prcessed)
                    return;

                //reissue success
                //  Dbg.Assert(!mshr_ok && !mctrl_ok);
                prcessed = true;
                curr_ins = null;
                curr_ins = get_ins_from_insp();
                curr_req = null;
                curr_req = new ProcRequest();
                curr_req.parse_ins(curr_ins);
            }
            if (curr_req.if_mem)

                handle_current_req();
            else
            {
                curr_ins = null;
                curr_ins = get_ins_from_insp();
                curr_req = null;
                curr_req = new ProcRequest();
                curr_req.parse_ins(curr_ins);

            }
        }
        public bool handle_last()
        {
            if (!curr_req.if_mem)
                return false;
            //retry mshr
            if (mshr_ok)
            {
                //   Dbg.Assert(!mctrl_retry);

                //retry mshr
                bool res = add_to_mshr(curr_req);
                if (!res)
                    return false;

                //success
                mshr_ok = false;

                //check if true miss
                bool false_miss = ins_w.if_exist(curr_req.block_addr);
                // Dbg.Assert(!false_miss);
                if (false_miss)
                    Console.Write("");

            }

            mctrl_ok = handle_last_req(curr_req);

            //retry mctrl
            if (mctrl_ok)
            {


                //retry mctrl
                bool mctrl_ok = add_to_mshr(curr_req);
                if (!mctrl_ok)
                    return false;

                //success
                this.mctrl_ok = false;
                //  Stat.procs[pid].l2_cache_miss_count.Collect();
                return true;
            }
            return true;

        }
        /// <summary>
        /// process current requests
        /// </summary>
        public void handle_current_req()
        {
            //if core had processed a memory operation, mem_restrct will be set to 0.
            //the loop will not be executed.
            while (mem_restrict.WaitOne())
            {
                if (ins_w.full())
                {
                    return;
                }
                bool hit = ins_w.if_exist(curr_req.block_addr);
                if (hit)
                {
                    bool ready = ins_w.get_readyinfo(curr_req.block_addr);

                    ins_w.add_ins(curr_ins, this.cycle);
                    ins_w.setLast(ready);
                    curr_ins = null;
                    curr_ins = get_ins_from_insp();
                    curr_req = null;
                    curr_req = new ProcRequest();
                    curr_req.parse_ins(curr_ins);
                    continue;
                }
                if (Config.use_cache)
                {
                    bool l1_hit = L1Cache.search_block(curr_req.block_addr, curr_req.type);
                    if (l1_hit)
                    {
                        //l1 cache hit

                        curr_ins.is_mem = true;
                        curr_ins.ready = true;
                        ins_w.add_ins(curr_ins, this.cycle);
                        curr_ins = null;
                        curr_ins = get_ins_from_insp();
                        curr_req = null;
                        curr_req = new ProcRequest();
                        curr_req.parse_ins(curr_ins);
                        continue;
                    }


                    bool mshr = add_to_mshr(curr_req);
                    if (!mshr)
                    {
                        mshr_retry = true;
                        return;
                    }
                    //l1 miss

                }
                curr_ins.is_mem = true;
                curr_ins.ready = false;
                ins_w.add_ins(curr_ins, this.cycle);

                bool mctrl_ = add_to_mctrl(curr_req);
                if (!mctrl_)
                {
                    mctrl_retry = true;
                    return;
                }

                curr_ins = null;
                curr_ins = get_ins_from_insp();


                curr_req = null;
                curr_req = new ProcRequest();
                curr_req.parse_ins(curr_ins);

            }
        }
        public bool add_to_mshr(ProcRequest req_)
        {
            if (MSHR.Count >= Config.mshr_size)
            {
                return false;
            }
            MSHR.Add(req_);
            return true;
        }
        public void add_to_cache(ProcRequest req_, bool if_shared = false)
        {
            req_.ts_departure = cycle + (if_shared ? Config.share_cache_hit_lantecy : Config.l1cache_hit_latency);
            cache_req_queue.Enqueue(req_);
            curr_ins.ready = false;
            curr_ins.is_mem = true;
            ins_w.add_ins(curr_ins,this.cycle);

        }
        public bool handle_last_req(ProcRequest req_)
        {
            //l1$ encounts miss
            curr_ins.is_mem = true;
            curr_ins.ready = false;
            ins_w.add_ins(curr_ins, this.cycle);
            return true;
        }
        public Instruction get_ins_from_insp()
        {

            if (current_block != null )
            {
                if (current_block != null)
                {
                    var item = current_block.get_ins(this.cycle);
                    if (item != null)
                        return item;
                    else
                    {
                        //serve time 
                        current_block = null;
                    }
                }
                
            }
            InputType tp = ins_p.get_req(this.pid, false);
            if (tp is Instruction)
            {
                if ((tp as Instruction).is_mem)
                    (tp as Instruction).block_addr = tlb.scan_page((tp as Instruction).address);
                return (tp as Instruction);
            }
            else
            {
                if (tp is InstructionBlock)
                {
                    current_block = tp as InstructionBlock;
                    var item= current_block.get_ins(this.cycle);
                    if (item == null)
                    {
                        DEBUG.Error("Block Instructions cannot be none!");
                        Environment.Exit(2);
                    }
                    return item;
                }
                else
                {
                   
                        //error 
                        Environment.Exit(1);
                        return null;
                    
                }
            }
        }

   
    }
}
