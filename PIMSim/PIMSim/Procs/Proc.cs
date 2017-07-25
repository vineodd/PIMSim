#region Referance
using System;
using System.Collections.Generic;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Statistics;
using PIMSim.PIM;
using mctrl = PIMSim.Procs.Mctrl;
using PIMSim.Partitioner;
using PIMSim.General.Ports;
using PIMSim.General.Protocols;
using System.Linq;


#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// This Class performs as a CPU Core.
    /// </summary>
    public class Proc : SimulatorObj
    {
        #region Private Variables

        private UInt64 pc = 0;
        /// <summary>
        /// [Private Level 1 Cache]
        /// </summary>
        private Cache L1Cache;

        /// <summary>
        /// If enbale shared cache, this class will be initialed, or it will be null.
        /// Varible is set in OverallConfig.shared_cache.
        /// </summary>
        private Shared_Cache shared_cache;

        /// <summary>
        /// [Miss-status Handling Registers] 
        /// </summary>
        private List<ProcRequest> MSHR;

        /// <summary>
        /// [WriteBack Queue]
        /// To process what LLC evites.
        /// </summary>
        private List<ProcRequest> writeback_req;


        private List<Register> registers = new List<Register>();


        /// <summary>
        /// [Instrcution Partitioner]
        /// Distrubute all the inputs to hosts or memorys.
        /// </summary>
        private InsPartition ins_p;

        /// <summary>
        /// [Instruction Window]
        /// OoO to process All ins in this processor.
        /// </summary>
        private InstructionWindow ins_w;

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

        /// <summary>
        /// [Processor IPC]
        /// </summary>
        private int IPC;

        /// <summary>
        /// [Arithmetic Logic Unit]
        /// </summary>
        private ALU alu;

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
        /// [Add_to_Cache Queue]
        /// To simulate Cache miss latency
        /// </summary>
        private Queue<ProcRequest> cache_req_queue;

        /// <summary>
        /// [Current Request]
        /// </summary>
        private ProcRequest curr_req;

        /// <summary>
        /// [Current Instruction]
        /// </summary>
        private Instruction curr_ins;

        /// <summary>
        /// [Translation Lookaside Buffer]
        /// </summary>
        private PageConverter tlb;

        #endregion

        #region Statistics Variables

        //For statics
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
        private UInt64 scache_hit => shared_cache.hits;
        private UInt64 scache_miss => shared_cache.miss;
        private UInt64 scache_loaded => scache_hit + scache_miss;

        private UInt64 total_flushed = 0;
        #endregion

        #region Public Variables

        public InspCPUSlavePort ins_port;


        /// <summary>
        /// [Process id]
        /// </summary>
        public int pid;

        /// <summary>
        /// [Memory Read Callback]
        /// Triger by a memory read operation complete 
        /// </summary>
        public ReadCallBack read_callback;

        /// <summary>
        /// [Memory Write Callback]
        /// Triger by a memory write operation complate
        /// </summary>
        public WriteCallBack write_callback;

        #endregion

        #region Public Methods

        /// <summary>
        /// Add Shared Cache to Proc
        /// </summary>
        /// <param name="cache_">Linked Shared Cache</param>
        public void attach_shared_cache(ref Shared_Cache cache_)
        {
            this.shared_cache = cache_;
        }

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
            return mctrl.add_to_mctrl(req_);
        }

        /// <summary>
        /// Processor Construction Function
        /// </summary>
        /// <param name="insp_"> Shared Instruction Partitioner </param>
        /// <param name="pid_"> Processor ID</param>
        public Proc(ref InsPartition insp_, int pid_)
        {
            //passing parameters 
            pid = pid_;
            ins_p = insp_;
            IPC = Config.IPC;

            //init private cache
            L1Cache = new Cache(false);

            //init instruction window
            ins_w = new InstructionWindow(Config.ins_w_size, pid);

            //queue init
            if (Config.use_cache)
                cache_req_queue = new Queue<ProcRequest>();
            MSHR = new List<ProcRequest>(Config.mshr_size);
            if (Config.writeback)
                writeback_req = new List<ProcRequest>();

            //restrict
            cal_restrict = new Counter(Config.IPC, Config.IPC);
            mem_restrict = new Counter(1, 1);

            //alu
            alu = new ALU();

            //init callback
            read_callback = new ReadCallBack(handle_read_callback);
            write_callback = new WriteCallBack(handle_write_callback);

            ins_port = new InspCPUSlavePort("CPU Insrtuction Cache", PortManager.Allocate());
            ins_port.owner = this;
        }


        /// <summary>
        /// Add to MSHR
        /// </summary>
        /// <param name="req_">Processor Request</param>
        /// <returns>False when MSHR is full; else true.</returns>
        public bool add_to_mshr(ProcRequest req_)
        {
            if (MSHR.Exists(x => x.actual_addr == req_.actual_addr && x.block_addr == req_.block_addr))
            {
                for (int i = 0; i < MSHR.Count; i++)
                {
                    if (MSHR[i].actual_addr == req_.actual_addr && MSHR[i].block_addr == req_.block_addr)
                    {
                        if (Config.DEBUG_PROC)
                            DEBUG.WriteLine("-- MSHR : Merge Reqs : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X") + "]");
                        return true;
                    }
                }
            }

            if (MSHR.Count > Config.mshr_size)
            {
                if (Config.DEBUG_PROC)
                    DEBUG.WriteLine("-- MSHR : Failed to add Req to MSHR.");
                mshr_stalled++;
                return false;
            }
            mshr_loaded++;
            MSHR.Add(req_);
            if (Config.DEBUG_PROC)
                DEBUG.WriteLine("-- MSHR : New Entry : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X") + "]");
            return true;
        }

        /// <summary>
        /// Process WriteBack Queue.
        /// Add writeback requests to memory.
        /// </summary>
        /// <returns>No writeback requests, return false;Added to memory controller,return true; else false;</returns>
        public bool handle_writeback_queue()
        {
            if (writeback_req.Count <= 0)
            {
                return false;
            }
            if (Config.DEBUG_PROC)
                DEBUG.WriteLine("-- Proc : Served WriteBack Reqs : [" + writeback_req[0].type + "] [0x" + writeback_req[0].actual_addr.ToString("X") + "]");
            ProcRequest req = writeback_req[0];
            return mctrl.add_to_mctrl(req);

        }

        /// <summary>
        /// Add to Cache
        /// Foreach requests, add lantency
        /// </summary>
        /// <param name="req_">Processor Request</param>
        /// <param name="if_shared"> if shared cache</param>
        public void add_to_cache(ProcRequest req_, bool if_shared = false)
        {
            req_.ts_departure = cycle + (if_shared ? Config.share_cache_hit_latecy : Config.l1cache_hit_latency);
            cache_req_queue.Enqueue(req_);
            curr_ins.ready = false;
            curr_ins.is_mem = true;
            if (Config.DEBUG_PROC)
                DEBUG.WriteLine("CPU [" + this.pid + "] : Add Reqs to Cache_Queue : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X") + "]");
            ins_w.add_ins(curr_ins, this.cycle);
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
                    ins_w.set_ready(req.block_addr, this.cycle);

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
            ins_w.set_ready(block_addr, this.cycle);
            MSHR.RemoveAll(x => x.block_addr == block_addr);

            //update cache
            if (Config.use_cache)
            {
                if (Config.shared_cache)
                {
                    UInt64 addr = NULL;
                    if (!shared_cache.search_block(block_addr, General.RequestType.READ))
                    {
                        addr = shared_cache.add(block_addr, General.RequestType.READ, this.pid);
                        if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                        {
                            L1Cache.add(block_addr, General.RequestType.READ, this.pid);
                        }
                        L1Cache.remove(addr);
                    }

                    if (addr != NULL)
                    {
                        var item = new General.ProcRequest();
                        item.block_addr = addr;
                        item.type = General.RequestType.WRITE;
                        item.pid = pid;
                        bool wb_merge = writeback_req.Exists(x => x.block_addr == item.block_addr);
                        if (!wb_merge)
                            writeback_req.Add(item);
                    }
                }
                else
                {
                    //only l1cache
                    if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                    {
                        L1Cache.add(block_addr, General.RequestType.READ, this.pid);
                    }
                }
            }
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
            ins_w.set_ready(block_addr, this.cycle);
            MSHR.RemoveAll(x => x.block_addr == block_addr);
            //update cache
            if (Config.use_cache)
            {
                if (Config.shared_cache)
                {
                    UInt64 addr = NULL;
                    if (!shared_cache.search_block(block_addr, General.RequestType.READ))
                    {
                        addr = shared_cache.add(block_addr, General.RequestType.WRITE, this.pid);
                        if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                        {
                            L1Cache.add(block_addr, General.RequestType.WRITE, this.pid);
                        }
                        L1Cache.remove(addr);
                    }

                    if (addr != NULL)
                    {
                        var item = new General.ProcRequest();
                        item.block_addr = addr;
                        item.type = General.RequestType.WRITE;
                        item.pid = pid;
                        bool wb_merge = writeback_req.Exists(x => x.block_addr == item.block_addr);
                        if (!wb_merge)
                            writeback_req.Add(item);
                    }
                }
                else
                {
                    if (!L1Cache.search_block(block_addr, General.RequestType.READ))
                    {
                        L1Cache.add(block_addr, General.RequestType.WRITE, this.pid);
                    }
                }
            }
        }

        /// <summary>
        /// update instruction window
        /// </summary>
        public void update_ins_w()
        {
            if (ins_w.empty())
                return;
            //evicted instruction window
            int evicted = ins_w.evicted(Config.IPC);
            if (ins_w.if_stall() && evicted != Config.IPC)
            {
                stall_cycle++;
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
        /// Handle last requests
        /// </summary>
        /// <param name="req_">Processor Requests</param>
        /// <returns>return false when l1$ hits </returns>
        public bool handle_last_req(ProcRequest req_)
        {
            //l1$ encounters miss
            bool hit = false;
            if (Config.use_cache)
            {
                if (Config.shared_cache)
                {
                    hit = shared_cache.search_block(req_.block_addr, req_.type);
                    if (hit)
                    {
                        //found data in shared cache
                        add_to_cache(curr_req, true);
                        return false;
                    }
                }
            }
            curr_ins.is_mem = true;
            curr_ins.ready = false;
            ins_w.add_ins(curr_ins, this.cycle);
            return true;
        }

        /// <summary>
        /// Retry last requests
        /// </summary>
        /// <returns></returns>
        public bool handle_last()
        {
            if (!curr_req.if_mem)
                return false;

            if (mshr_retry)
            {
                bool res = add_to_mshr(curr_req);
                if (!res)
                    return false;

                //success
                mshr_retry = false;

                if (ins_w.if_exist(curr_req.block_addr))
                {
                    if (Config.DEBUG_PROC)
                    {
                        DEBUG.WriteLine("-- InsWd : Instruction had been loaded.");
                    }
                }



            }

            mctrl_retry = handle_last_req(curr_req);

            //retry mctrl
            if (mctrl_retry)
            {
                //retry mctrl
                bool mctrl_ok = add_to_mshr(curr_req);
                if (!mctrl_ok)
                    return false;

                //success
                this.mctrl_retry = false;

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
                     HandleNewRequest();
                   // Clear();
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
                        HandleNewRequest();
               
                        continue;
                    }


                    bool mshr = add_to_mshr(curr_req);
                    if (!mshr)
                    {
                        mshr_retry = true;
                        return;
                    }
                    //l1 miss
                    if (Config.shared_cache)
                    {
                        bool shared_hit = shared_cache.search_block(curr_req.block_addr, curr_req.type);
                        if (shared_hit)
                        {
                            //shared_hit

                            add_to_cache(curr_req);
                            HandleNewRequest();
                           
                            continue;
                        }
                    }

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

                HandleNewRequest();
      
            }
        }

        /// <summary>
        /// Flush cacheline
        /// </summary>
        /// <param name="addr">Block address</param>
        public bool flush(UInt64 addr, bool actual = false)
        {
            var address = addr;
            if (actual)
                address = tlb.scan_page(addr);
            if (!Config.use_cache)
                return true;
            if (Coherence.flush_queue.Contains(addr))
            {
                return false;
            }
            if (L1Cache.ifdirty(addr) || (Config.shared_cache ? shared_cache.ifdirty(addr) : false))
            {
                ProcRequest item = new ProcRequest();
                item.block_addr = addr;
                item.actual_addr = tlb.scan_page(addr);
                item.cycle = GlobalTimer.tick;
                item.if_mem = true;
                item.pid = this.pid;
                item.type = RequestType.FLUSH;
                item.ready = false;
                if (Config.writeback)
                    writeback_req.Add(item);
                else
                    add_to_mctrl(item);

                total_flushed++;
                L1Cache.remove(addr);
                if (Config.shared_cache)
                {
                    shared_cache.remove(addr);
                }
                return false;
            }
            return true;



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

        public bool outstanding_requests()
        {
            return MSHR.Count == 0;
        }






        /// <summary>
        /// Get instruction from instruction partitioner.
        /// We assume that host core can only process intructions(Not Functions and InstrcutionBlocks).
        /// When core encountered a non-instruction, return error.
        /// </summary>
        /// <returns>Instruction to be processed. If none, NOP.</returns>
        public Instruction get_ins_from_insp()
        {
            Input tp;
            if (Config.trace_type == Trace_Type.PC)
                tp = ins_p.get_req(this.pid, true, this.pc);
            else
                tp = ins_p.get_req(this.pid, true);

            //indentify if input is an instruction
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
                //return instruction
                if ((tp as Instruction).is_mem)
                {
                    (tp as Instruction).block_addr = tlb.scan_page((tp as Instruction).address);

                }

                if (Config.DEBUG_PROC)
                    DEBUG.WriteLine("-- Current Instruction : " + (tp as Instruction).ToString());
                return (tp as Instruction);
            }
            else
            {
                if (tp is PCTrace)
                {


                    pc = (tp as PCTrace).PC-1;
                    var res = (tp as PCTrace).parsetoIns();
                    res.block_addr = tlb.scan_page(res.address);
                    return res;

                }
                else
                {
                    //Host processor can only process Instructions
                    if (Config.DEBUG_PROC)
                        DEBUG.Error("-- Receieved a none Instrcution Input.");
                    Environment.Exit(Error.InputArgsError);
                    return null;
                }
            }
        }
        public void HandleNewRequest()
        {
            curr_ins = null;
            curr_ins = get_ins_from_insp();
            curr_req = null;
            curr_req = new ProcRequest();
            curr_req.parse_ins(curr_ins);
        }
        public bool done()
        {
            return outstanding_requests() && curr_ins.type == InstructionType.NOP;
        }
        /// <summary>
        /// One cycle of Core.
        /// </summary>
        public override void Step()
        {

            cycle++;
            if (Config.DEBUG_PROC)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("----------Host CPU [" + this.pid + "] Update [Cycle " + cycle + "]------------");
                DEBUG.WriteLine();
            }
            //if (Config.trace_type == Trace_Type.PC)
            //{
            //    if (curr_req != null && curr_req.pc > 0 && pc == 0)
            //        pc = curr_req.pc;
            //    else
            //    {
            //        get_ins_from_insp();
            //        return;
            //    }
            //}
            /**
                * Free all the restricts to enable a new round of CPU cycles.
            **/
            reset_restrict();   //reset all restriction

            //period statics
            if (cycle % Config.proc_static_period == 0 && cycle != 0)
            {
                //static 
            }

            //init current request and instruction when cycle 1.
            //otherwise current request and instruction cannot be null.
           

            if (curr_ins == null || curr_req == null)
            {
                curr_ins = get_ins_from_insp();
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
                
                /**
                    * In PC trace mode, CPU only simulates cache and memory 
                    * behaviours. ALU should be disabled due to the lack of 
                    * detailed instruction information. Because that the trace 
                    * file is fetched by physical mechines, which provide 
                    * the correctness of execution. PIMSim just needs to 
                    * send memory or cache requests at exact time.
                **/
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
            if (Config.use_cache)
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


                mem_restrict.WaitOne();
                HandleNewRequest();
            }
            if (curr_req.if_mem)

                handle_current_req();
            else
            {
                HandleNewRequest();
            }
        }
        public void Clear()
        {
            curr_ins = null;
            curr_req = null;
        }
        /// <summary>
        /// Print Statics.
        /// </summary>
        public void PrintStatus()
        {
            DEBUG.WriteLine("=====================Processor [" + pid + "] Statistics=====================");
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
            DEBUG.WriteLine("         Total Flushed Reqs       : " + total_flushed);
            if (Config.use_cache)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("        L1 Cache Hit              : " + l1cache_hit);
                DEBUG.WriteLine("        L1 Cache Miss             : " + l1cache_miss);
                DEBUG.WriteLine("        L1 Cache Total Loaded     : " + l1cache_loaded);
                if (Config.shared_cache)
                {
                    DEBUG.WriteLine("        Shared Cache Hit          : " + scache_hit);
                    DEBUG.WriteLine("        Shared Cache Miss         : " + scache_miss);
                    DEBUG.WriteLine("        Shared Cache Total Loaded : " + scache_loaded);
                }
            }
            DEBUG.WriteLine();
        }
        #endregion
    }
}
