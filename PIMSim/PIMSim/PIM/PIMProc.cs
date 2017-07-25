#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Procs;
using PIMSim.Configs;
using PIMSim.Memory;
using PIMSim.General;
using PIMSim.Memory.DDR;
using PIMSim.Statistics;
using mctrl = PIMSim.PIM.PIMMctrl;
using PIMSim.Partitioner;
#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// PIMProc
    /// </summary>
    public class PIMProc : ComputationalUnit
    {
        #region Private Variables
        private UInt64 pc = 0;
        /// <summary>
        /// [Private Level 1 Cache]
        /// </summary>
        public Cache L1Cache;

        /// <summary>
        /// [Miss-status Handling Registers] 
        /// </summary>
        private List<ProcRequest> MSHR;

        /// <summary>
        /// [WriteBack Queue]
        /// To process what LLC evites.
        /// </summary>
        private List<ProcRequest> writeback_req;

        /// <summary>
        /// [Instrcution Partitioner]
        /// Distrubute all the inputs to hosts or memorys.
        /// </summary>
        private InsPartition ins_p;

        /// <summary>
        /// [Processor IPC]
        /// </summary>
        private int IPC;

        private bool started = false;

        /// <summary>
        /// [Arithmetic Logic Unit]
        /// </summary>
        private ALU alu;


        /// <summary>
        /// [Add_to_Cache Queue]
        /// To simulate Cache miss latency
        /// </summary>
        private Queue<ProcRequest> cache_req_queue;


        /// <summary>
        /// [Instruction Window]
        /// OoO to process All ins in this processor.
        /// </summary>
        private InstructionWindow ins_w;


        /// <summary>
        /// [Current Request]
        /// </summary>
        private ProcRequest curr_req;

        /// <summary>
        /// [Current Instruction]
        /// </summary>
        private Instruction curr_ins;

        /// <summary>
        /// [Current Block]
        /// </summary>
        private InstructionBlock current_block = null;


        /// <summary>
        /// [Translation Lookaside Buffer]
        /// </summary>
        private PageConverter tlb;

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

        /// <summary>
        /// [Calculation restriction]
        /// Marks how many calcution this core can process in one CPU cycle.
        /// </summary>
        private Counter cal_restrict;

        /// <summary>
        /// [Memory Operation Restriction]
        /// Marks how many memory opeartion can be handed in one CPU cycle.
        /// </summary>
        private Counter mem_restrict;

        #endregion

        #region Public Variables

        /// <summary>
        /// [Process id]
        /// </summary>
        public int pid;

        /// <summary>
        /// [Memory Read Callback]
        /// Triger by a memory read operation complete 
        /// </summary>
        public new ReadCallBack read_callback;

        /// <summary>
        /// [Memory Write Callback]
        /// Triger by a memory write operation complate
        /// </summary>
        public new WriteCallBack write_callback;


        #endregion

        #region Statistics Variables
        //for statistics
        private UInt64 total_instruction => cal_ins + read_reqs + write_reqs + nop;
        private UInt64 cal_ins = 0;
        private UInt64 read_reqs = 0;
        private UInt64 write_reqs = 0;
        private UInt64 nop = 0;
        private UInt64 memory_reqs => read_reqs + write_reqs;
        private UInt64 mshr_loaded = 0;
        private UInt64 mshr_stalled = 0;
        private UInt64 mshr_count => mshr_stalled + mshr_loaded;
        private UInt64 stall_cycle = 0;
        private UInt64 memory_cycle = 0;
        private UInt64 total_read_latency => ins_w.total_read_latency;
        private UInt64 avg_read_latency => read_reqs != 0 ? total_read_latency / read_reqs : 0;
        private UInt64 avg_write_latency => write_reqs != 0 ? total_write_latency / write_reqs : 0;
        private UInt64 total_write_latency => ins_w.total_write_latency;
        private UInt64 total_latency => total_read_latency + total_write_latency;
        private UInt64 avg_latency => memory_reqs != 0 ? total_latency / memory_reqs : 0;
        private UInt64 l1cache_hit => L1Cache.hits;
        private UInt64 l1cache_miss => L1Cache.miss;
        private UInt64 l1cache_loaded => l1cache_hit + l1cache_miss;

        private UInt64 total_block_load = 0;
        private Dictionary<string, UInt64> block_count = new Dictionary<string, ulong>();
        private Dictionary<string, UInt64> block_latency = new Dictionary<string, ulong>();

        private UInt64 bandwidth_bit = 0;
        private double interal_bandwidth=>bandwidth_bit/ 8 //byte
                / 1024//KB
                / 1024//MB
                *1.0 / GlobalTimer.tick //MB/cycle
                * GlobalTimer.reference_clock;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add TLB to Proc.
        /// Used when shared TLB.
        /// </summary>
        /// <param name="tlb_">Linked TLB</param>
        public void attach_tlb(ref PageConverter tlb_)
        {
            this.tlb = tlb_;
        }



        /// <summary>
        /// Add a Processor Request to Memory Controller.
        /// </summary>
        /// <param name="req_">Processor Request</param>
        /// <returns></returns>
        public bool add_to_mctrl(ProcRequest req_)
        {
            bandwidth_bit += 64;
            return mctrl.add_to_mctrl(req_);
        }


        /// <summary>
        /// update cache
        /// </summary>
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
                    ins_w.set_ready(req.block_addr,this.cycle);
                }

            }
        }

        /// <summary>
        /// Get writeback statue.
        /// </summary>
        /// <returns></returns>
        public bool write_b_stall()
        {
            return writeback_req.Count > Config.writeback_queue_size;
        }

        /// <summary>
        /// Write Complete Callback
        /// </summary>
        /// <param name="block_addr">Block address</param>
        public bool handle_writeback_queue()
        {
            if (writeback_req.Count <= 0)
            {
                return false;
            }
            if (Config.DEBUG_PIM)
                DEBUG.WriteLine("--PIM Proc : Served WriteBack Reqs : [" + writeback_req[0].type + "] [0x" + writeback_req[0].actual_addr.ToString("X") + "]");
            ProcRequest req = writeback_req[0];
            return mctrl.add_to_mctrl(req);
        }

        /// <summary>
        /// Processor Construction Function
        /// </summary>
        /// <param name="insp_"> Shared Instruction Partitioner </param>
        /// <param name="pid_"> Processor ID</param>
        public PIMProc( ref InsPartition insp_, int pid_)
        {
            pid = pid_;
            ins_p = insp_;
            L1Cache = new Cache(true);
            //  Shared_Cache = new Cache(config, true);
            ins_w = new InstructionWindow(Config.ins_w_size, pid);

            IPC = PIMConfigs.IPC;
            if(PIMConfigs.use_l1_cache)
                cache_req_queue = new Queue<ProcRequest>();
            MSHR = new List<ProcRequest>(Config.mshr_size);
            cal_restrict = new Counter(Config.IPC, Config.IPC);
            mem_restrict = new Counter(1, 1);
            alu = new ALU();
            tlb = new PageConverter();
            if (PIMConfigs.writeback)
                writeback_req = new List<ProcRequest>();
            //init callback
            read_callback = new ReadCallBack(handle_read_callback);
            write_callback = new WriteCallBack(handle_write_callback);
        }

        /// <summary>
        /// Reset restriction of memory and calculation
        /// </summary>
        public void reset_restrict()
        {
            cal_restrict = null;
            cal_restrict = new Counter(Config.IPC, Config.IPC);
            mem_restrict = null;
            mem_restrict = new Counter(1, 1);
        }

        /// <summary>
        /// update instruction window
        /// </summary>
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

        public override bool outstanding_requests()
        {
            return MSHR.Count == 0;
        }
        public override bool done()
        {
            return outstanding_requests() && curr_ins.type == InstructionType.NOP;
        }
        /// <summary>
        /// One cycle of Core.
        /// </summary>
        public override void Step()
        {
            cycle++;
            if (Config.DEBUG_PIM)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("----------PIM CPU [" + this.pid + "] Update [Cycle " + cycle + "]------------");
                DEBUG.WriteLine();

            }
            //reset all restriction
            reset_restrict();

            //period statics
            if (cycle % Config.pim_static_period == 0 && cycle != 0)
            {
                //static 
            }

            //init current request and instruction when cycle 1.
            //otherwise current request and instruction cannot be null.
            if (curr_ins == null || curr_req == null)
            {
                curr_ins = get_ins_from_insp();
                if (!started)
                {
                    if (curr_ins.type == InstructionType.NOP)
                    {
                            return;
                    }
                    else
                    {
                        started = true;
                    }
                }
                if (curr_req == null)
                    curr_req = new ProcRequest();
                curr_req.parse_ins(curr_ins);
            }
            if (Config.trace_type == Trace_Type.PC)
            {
                pc++;
                Console.WriteLine(pc.ToString("x"));
                //if (curr_req.type != RequestType.NOP)
                //{
                //    if (pc > curr_req.pc)
                //    {
                //        pc = curr_req.pc;
                //    }
                //}
            }
            if (Config.sim_type == SIM_TYPE.cycle)
            {
                //simulater has reach max sim cysle,exit
                if (cycle > Config.sim_cycle) { return; }
            }
            if (Config.trace_type != Trace_Type.PC)
            {
                //if no memory operation, insert ins to ALU
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
            }
            if (PIMConfigs.use_l1_cache)
                handle_cache_req();

            update_ins_w();

            if (outstanding_requests())
            {
                memory_cycle++;
            }
            if (Config.writeback)
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
            if (mshr_retry || mctrl_retry)
            {
                if (ins_w.full())
                {

                    if (Config.DEBUG_PROC)
                    {
                        DEBUG.WriteLine("-- InsWd : Queue Full.");
                    }
                    return;
                }


                //mshr/mctrl stall
                prcessed = handle_last();
                if (!prcessed)
                    return;

                //reissue success

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

        /// <summary>
        /// Retry last requests
        /// </summary>
        /// <returns></returns>
        public bool handle_last()
        {
            if (!curr_req.if_mem)
                return false;
            //retry mshr
            if (mshr_retry)
            {

                //retry mshr
                bool res = add_to_mshr(curr_req);
                if (!res)
                    return false;

                //success
                mshr_retry = false;

                //check if true miss
                if (ins_w.if_exist(curr_req.block_addr))
                {
                    if (Config.DEBUG_PIM)
                    {
                        DEBUG.WriteLine("-- InsWd : Instruction had been loaded.");
                    }
                }


            }

            handle_last_req(curr_req);



            bool mctrl_ok = add_to_mshr(curr_req);
            if (!mctrl_ok)
                return false;

            //success
            this.mctrl_retry = false;

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
                if (PIMConfigs.use_l1_cache)
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

        /// <summary>
        /// Add to MSHR
        /// </summary>
        /// <param name="req_">Processor Request</param>
        /// <returns>False when MSHR is full; else true.</returns>
        public bool add_to_mshr(ProcRequest req_)
        {
            if (MSHR.Count >= Config.mshr_size)
            {
                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("-- MSHR : Failed to add Req to MSHR.");
                mshr_stalled++;
                return false;
            }
            mshr_loaded++;
            MSHR.Add(req_);
            if (Config.DEBUG_PIM)
                DEBUG.WriteLine("-- MSHR : New Entry : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X") + "]");
            return true;
        }

        /// <summary>
        /// Add to Cache
        /// Foreach requests, add lantency
        /// </summary>
        /// <param name="req_">Processor Request</param>
        public void add_to_cache(ProcRequest req_)
        {
            req_.ts_departure = cycle + Config.l1cache_hit_latency;
            cache_req_queue.Enqueue(req_);
            curr_ins.ready = false;
            curr_ins.is_mem = true;
            ins_w.add_ins(curr_ins,this.cycle);
        }

        /// <summary>
        /// Handle last requests
        /// </summary>
        /// <param name="req_">Processor Requests</param>
        /// <returns>return false when l1$ hits </returns>
        public bool handle_last_req(ProcRequest req_)
        {
            //l1$ encounts miss
            curr_ins.is_mem = true;
            curr_ins.ready = false;
            ins_w.add_ins(curr_ins, this.cycle);
            return true;
        }

        /// <summary>
        /// Get inputs from instruction partitioner.
        /// We assume that host core can only process intructions(Not Functions and InstrcutionBlocks).
        /// When core encountered a non-instruction, return error.
        /// </summary>
        /// <returns>Instruction to be processed. If none, NOP.</returns>
        public Instruction get_ins_from_insp()
        {
            //current block has un-processed instructions
            if (current_block != null)
            {
                //get current instructions
                var item = current_block.get_ins(this.cycle);
                if (item != null)
                {
                    bandwidth_bit = bandwidth_bit + item.Length();
                    return item;
                }
                else
                {
                    //current block is empty
                    if (block_latency.Any(s => s.Key == current_block.name))
                    {
                        block_latency[current_block.name] += cycle - current_block.servetime;
                    }
                    block_latency.Add(current_block.name, cycle - current_block.servetime);
                    current_block = null;
                }
            }

            Input tp;
            if (Config.trace_type == Trace_Type.PC)
                tp = ins_p.get_req(this.pid, false, this.pc);
            else
                tp = ins_p.get_req(this.pid, false);
            if (tp is Instruction)
            {
                switch ((tp as Instruction).type)
                {
                    case InstructionType.READ:
                        read_reqs++;
                        break;
                    case InstructionType.WRITE:
                        write_reqs++;
                        break;
                    case InstructionType.NOP:
                        nop++;
                        break;
                    case InstructionType.CALCULATION:
                        cal_ins++;
                        break;
                    default:
                        break;
                }

                if ((tp as Instruction).is_mem)
                    (tp as Instruction).block_addr = tlb.scan_page((tp as Instruction).address);

                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("-- Current Instruction : " + (tp as Instruction).ToString());
                bandwidth_bit = bandwidth_bit + (tp as Instruction).Length();
                return (tp as Instruction);
            }
            else
            {
                if (tp is InstructionBlock)
                {
                    total_block_load++;
                    current_block = tp as InstructionBlock;
                    var item = current_block.get_ins(this.cycle);
                    if (item == null)
                    {
                        DEBUG.Error("Block Instructions cannot be none!");
                        Environment.Exit(2);
                    }
                    if (block_count.Any(s => s.Key == current_block.name))
                        block_count[current_block.name]++;
                    else
                        block_count.Add(current_block.name, 1);
                    if (Config.DEBUG_PIM)
                        DEBUG.WriteLine("-- Fetched Block : " + (tp as InstructionBlock).ToString());
                    bandwidth_bit = bandwidth_bit + (item as Instruction).Length();
                    return item;
                }
                else
                {
                    if (tp is PCTrace)
                    {
                        pc = (tp as PCTrace).PC - 1;
                        var res = (tp as PCTrace).parsetoIns();
                        res.block_addr = tlb.scan_page(res.address);
                        return res;
                    }
                    else
                    {
                        if (Config.DEBUG_PIM)
                            DEBUG.Error("-- Receieved a FUNCTION Input.");
                        Environment.Exit(Error.InputArgsError);
                        return null;
                    }

                }
            }
        }

        /// <summary>
        /// Read Complete Callback
        /// </summary>
        /// <param name="block_addr">Block address</param>
        /// <param name="addr_">Actual address</param>
        public void handle_read_callback(CallBackInfo callback)
        {

            var block_addr = callback.block_addr;
            var addr_ = callback.address;
            if (Coherence.consistency == Consistency.SpinLock)
                Coherence.spin_lock.relese_lock(addr_);
            ins_w.set_ready(block_addr, this.cycle);
            MSHR.RemoveAll(x => x.block_addr == block_addr);
            if (PIMConfigs.use_l1_cache)
            {
                if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                {
                    L1Cache.add(block_addr, General.RequestType.READ, this.pid);

                }
                bandwidth_bit = bandwidth_bit + PIMConfigs.l1_cacheline_size * 8;
            }
            Coherence.spin_lock.relese_lock(callback.address);
        }

        /// <summary>
        /// Write Complete Callback
        /// </summary>
        /// <param name="block_addr">Block address</param>
        public void handle_write_callback(CallBackInfo callback)
        {
            var block_addr = callback.block_addr;
            var addr_ = callback.address;
            //if (Coherence.consistency == Consistency.SpinLock)
            //    Coherence.spin_lock.relese_lock(addr_);
            bandwidth_bit = bandwidth_bit +  128;
            ins_w.set_ready(block_addr, this.cycle);
            MSHR.RemoveAll(x => x.block_addr == block_addr);
            //update cache

            if (PIMConfigs.use_l1_cache)
            {
                if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                {
                    L1Cache.add(block_addr, General.RequestType.READ, this.pid);
                }
                bandwidth_bit = bandwidth_bit + PIMConfigs.l1_cacheline_size * 8;
            }

            Coherence.spin_lock.relese_lock(callback.address);


        }

        /// <summary>
        /// Print Statics.
        /// </summary>
        public override void PrintStatus()
        {
            DEBUG.WriteLine("=====================PIM Processor [" + pid + "] Statistics=====================");
            DEBUG.WriteLine();
            DEBUG.WriteLine("        Total Instructions Served : " + total_instruction);
            DEBUG.WriteLine("        Read Instructions         : " + read_reqs);
            DEBUG.WriteLine("        Write Instrcutions        : " + write_reqs);
            DEBUG.WriteLine("        Calculation Instructions  : " + cal_ins);
            DEBUG.WriteLine("        NOP Instructions          : " + nop);
            DEBUG.WriteLine();
            DEBUG.WriteLine("        MSHR Loaded               : " + mshr_loaded);
            DEBUG.WriteLine("        MSHR Stalled              : " + mshr_stalled);
            DEBUG.WriteLine("        MSHR Total                : " + mshr_count);
            DEBUG.WriteLine();
            DEBUG.WriteLine("        Simulated Cycle           : " + cycle);
            DEBUG.WriteLine("        Memory Cycle              : " + memory_cycle);
            DEBUG.WriteLine();
            DEBUG.WriteLine("        Average Read Latency      : " + avg_read_latency);
            DEBUG.WriteLine("        Average Write Latency     : " + avg_write_latency);
            DEBUG.WriteLine("        Average Memory Latency    : " + avg_latency);
            if (Config.use_cache)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("        L1 Cache Hit              : " + l1cache_hit);
                DEBUG.WriteLine("        L1 Cache Miss             : " + l1cache_miss);
                DEBUG.WriteLine("        L1 Cache Total Loaded     : " + l1cache_loaded);
            }
            DEBUG.WriteLine(" Internal Bandwidth :" + interal_bandwidth);
            foreach(var item in block_count)
            {
                DEBUG.WriteLine("------------BLOCK "+item.Key+ "Statistics -------------");
                DEBUG.WriteLine("                Count : " + item.Value);
                DEBUG.WriteLine("      Average Latency : " + block_latency[item.Key] * 1.0 / item.Value);
                DEBUG.WriteLine();
            }
            
        }
        #endregion
    }
}
