#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using PIMSim.Configs;
using PIMSim.Memory;
using PIMSim.General;
using PIMSim.PIM;
using PIMSim.Statistics;
#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// [Memory Controller Defination] 
    /// This class performs as a managment of packets sending.
    /// The real memory controller is defined at \Memory\.
    /// </summary>
    public static class Mctrl 
    {
        #region Static Variables
        /// <summary>
        /// NULL marks Invaild Data Or Blank Address.
        /// </summary>
        public static readonly UInt64 NULL = UInt64.MaxValue;


        #endregion

        #region Private Variables

        /// <summary>
        /// [Wait Queue] 
        /// Added Processor Requests are placed here.
        /// </summary>
        private static List<ProcRequest> wait_queue;

        /// <summary>
        /// Proceessor Requests are flitered into unique lists.
        /// </summary>
        private static List<Queue<MemRequest>> send_queue;

        #endregion

        #region Public Variables
        /// <summary>
        /// PIM Controller bit
        /// <para>When PIM memory controller processed a memory operation,
        /// it should check whether host CPU has data duplications.
        /// If that, PIM will ask host CPU flush vaild data into memory.
        /// </para>
        /// </summary>
        public static bool pim = false;

        public static UInt64 cycle = 0;

        public static string name = "";

        public static int id = 0;

        #endregion

        #region Statistics Variables

        private static UInt64 stalled_reqs_by_coherence = 0;
        private static UInt64 total_add = 0;
        private static UInt64 add_failed = 0;

        #endregion 

        #region Public Methods


        public static bool done()
        {
            bool res = true;
            for(int i=0;i < send_queue.Count(); i++)
            {
                res = res & (send_queue[i].Count() <= 0);
            }
            return wait_queue.Count <= 0 && res;
        }
        /// <summary>
        /// Constructed Function;
        /// </summary>
        /// <param name="pim_">if this MTRL is at memory-side</param>
        static Mctrl()
        {
            id = 0;
            wait_queue = new List<ProcRequest>();
            send_queue = new List<Queue<MemRequest>>();
        }

        /// <summary>
        /// Memory Controller has to initial after memory objects initialed.
        /// </summary>
        public static void init_queue()
        {
            for (int i = 0; i < MemorySelector.get_mem_count; i++)
                send_queue.Add(new Queue<MemRequest>());
        }

        /// <summary>
        /// Add processor requests to wait queue in mctl
        /// </summary>
        /// <param name="req_">Request sent by Processors</param>
        /// <returns>Return true when request is added to wait_queue.</returns>
        public static bool add_to_mctrl(ProcRequest req_)
        {
            if (wait_queue.Count > Config.crtl_queue_max - 1)
            {
                if (Config.DEBUG_MTRL)
                    DEBUG.WriteLine("-- MTRL : Add requests failed : wait_queue full --[" + req_.type + "] [0x" + req_.actual_addr.ToString("X")+"]");
                add_failed++;
                return false;
            }

            wait_queue.Add(req_);

            if (Config.DEBUG_MTRL)
                DEBUG.WriteLine("-- MTRL : Add requests : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X")+"]");
            total_add++;
            return true;
        }

        /// <summary>
        /// Memory Object get their requests here.
        /// </summary>
        /// <param name="pid">ID of memory objects.</param>
        /// <param name="req_">Processor Requests</param>
        /// <returns></returns>
        public static bool get_req(int pid, ref MemRequest req_)
        {
            if (send_queue[pid].Count() <= 0)
            {

                //if (Config.DEBUG_MTRL)
                //    DEBUG.WriteLine(s + "Memory Controller -- Memory [" + pid + "] : Request No requests");
                return false;
            }
            req_ = send_queue[pid].Peek();
            req_.pim = false;
            if (Config.DEBUG_MTRL)
                DEBUG.WriteLine("--" + " MTRL [" + pid + "] : Push Requests : [" + req_.memtype + "] [0x" + req_.address.ToString("X") + "]");
            send_queue[pid].Dequeue();
            return true;
        }

        /// <summary>
        /// Things ctrl done every cycle.
        /// </summary>
        public static void Step()
        {
            cycle++;
            if (Config.DEBUG_MTRL)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("---------" + (pim ? "PIM " : "") + " Memory Controller ["+id+"] Update [Cycle " + cycle + "]------------");
            }
            for (int i = 0; i < wait_queue.Count(); i++)
            {
                ProcRequest peek = wait_queue[i];
                if (peek.cycle + (UInt64)Config.mc_latency <= cycle - 1)
                {
                    if (Config.DEBUG_MTRL)
                        DEBUG.WriteLine("-- Issue ProcRequest : [" + peek.type + "] [0x"+peek.block_addr.ToString("X")+ "] [0x" + peek.actual_addr.ToString("X") + "]");
                    if (PIMConfigs.Consistency_Model == Consistency.SpinLock)
                    {
                        //if (Config.DEBUG_MTRL)
                        //    DEBUG.WriteLine("-- Use Coherence : [" + Config.pim_config.Consistency_Model.ToString() + "]");
                        if (!Coherence.spin_lock.get_lock_state(peek.actual_addr))
                        {
                            //if (pim)
                            //{
                          
                            //    Coherence.spin_lock.setlock(peek.actual_addr);

                            //    //when pim units start to perform, flush all relative data in the host core
                            //    if (!Coherence.flush(peek.block_addr))
                            //    {
                            //        Coherence.spin_lock.relese_lock(peek.actual_addr);
                            //        DEBUG.WriteLine("-- Waiting Host cores flushing data : [0x" + peek.block_addr.ToString("X") + "] [0x" + peek.actual_addr.ToString("X") + "]");
                            //        continue;
                            //    }
                            //}
                            send_queue[MemorySelector.get_id(wait_queue[i].actual_addr)].Enqueue(transfer(wait_queue[i]));
                            wait_queue.RemoveAt(i);
                            i--;
                            if (Config.DEBUG_MTRL)
                                DEBUG.WriteLine("-- Sent ProcRequest :  [" + peek.type + "] [0x" + peek.block_addr.ToString("X") + "] [0x" + peek.actual_addr.ToString("X") + "]");

                        }
                        else
                        {
                            //current data are unavalible cuz it locked.
                            //stall req++
                            stalled_reqs_by_coherence++;
                            if (Config.DEBUG_MTRL)
                                DEBUG.WriteLine("-- ProcRequest Stalled by SpinLock : [" + peek.type + "] [0x" + peek.block_addr.ToString("X") + "] [0x" + peek.actual_addr.ToString("X") + "]");
                        }
                    }


                }
            }
            if (Config.DEBUG_MTRL)
            {
                DEBUG.WriteLine();
            }
        }

        /// <summary>
        /// Transfer Processor requests into Memory requests
        /// </summary>
        /// <param name="pro_req_">processor request to transfer</param>
        /// <returns></returns>
        public static MemRequest transfer(ProcRequest pro_req_)
        {
            MemRequest trans = new MemRequest();
            trans.address = MemorySelector.resize( pro_req_.actual_addr);
            trans.data = 0;     //actully we need no data here, but we'll take it in the future.
            trans.block_addr = pro_req_.block_addr;
            trans.pid.Add(pro_req_.pid);
            trans.cycle = pro_req_.cycle;
            switch (pro_req_.type)
            {
                case RequestType.READ:
                    trans.memtype = MemReqType.READ;
                    break;
                case RequestType.WRITE:
                    trans.memtype = MemReqType.WRITE;
                    break;
                case RequestType.FLUSH:
                    trans.memtype = MemReqType.FLUSH;
                    break;
                default:
                    trans.memtype = MemReqType.RETURN_DATA;
                    break;
            }
            return trans;
        }

        /// <summary>
        /// Print Statistics Info
        /// </summary>
        public static void PrintStatus()
        {
            DEBUG.WriteLine("---------------  MTRL ["+id+"] Statistics  -----------");
            DEBUG.WriteLine("    Total reqs added : " + total_add);
            DEBUG.WriteLine("    Total regs stalled : " + add_failed);
            DEBUG.WriteLine(" Total Stalled by Coherence :" + stalled_reqs_by_coherence);
            DEBUG.WriteLine();
        }

        #endregion
    }
}
