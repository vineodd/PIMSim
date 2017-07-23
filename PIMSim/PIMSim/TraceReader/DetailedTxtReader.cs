using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.TraceReader
{
    public class DetailedTxtReader : FileReader
    {
        public DetailedTxtReader() : base() { }
        public override Input get_req(int pid_)
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
                if (!currentline.Contains("PIM_") && !currentline.Contains("START") && !currentline.Contains("END"))
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
                            func.cycle = UInt64.Parse(currentline.Substring(0, currentline.IndexOf("|")));
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

        public Instruction parse_ins(string line, int pid_)
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
    }
}
