#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
#endregion

namespace PIMSim.Tools
{
    /// <summary>
    /// Transfer GEM5 trace to usable trace file
    /// </summary>
    public class TextTraceParser
    {
        public const string spliter = "|";
        FileStream fs_read;
        FileStream fs_out;
        StreamReader sr;
        StreamWriter sw;
        string trace_path = "";
        Int64 cycle = 1;
        string currentline = "";
        string nextline = "";
        public TextTraceParser(string tracefile)
        {
            this.trace_path = tracefile;
            fs_read = new FileStream(trace_path, FileMode.Open);
            fs_out = new FileStream("tmp.out", FileMode.OpenOrCreate);
            sr = new StreamReader(fs_read);
            sw = new StreamWriter(fs_out);
        }
        ~TextTraceParser()
        {
            sw.Close();
            sr.Close();
            fs_out.Close();
            fs_read.Close();
        }
        public bool Parse()
        {
            currentline = sr.ReadLine();


            while ((nextline = sr.ReadLine()) != null)
            {

                //if (cycle > configs.cycle)
                //   break;
                if (currentline.Split(':')[0].Replace(" ", "") == nextline.Split(':')[0].Replace(" ", ""))
                {
                    currentline = nextline;
                    continue;
                }
                string[] split = currentline.Split(':');
                string cpu = split[1].Replace("system.cpu", "").Split(' ')[1];

                string rest = currentline.Substring(currentline.IndexOf(split[3]));
                string instruction = "";
                string index = "";
                string op = "";
                string ad = "";
                foreach (string s in split)
                {
                    if (s.Contains("Mem") || s.Contains("Alu") || s.Contains("Int") || s.Contains("No"))
                        index = s;
                }
                //  Debug.Assert(index != "");

                string[] res = Regex.Split(rest, index);
                instruction = res[0].Substring(0, res[0].Length - 2);
                instruction = instruction.Substring(instruction.IndexOf(":") + 2).Replace("   ", " ");
                rest = res[1];


                //  Debug.Assert(instruction != "");
                if (index.Contains("Mem"))
                {
                    index = index.Replace(" ", "").Replace("Mem", "");
                    if (index.Equals("Read"))
                    {
                        op = "R";
                    }
                    if (index.Equals("Write"))
                    {
                        op = "W";
                    }
                    // Debug.Assert(op != "");
                    ad = rest.Substring(rest.IndexOf("D="));
                    //  Debug.Assert(ad != "");
                }
                string write = cpu + spliter + instruction;
                if (op != "")
                {
                    write += spliter + op + spliter + ad;
                }
                sw.WriteLine(write);
                cycle++;

                currentline = nextline;
            }
            return true;
        }
    }
}
