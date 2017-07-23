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
        /// Attached TraceFetcher 
        /// </summary>
        private TraceFetcher trace;

        /// <summary>
        /// End of file flag list.
        /// If set TRUE, indicates that corresponding trace file encounters eof.
        /// </summary>
        private List<bool> eof;

        #endregion

        public TraceFetcherSlavePort data_port;

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
            data_port = new TraceFetcherSlavePort("Insp Data Port", PortManager.Allocate());
            data_port.owner = this;

        }


        /// <summary>
        /// Processors or PIMUnit use this method to get their inputs.
        /// </summary>
        /// <param name="pid">pid of unit</param>
        /// <param name="host">true indicates target unit is at host-side. Otherwise at memory-side.</param>
        /// <returns></returns>
        public Input get_req(int pid, bool host = false)
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
                if (pim_ins[pid].Count == 0)
                {
                    //when pim queue has no reqs, nothing is sent to PIM.
                    return new Instruction();
                }
                Input current = pim_ins[pid].Peek();
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

        public override bool sendTimingReq(ref Packet pkt)
        {
            DEBUG.Assert(pkt.isRequest() && pkt.isRead());
            pkt.ts_departure = GlobalTimer.tick;
            data_port._masterPort.addPacket(pkt);
            return true;
        }
        public bool sendTimeingReq(int dst)
        {
            var pkt = buildRequest(dst);
            pkt.linkDelay = Config.linkdelay_insp_to_tracetetcher;
            return sendTimingReq(ref pkt);
        }
        public override bool sendFunctionalReq(ref Packet pkt)
        {
            DEBUG.Assert(pkt.isRequest() && pkt.isRead());
            pkt.ts_departure = GlobalTimer.tick;
            data_port._masterPort.recvFunctionalReq(pkt);
            return true;
        }
        public bool sendFunctionalReq(int dst)
        {
            var pkt = buildRequest(dst);
            return sendFunctionalReq(ref pkt);
        }
        public Packet buildRequest(int dst)
        {
            Packet pkt = new Packet(CMD.ReadReq);
            pkt.BuildData(dst);
            return pkt;

        }

        public override bool recvFunctionalResp(Packet pkt)
        {
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
        }

        public new bool recvTimingResp(Packet pkt)
        {
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
        }
        public void ServeBuffer()
        {
            if (data_port.buffer.Count() > 0)
            {
                var packets = data_port.buffer.Where(x => x.Item1 + x.Item2.linkDelay <= GlobalTimer.tick).ToList();
                if (packets.Count() > 0)
                {
                    packets.ForEach(x => { recvTimingResp(x.Item2); data_port.buffer.Remove(x); });
                    
                }
            }
        }

        /// <summary>
        /// Interal Step
        /// </summary>
        public override void Step()
        {
            cycle++;
            ServeBuffer();
            for (int i = 0; i < Config.N; i++)
            {
                for (int j = 0; j < Config.IPC; j++)
                {
                    if (!eof[i])
                    {
                        //not running out of traces.
                        if (all_ins[i].Count() >= Config.max_insp_waitting_queue)
                        {
                            //input waitting queue is full
                            continue;
                        }
                        sendTimeingReq(i);
                       // sendFunctionalReq(i);
                    }
                }
            }


        }

        public bool trace_done()
        {
            return !eof.Aggregate((i, j) => i && j);
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
