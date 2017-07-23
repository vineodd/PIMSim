#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Statistics;
using PIMSim.General.Ports;
using PIMSim.General.Protocols;
#endregion

namespace PIMSim.TraceReader
{
    /// <summary>
    /// TraceFetcher Defination
    /// </summary>
    public class TraceFetcher :SimulatorObj
    {
        #region Private Variables

        /// <summary>
        /// FileStream of input trace
        /// </summary>
        private List<FileStream> trace = new List<FileStream>();

        /// <summary>
        /// StreamReader of FileStream.
        /// You can replace it with GZIPReader.
        /// </summary>
        private List<StreamReader> sr = new List<StreamReader>();

        /// <summary>
        /// Trace file folder path.
        /// </summary>
        private string path = Config.trace_path;

        #endregion
        public TraceFetcherMasterPorts port;
        #region Public Methods

        /// <summary>
        /// Set Trace File Folder Path.
        /// </summary>
        /// <param name="trace_file">path</param>
        /// <returns></returns>
        public bool SET_trace_path(string trace_file)
        {

            if (Directory.Exists(trace_file))
            {
                this.path = trace_file;
                if (Config.DEBUG_TRACE)
                    DEBUG.WriteLine("-- Trace Fetcher : Set Trace File Path : " + path);
              
                trace = new List<FileStream>(Config.N);
                sr = new List<StreamReader>(Config.N);
                for (int i = 0; i < Config.N; i++)
                {
                    trace.Add(new FileStream(path + Path.DirectorySeparatorChar+"CPU" + i + ".trace", FileMode.Open));
                    sr.Add(new StreamReader(trace[i]));
                }

                return true;
            }
            return false;

        }

        /// <summary>
        /// Destructor
        /// Close File handles.
        /// </summary>
        ~TraceFetcher()
        {
            foreach(var item in sr) { item.Close(); }
            foreach (var item in trace) { item.Close(); }
        }
        public TraceFetcher()
        {
            name = "TraceFetcher";
            port = new TraceFetcherMasterPorts("TraceFetcher Data Port", PortManager.Allocate());
            port.owner = this;
        }
        public void ServeBuffer()
        {
            if (port.buffer.Count() > 0)
            {
                var packets = port.buffer.Where(x => x.Item1 + x.Item2.linkDelay <= GlobalTimer.tick).ToList();
                if (packets.Count() > 0)
                {
                    packets.ForEach(x => x.Item2.ts_arrival=GlobalTimer.tick);
                    packets.ForEach(x => recvTimingReq(x.Item2));
                  //  packets.ForEach(x => recvFunctionalReq(x.Item2));
                }
            }
        }
        public new bool recvTimingReq(Packet pkt)
        {
            pkt.ts_issue = GlobalTimer.tick;
            var x = get_req(BitConverter.ToInt32(pkt.ReadData(), 0));
            PacketManager.Collect(pkt);
            Packet new_pkt = new Packet(CMD.ReadResp);
            new_pkt.linkDelay = Config.linkdelay_tracetetcher_to_insp;
            new_pkt.BuildData(SerializationHelper.SerializeObject(x));
            return sendTimingResq(ref new_pkt);

        }
        public new bool recvFunctionalReq(Packet pkt)
        {
            pkt.ts_issue = GlobalTimer.tick;
            var x = get_req(BitConverter.ToInt32(pkt.ReadData(), 0));
            PacketManager.Collect(pkt);
            Packet new_pkt = new Packet(CMD.ReadResp);
            new_pkt.BuildData(SerializationHelper.SerializeObject(x));
            return sendFunctionalResq(ref new_pkt);
        }

        public new bool sendFunctionalResq(ref Packet pkt)
        {
            port._slavePort.recvFunctionalResp(pkt);
            return true;
        }

        public new bool sendTimingResq(ref Packet pkt)
        {
            port._slavePort.addPacket(pkt);
            return true;
        }
        /// <summary>
        /// Parse one trace line into instruction
        /// </summary>
        /// <param name="line">string line</param>
        /// <param name="pid_">trace file id</param>
        /// <returns></returns>
        public Instruction parse_ins(string line,int pid_)
        {
            try
            {
                //try one line
                DEBUG.Assert(line != null);

                string[] split_ins = line.Split('|');
                Instruction ins = new Instruction(split_ins[1]);
                ins.cycle = UInt64.Parse(split_ins[0]);
                ins.pid = pid_;

                if (PIMConfigs.PIM_Fliter == PIM_input_type.All)
                {
                    //set all insruction to pim
                    ins.pim = PIMConfigs.PIM_Ins_List.Any(s => s == ins.Operation) ? true : false;
                }
                else
                {
                    //use PIM_ label to identify PIM operations
                    ins.pim = (ins.Operation.StartsWith("PIM_")) ? true : false;
                }
                //read address and data.
                if (split_ins.Length >= 4)
                {
                    ins.type = (split_ins[2] == "R") ? InstructionType.READ : InstructionType.WRITE;
                  //  ins.type = (split_ins[2] == "W") ? InstructionType.WRITE : InstructionType.CALCULATION;



                    string[] d_and_a = split_ins[3].Split(' ');
                    ins.is_mem = false;

                    //read data and address
                    foreach (string p in d_and_a)
                    {
                        if (p.Contains("A="))
                        {
                            ins.address = UInt64.Parse(p.Replace("A=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                            ins.is_mem = true;
                        }
                        else
                        {
                            if (p.Contains("D="))
                            {
                                ins.data = UInt64.Parse(p.Replace("D=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                                ins.is_mem = true;
                            }
                            else
                            {
                                DEBUG.Error("Error in parsing trace line.");
                                Environment.Exit(2);
                            }
                        }
                    }

                }
                else
                {
                    ins.type = InstructionType.CALCULATION;
                }
                return ins;
            }

            catch 
            {
                DEBUG.Error("Faied to parse trace in CPU:" + pid_ + "line=" + line);
                return null;
            }
        }

        /// <summary>
        /// Get inputs by trace id
        /// </summary>
        /// <param name="pid_">trace file id</param>
        /// <returns>InputType</returns>
        public InputType get_req(int pid_)
        {
            string currentline = "";
            while (true)
            {
                currentline = sr[pid_].ReadLine();
                if (currentline == null)
                {
                    Instruction res = new Instruction();
                    res.type = InstructionType.EOF;
                    return res;

                }
                if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                    continue;
                
                if (currentline.Contains(";"))
                    currentline = currentline.Substring(0, currentline.IndexOf(";") + 1);
                if (!currentline.Contains("PIM_") &&! currentline.Contains("START") && !currentline.Contains("END"))
                {
                    return parse_ins(currentline, pid_);
                }
                else
                {
                    if (currentline.Contains("PIM_") && currentline.Contains("_START"))
                    {
                        //reading block
                        int block_size = 0;
                        InstructionBlock to_add = new InstructionBlock();
                        to_add.name = currentline.Replace("PIM_", "").Replace("_START", "");
                        while (true)
                        {
                            currentline = sr[pid_].ReadLine();
                            if (currentline == null)
                            {
                                return new Instruction(InstructionType.EOF);
                            }
                            if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                                continue;
                            if (currentline.Contains("PIM_") && currentline.Contains("END"))
                            {
                                return to_add;
                            }
                            if (currentline.Contains(";"))
                                currentline = currentline.Substring(0, currentline.IndexOf(";") + 1);
                            to_add.add_ins(parse_ins(currentline, pid_));
                            block_size++;
                            if (block_size > PIMConfigs.max_pim_block)
                            {
                                //error
                                Environment.Exit(1);
                            }
                        }
                    }
                    else
                    {
                        if (currentline.Contains("FUNCTION_") && currentline.Contains("_START"))
                        {
                            Function func = new Function();
                            func.cycle = UInt64.Parse(currentline.Substring(0, currentline.IndexOf("|") ));
                            func.name = currentline.Split('_')[1];
                            while (true)
                            {
                                currentline = sr[pid_].ReadLine().Replace("\t", "").Replace(" ", "");
                                if (currentline == null)
                                {
                                    return new Instruction(InstructionType.EOF);
                                }
                                if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                                    continue;
                                if (currentline.Contains("FUNCTION_") && currentline.Contains("_END"))
                                {

                                    return func;
                                }
                                if (currentline.Contains(";"))
                                    currentline = currentline.Substring(0, currentline.IndexOf(";") + 1);
                                currentline = currentline.Replace(" ", "");
                                string[] tp = currentline.Split('=');
                                if (tp[0].Equals("input"))
                                {
                                    func.input.Add(UInt64.Parse(tp[1].Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier));
                                }
                                else
                                {
                                    if (tp[0].Equals("output"))
                                    {
                                        func.output.Add(UInt64.Parse(tp[1].Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier));
                                    }
                                    else
                                    {
                                        if (tp[0].Equals("latency"))
                                        {
                                            func.latency = UInt64.Parse(tp[1].Replace("0x", ""));
                                        }
                                        else
                                        {
                                            ///?????
                                            ///
                                            Environment.Exit(1);

                                        }
                                    }
                                }


                            }
                        }
                        else
                        {
                            return parse_ins(currentline, pid_);
                           
                        }
                    }

                }




            }
        }

        /// <summary>
        /// Cycle++
        /// </summary>
        public override void Step()
        {
            cycle++;
            ServeBuffer();
        }

        #endregion
    }
}
