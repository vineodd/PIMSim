using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.Statics;

namespace SimplePIM.Procs
{
    public class TraceFetcher :SimulatorObj
    {
        public List<FileStream> trace = new List<FileStream>();
        public List<StreamReader> sr = new List<StreamReader>();
        public string path = "";
        public int n;

       
        public TraceFetcher()
        {

            path = Config.trace_path;
            n = Config.N;
            
        }
        public bool SET_trace_path(string trace_file)
        {

            if (Directory.Exists(trace_file))
            {
                this.path = trace_file;
                if (Config.DEBUG_TRACE)
                    DEBUG.WriteLine("-- Trace Fetcher : Set Trace File Path : " + path);
              
                trace = new List<FileStream>(n);
                sr = new List<StreamReader>(n);
                for (int i = 0; i < n; i++)
                {
                    trace.Add(new FileStream(path + @"\CPU" + i + ".trace", FileMode.Open));
                    sr.Add(new StreamReader(trace[i]));
                }

                return true;
            }
            return false;
            // Debug.Assert(File.Exists(tracepath));
        }
        ~TraceFetcher()
        {
            foreach(var item in sr) { item.Close(); }
            foreach (var item in trace) { item.Close(); }

        }
        public Instruction parse_ins(string line,int pid_)
        {
            try
            {
                string[] tmp = line.Split('|');
                Instruction ins = new Instruction(tmp[1]);
                ins.cycle = UInt64.Parse(tmp[0]);
                ins.pid = pid_;
                if (Config.pim_config.PIM_Fliter == PIM_input_type.All)
                {
                    //all ins
                }
                else
                {
                    if (ins.Operation.StartsWith("PIM_"))
                        ins.pim = true;
                    else
                        ins.pim = false;
                }
                if (tmp.Length >= 4)
                {
                    if (tmp[2] == "R")
                    {
                        ins.type = InstructionType.READ;
                        string[] danda = tmp[3].Split(' ');
                        ins.is_mem = false;
                        foreach (string p in danda)
                        {
                            if (p.Contains("A="))
                            {
                                ins.address = UInt64.Parse(p.Replace("A=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                                ins.is_mem = true;
                            }
                            if (p.Contains("D="))
                            {
                                ins.data = UInt64.Parse(p.Replace("D=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                                ins.is_mem = true;
                            }

                        }


                    }
                    else {
                        if (tmp[2] == "W")
                        {
                            ins.type = InstructionType.WRITE;
                            string[] danda = tmp[3].Split(' ');
                            foreach (string p in danda)
                            {
                                if (p.Contains("A="))
                                {
                                    ins.address = UInt64.Parse(p.Replace("A=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                                    ins.is_mem = true;
                                }
                                if (p.Contains("D="))
                                {
                                    ins.data = UInt64.Parse(p.Replace("D=0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
                                    ins.is_mem = true;
                                }
                            }
                        }
                        else
                        {
                            //these shall never happend
                            ins.type = InstructionType.EOF;
                        }
                    }
                }
                else
                {
                    ins.type = InstructionType.CALCULATION;
                }
                return ins;
            }

            catch (Exception e)
            {
                Console.WriteLine("ERROR : faied to parse trace in CPU:" + pid_ + "line=" + line);
                return null;

            }
        }
        public InputType get_req(int pid_)
        {
            string currentline = "";
            while (true)
            {
                currentline = sr[pid_].ReadLine();
                if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                    continue;
                if (currentline == null)
                {
                    Instruction res = new Instruction();
                    res.type = InstructionType.EOF;
                    return res;
                    
                }
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
                            if (block_size > Config.pim_config.max_pim_block)
                            {
                                //error
                                Environment.Exit(1);
                            }
                        }
                    }
                    else
                    {
                        if (currentline.Contains("PIM_") && currentline.Contains("FUNCTION_START"))
                        {
                            int block_size = 0;
                            Function func = new Function();
                            func.cycle = UInt64.Parse(currentline.Substring(0, currentline.IndexOf("|") + 1));
                            while (true)
                            {
                                currentline = sr[pid_].ReadLine();
                                if (currentline == null)
                                {
                                    return new Instruction(InstructionType.EOF);
                                }
                                if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                                    continue;
                                if (currentline.Contains("PIM_") && currentline.Contains("FUNCTION_END"))
                                {

                                    return func;
                                }
                                if (currentline.Contains(";"))
                                    currentline = currentline.Substring(0, currentline.IndexOf(";") + 1);
                                currentline = currentline.Replace(" ", "");
                                string[] tp = currentline.Split('=');
                                if (tp[0].Equals("input"))
                                {
                                    func.input.Add(UInt64.Parse(tp[1].Replace("0x", "")));
                                }
                                else
                                {
                                    if (tp[0].Equals("output"))
                                    {
                                        func.output.Add(UInt64.Parse(tp[1].Replace("0x", "")));
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

        public override void Step()
        {
            cycle++;
        }


    }
}
