#region using
using System;
using System.Collections.Generic;
using System.Linq;
using SimplePIM.Configs;
using SimplePIM.Memory;
using SimplePIM.General;
using SimplePIM.PIM;
using SimplePIM.Statics;
#endregion

namespace SimplePIM.Procs
{
    /// <summary>
    /// [Memory Controller Defination] 
    /// This class performs as a managment of packets sending.
    /// </summary>
    public class Mctrl : SimulatorObj
    {
        /// <summary>
        /// [Wait Queue] 
        /// Added Processor Requests are placed here.
        /// </summary>
        private List<ProcRequest> wait_queue;

        /// <summary>
        /// Proceessor Requests are flitered into unique lists.
        /// </summary>
        private List<Queue<MemRequest>> send_queue;

        /// <summary>
        /// PIM Controller bit
        /// <para>When PIM memory controller processed a memory operation,
        /// it should check whether host CPU has data duplications.
        /// If that, PIM will ask host CPU flush vaild data into memory.
        /// </para>
        /// </summary>
        public bool PIM = false;
        public Mctrl(bool pim_ = false)
        {
            PIM = pim_;
            wait_queue = new List<ProcRequest>();
            send_queue = new List<Queue<MemRequest>>();
        }

        /// <summary>
        /// Memory Controller has to initial after memory objects initialed.
        /// </summary>
        public void init_queue()
        {
            for (int i = 0; i < MemorySelecter.get_mem_count(); i++)
                send_queue.Add(new Queue<MemRequest>());
        }

        /// <summary>
        /// Add processor requests to wait queue in mctl
        /// </summary>
        /// <param name="req_">Request sent by Processors</param>
        /// <returns>Return true when request is added to wait_queue.</returns>
        public bool add_to_mctrl(ProcRequest req_)
        {
            if (wait_queue.Count > Config.crtl_queue_max - 1)
            {
                if (Config.DEBUG_MTRL)
                    DEBUG.WriteLine("-- MTRL : Add request to mctrl failed : wait_queue full --[" + req_.type + "] [0x" + req_.actual_addr.ToString("X")+"]");
                return false;
            }

            wait_queue.Add(req_);

            if (Config.DEBUG_MTRL)
                DEBUG.WriteLine("--MTRL : Add request to mctrl : [" + req_.type + "] [0x" + req_.actual_addr.ToString("X")+"]");
            return true;
        }
        /// <summary>
        /// Memory Object get their requests here.
        /// </summary>
        /// <param name="pid">ID of memory objects.</param>
        /// <param name="req_">Processor Requests</param>
        /// <returns></returns>
        public bool get_req(int pid, ref MemRequest req_)
        {
            string s = PIM ? "PIM " : "";
            if (send_queue[pid].Count() <= 0)
            {

                //if (Config.DEBUG_MTRL)
                //    DEBUG.WriteLine(s + "Memory Controller -- Memory [" + pid + "] : Request No requests");
                return false;
            }
            req_ = send_queue[pid].Peek();
            req_.pim = PIM ? true : false;
            if (Config.DEBUG_MTRL)
                DEBUG.WriteLine("--" + s + " MTRL -- [" + pid + "] : Pull Requests : [" + req_.memtype + "] [0x" + req_.address.ToString("X") + "]");
            send_queue[pid].Dequeue();
            return true;
        }

        /// <summary>
        /// Things ctrl done every cycle.
        /// </summary>
        public override void Step()
        {
            cycle++;
            if (Config.DEBUG_MTRL)
            {
                DEBUG.WriteLine();
                DEBUG.WriteLine("----------Memory Controller Update [Cycle " + cycle + "]------------");
            }
            for (int i = 0; i < wait_queue.Count(); i++)
            {
                ProcRequest peek = wait_queue[i];
                if (peek.cycle + (UInt64)Config.mc_latency <= cycle - 1)
                {
                    if (Config.DEBUG_MTRL)
                        DEBUG.WriteLine("-- Issue ProcRequest : [" + peek.type + "] [0x"+peek.block_addr.ToString("X")+ "] [0x" + peek.actual_addr.ToString("X") + "]");
                    if (Config.pim_config.Consistency_Model == Consistency.SpinLock)
                    {
                        //if (Config.DEBUG_MTRL)
                        //    DEBUG.WriteLine("-- Use Coherence : [" + Config.pim_config.Consistency_Model.ToString() + "]");
                        if (!Coherence.spin_lock.get_lock_state(peek.actual_addr))
                        {
                            send_queue[MemorySelecter.get_id(wait_queue[i].actual_addr)].Enqueue(transfer(wait_queue[i]));
                            wait_queue.RemoveAt(i);
                            i--;
                            if (Config.DEBUG_MTRL)
                                DEBUG.WriteLine("-- Sent ProcRequest :  [" + peek.type + "] [0x" + peek.block_addr.ToString("X") + "] [0x" + peek.actual_addr.ToString("X") + "]");
                            if (PIM)
                            {
                                //Console.WriteLine("SetLock");
                                Coherence.spin_lock.setlock(peek.actual_addr);
                                //when pim units start to perform, flush all relative data in the host core
                                Coherence.flush(peek.block_addr);
                            }
                        }
                        else
                        {
                            //current data are unavalible cuz it locked.
                            //stall time++
                            if (Config.DEBUG_MTRL)
                                DEBUG.WriteLine("-- ProcRequest Stalled by SpinLock : [" + peek.type + "] [0x" + peek.block_addr.ToString("X") + "] [0x" + peek.actual_addr.ToString("X") + "]");
                        }
                    }


                }
            }
            if (Config.DEBUG_MTRL)
            {
                DEBUG.WriteLine("--------------------------------------------");
            }
        }
        public MemRequest transfer(ProcRequest pro_req_)
        {
            MemRequest trans = new MemRequest();
            trans.address = MemorySelecter.resize( pro_req_.actual_addr);
            trans.data = 0;
            trans.block_addr = pro_req_.block_addr;
            trans.pid = pro_req_.pid;
            switch (pro_req_.type)
            {
                case RequestType.READ:
                    trans.memtype = MemReqType.READ;
                    break;
                case RequestType.WRITE:
                    trans.memtype = MemReqType.WRITE;
                    break;
                default:
                    trans.memtype = MemReqType.RETURN_DATA;
                    break;
            }
            

            return trans;
        }
    }
}
