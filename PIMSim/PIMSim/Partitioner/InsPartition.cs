#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Statistics;
using PIMSim.TraceReader;
using PIMSim.General.Ports;
using PIMSim.General.Protocols;
#endregion
namespace PIMSim.Partitioner
{
    /// <summary>
    /// Instruction Partitioner
    /// Instruction partitioner does two things:
    /// 1.  Get reqs from TraceFetcher and  store them in the list by the order of there destiny id.
    /// 2.  Send reqs to their execution units.
    /// </summary>
    public class InsPartition : SimulatorObj
    {
        #region Private Varibles
        /// <summary>
        /// Host processor count
        /// </summary>
        private int n = 0;

        /// <summary>
        /// inupts to feed to host processors
        /// </summary>
        private List<Queue<Input>> all_ins;

        /// <summary>
        /// inputs to feed to pim units
        /// </summary>
        private List<Queue<Input>> pim_ins;


        /// <summary>
        /// End of file flag list.
        /// If set TRUE, indicates that corresponding trace file encounters eof.
        /// </summary>
        private List<bool> eof;

        #endregion

        public TraceFetcherSlavePort ins_port;

        public InspCPUMasterPort data_port;

        #region Statistics Varibles
        //for statistics
        private List<UInt64> divide_pim_reqs = new List<ulong>();
        private List<UInt64> divide_host_reqs = new List<ulong>();
        public UInt64 total_pim_reqs=> divide_pim_reqs.Aggregate((total, next) => total + next);
        public UInt64 total_host_reqs => divide_host_reqs.Aggregate((total, next) => total + next);
        public UInt64 total_reqs => divide_host_reqs.Aggregate((total, next) => total + next) + divide_pim_reqs.Aggregate((total, next) => total + next);
        private List<UInt64> divide_pim_sent = new List<ulong>();
        private List<UInt64> divide_host_sent = new List<ulong>();
        private UInt64 total_pim_sent => divide_pim_sent.Aggregate((total, next) => total + next);
        private UInt64 total_host_sent=>divide_host_sent.Aggregate((total, next) => total + next);

        public double PIMBandWidth(int i)
        {
            return divide_pim_sent[i] / 8 //byte
                / 1024//KB
                / 1024//MB
                *1.0 / cycle //MB/cycle
                * GlobalTimer.reference_clock;
        }
        public double PIMBandWidth()
        {
            return total_pim_sent / 8 //byte
                / 1024//KB
                / 1024//MB
                *1.0 / cycle //MB/cycle
                * GlobalTimer.reference_clock;
        }
        #endregion

        #region Public Methods
        public InsPartition()
        {

            n = Config.N;
            all_ins = new List<Queue<Input>>();
            eof = new List<bool>();
            for (int i = 0; i < n; i++)
            {
                all_ins.Add(new Queue<Input>());
                eof.Add(false);
                divide_host_reqs.Add(0);
                divide_host_sent.Add(0);
            }
            pim_ins = new List<Queue<Input>>();
            for (int i = 0; i < PIMConfigs.pim_cu_count; i++)
            {
                pim_ins.Add(new Queue<Input>());
                divide_pim_reqs.Add(0);
                divide_pim_sent.Add(0);
            }
            ins_port = new TraceFetcherSlavePort("Insp Ins Port", PortManager.Allocate());
            ins_port.owner = this;
            data_port = new InspCPUMasterPort("Insp Data Port", PortManager.Allocate());
            data_port.owner = this;
        }


        /// <summary>
        /// Processors or PIMUnit use this method to get their inputs.
        /// </summary>
        /// <param name="pid">pid of unit</param>
        /// <param name="host">true indicates target unit is at host-side. Otherwise at memory-side.</param>
        /// <returns></returns>
        public Input get_req(int pid, bool host = false,UInt64 pc=0)
        {
            if (host)
            {     
                if (all_ins[pid].Count == 0)
                {
                    //no input detected
                    var ins= new Instruction();
                    divide_host_reqs[pid]++;
                    divide_host_sent[pid] += ins.Length();
                    return ins;
                }
                Input current = all_ins[pid].Peek();
                if (!(current is PCTrace))
                {
                    if (current.cycle > cycle - 1)
                    {
                        //not this time
                        var ins = new Instruction();
                        divide_host_reqs[pid]++;
                        divide_host_sent[pid] += ins.Length();
                        return ins;
                    }
                    else
                    {

                        if (current.cycle <= cycle - 1)
                        {
                            if (current is Function)
                            {
                                //procs cannot process functions
                                Environment.Exit(1);
                            }
                            //pop current inputs
                            all_ins[pid].Dequeue();
                            divide_host_reqs[pid]++;
                            divide_host_sent[pid] += current.Length();
                            return current;
                        }
                        else
                        {
                            //if program runs into this part, exit in error
                            if (Config.DEBUG_INSP)
                                DEBUG.WriteLine("ERROR : ");
                            Environment.Exit(1);
                            return null;
                        }
                    }
                }
                else
                {

                    var peek = all_ins[pid].Peek() as PCTrace;
                    if (pc == 0 || pc >= peek.PC)
                    {
                        all_ins[pid].Dequeue();
                        return peek;
                    }
                    else
                    {
                        var ins = new Instruction();
                        divide_host_reqs[pid]++;
                        divide_host_sent[pid] += ins.Length();
                        return ins;
                    }

                }
            }
            else
            {
                if (pim_ins[pid].Count == 0)
                {
                    //when pim queue has no reqs, nothing is sent to PIM.
                    return new Instruction();
                }
                Input current = pim_ins[pid].Peek();
                if (!(current is PCTrace))
                {
                    if (current.cycle > cycle - 1)
                    {
                        //when pim queue has no reqs, nothing is sent to PIM.
                        return new Instruction();
                    }
                    else
                    {
                        if (current.cycle <= cycle - 1)
                        {

                            pim_ins[pid].Dequeue();
                            divide_pim_reqs[pid]++;
                            divide_pim_sent[pid] += current.Length();
                            return current;
                        }
                        else
                        {
                            //if program runs into this part, exit in error
                            if (Config.DEBUG_INSP)
                                DEBUG.WriteLine("ERROR : ");
                            Environment.Exit(1);
                            return null;
                        }
                    }
                }
                else
                {
                    var peek = pim_ins[pid].Peek() as PCTrace;
                    if (pc == 0 || pc >= peek.PC)
                    {
                        pim_ins[pid].Dequeue();
                        return peek;
                    }
                    else
                    {
                        var ins = new Instruction();
                        divide_host_reqs[pid]++;
                        divide_host_sent[pid] += ins.Length();
                        return ins;
                    }
                }
            }
          
        }

        /// <summary>
        /// Link input with corresponding PIM units.
        /// You can modify it by names, pids.
        /// Default to zero.
        /// </summary>
        /// <param name="ins_"></param>
        /// <returns></returns>
        public int corresponding_unit(Input ins_)
        {
            return 0;
        }

        public override bool sendTimingReq(PacketSource destiny, ref Packet pkt)
        {
            switch (destiny)
            {
                case PacketSource.TraceFetcher:
                    DEBUG.Assert(pkt.isRequest() && pkt.isRead());
                    pkt.ts_departure = GlobalTimer.tick;
                    ins_port._masterPort.addPacket(pkt);
                    return true;
                default:
                    return false;
            }
            
        }


        public bool sendTimingReq(int dst,PacketSource destiny)
        {
            var pkt = buildTracerRequest(dst, destiny);
            pkt.linkDelay = Config.linkdelay_insp_to_tracetetcher;
            return sendTimingReq(destiny, ref pkt);
        }
        public override bool sendFunctionalReq(PacketSource destiny, ref Packet pkt)
        {
            switch (destiny)
            {
                case PacketSource.TraceFetcher:
                    DEBUG.Assert(pkt.isRequest() && pkt.isRead());
                    pkt.ts_departure = GlobalTimer.tick;
                    ins_port._masterPort.recvFunctionalReq(pkt);
                    return true;
                default:
                    return false;
            }
            
        }
        public bool sendFunctionalReq(int dst, PacketSource destiny)
        {
            var pkt = buildTracerRequest(dst, destiny);
            return sendFunctionalReq(destiny, ref pkt);
        }

        public Packet buildTracerRequest(int dst,PacketSource destiny )
        {
            switch (destiny)
            {
                case PacketSource.TraceFetcher:
                    Packet pkt = new Packet(CMD.ReadReq);
                    pkt.source = PacketSource.Insp;
                    pkt.BuildData(dst);
                    return pkt;

                default:
                    return null;
            } 
        }

        public override bool recvFunctionalResp(Packet pkt)
        {
            switch (pkt.source)
            {
                case PacketSource.TraceFetcher:
                    pkt.ts_issue = GlobalTimer.tick;
                    Input to_add = (Input)SerializationHelper.DeserializeObject(pkt.ReadData());
                    if (to_add is Instruction )
                    {
                        if ((to_add as Instruction).type != InstructionType.EOF)
                        {
                            if (!(to_add as Instruction).pim)
                                all_ins[(to_add as Instruction).pid].Enqueue(to_add);
                            else
                            {
                                pim_ins[corresponding_unit(null)].Enqueue(to_add);
                            }
                            to_add = null;
                        }
                        else
                        {
                            eof[(to_add as Instruction).pid] = true;
                        }
                    }
                    else
                    {
                        if (to_add is PCTrace)
                        {
                            bool contain = false;
                            PIMConfigs.PIM_kernal.ForEach(x => { if (x.contains((to_add as PCTrace).PC)) contain = true; });
                            if(contain)
                                pim_ins[(to_add as PCTrace)._id].Enqueue(to_add);
                            else
                                all_ins[(to_add as PCTrace)._id].Enqueue(to_add);
                        }
                        else
                        {
                            pim_ins[corresponding_unit(null)].Enqueue(to_add);
                        }
                    }
                    return true;


                default:
                    return false;
            }
        }

        public new bool recvTimingResp(Packet pkt)
        {
            switch (pkt.source)
            {
                case PacketSource.TraceFetcher:
                    pkt.ts_issue = GlobalTimer.tick;
                    Input to_add = (Input)SerializationHelper.DeserializeObject(pkt.ReadData());
                    if (to_add is Instruction)
                    {
                        if ((to_add as Instruction).type != InstructionType.EOF)
                        {
                            if (!(to_add as Instruction).pim)
                                all_ins[(to_add as Instruction).pid].Enqueue(to_add);
                            else
                            {
                                pim_ins[corresponding_unit(null)].Enqueue(to_add);
                            }
                            to_add = null;
                        }
                        else
                        {
                            eof[(to_add as Instruction).pid] = true;
                        }
                    }
                    else
                    {
                        pim_ins[corresponding_unit(null)].Enqueue(to_add);
                    }
                    return true;

                
                default:
                    return false;
            }
            
        }

        /// <summary>
        /// Serve buffer
        /// </summary>
        public void ServeBuffer()
        {
            if (ins_port.buffer.Count() > 0)
            {
                var packets = ins_port.buffer.Where(x => x.Item1 + x.Item2.linkDelay <= GlobalTimer.tick).ToList();
                if (packets.Count() > 0)
                {
                    packets.ForEach(x => { recvTimingResp(x.Item2); ins_port.buffer.Remove(x); });
                    
                }
            }
        }

        /// <summary>
        /// Interal Step
        /// </summary>
        public override void Step()
        {
            cycle++;
            PartitionMethod();
        }
        /// <summary>
        /// Replace or achieve your own partion methods here.
        /// </summary>
        public void PartitionMethod()
        {
            /**
            *   The default partition methods is to identify the PIM_ label
            *   marked in the trace file. We use two lists to store trace 
            *   inputs :
            *       all_ins is the host-side processors input queues.
            *       pim_ins is the memory-side input queues.
            **/

            /**
            *   ServeBuffer here is to process timing responses sent by 
            *   TraceFetcher. We use a queue to store inputs temporarily.
            *
            *   Need not while using functional mode.
            **/
            //ServeBuffer();

            for (int i = 0; i < Config.N; i++)
            {
                for (int j = 0; j < Config.IPC; j++)
                {
                    if (!eof[i])
                    {
                        //run until out of traces.
                        if (all_ins[i].Count() >= Config.max_insp_waitting_queue)
                        {
                            //input waitting queue is full.
                            //avoid the memory occupation.
                            continue;
                        }

                        //sendTimingReq(i, PacketSource.TraceFetcher);
                        sendFunctionalReq(i, PacketSource.TraceFetcher);
                    }
                }
            }
        }

        /// <summary>
        /// Indicate whether we've already processed every input traces.
        /// </summary>
        /// <returns> true when all the inputs were processed.</returns>
        public bool done()
        {
            /**
            *   Make sure each queue is empty and all the eof flag is set.
            **/
            bool res = true;
            for(int i = 0; i < eof.Count(); i++)
            {
                res = res & eof[i];
            }
            if (!res)
                return false;
            for(int i = 0; i < all_ins.Count(); i++)
            {
                res = res & (all_ins[i].Count <= 0);
            }
            if (!res)
                return false;
            for (int i = 0; i < pim_ins.Count(); i++)
            {
                res = res & (pim_ins[i].Count <= 0);
            }
            return res;
            //
            //return !eof.Aggregate((i, j) => i && j);
        }

        /// <summary>
        /// Print Statistics Info
        /// </summary>
        public void PrintStatus()
        {
            DEBUG.WriteLine("=====================InsPartition Statistics=====================");
            DEBUG.WriteLine();
            DEBUG.WriteLine("        Total Requests Sent   : " + total_reqs);
            DEBUG.WriteLine("        Total PIM Requests    : " + total_pim_reqs);
            DEBUG.WriteLine();
            for (int i=0;i<divide_pim_reqs.Count();i++)
            {
            DEBUG.WriteLine("-------------- PIM Unit [" + i + "] Messages Statistics ---------");
            DEBUG.WriteLine();
            DEBUG.WriteLine("         Sent Messages        : " + divide_pim_reqs[i]);
            DEBUG.WriteLine("      Messages Percentage     : " + (total_reqs!=0?divide_pim_reqs[i]*100.0/ total_reqs: 100)+"%");
            DEBUG.WriteLine("          Bandwidth           : " + PIMBandWidth(i) + "MB/s");
            DEBUG.WriteLine();
            }
            DEBUG.WriteLine("        Total Bandwidth           : " + PIMBandWidth() + "MB/s");
            DEBUG.WriteLine();
        }
        #endregion
    }
}
