using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.PIM;
using System.IO;

namespace SimplePIM.Configs
{
    public class PIMConfigs
    {
        public PIM_Unit_Type unit_type;
        public int PIM_add_ability = 2;
        public int PIM_multi_ability = 1;
        public int PIM_clock_factor = 1;
        public Consistency Consistency_Model = Consistency.SpinLock;
        public UInt64 Load_latency = 5;
        public PIM_input_type PIM_Fliter = PIM_input_type.All;
        public RAM_TYPE ram_type = RAM_TYPE.DRAM;
        public List<string> PIM_Ins_List = new List<string>();
        public int max_pim_block = 15;
        public int pim_cu_count => CU_Name.Count();
        public bool func_general = false;
        public int N = 1;
        public int IPC = 1;
        public bool use_l1_cache = true;
        public string PIM_Settings => Config.config_file+ @"\PIM_Settings.ini";
        public List<string> CU_Name = new List<string>();
        public int stage = 0;
        public bool wb = true;
        public List<string> stage_name = new List<string>();
        public PIMConfigs()
        {

        }
        public void initConfig()
        {
            FileStream fs = new FileStream(PIM_Settings, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains(";"))
                {
                    line = line.Substring(0, line.IndexOf(";") + 1);
                }
                line = line.Replace(" ", "");
                string[] split = line.Split('=');
                if (split.Count() != 2)
                {
                    //error
                    Console.WriteLine("Error in parsing line.");
                    continue;
                }
                if (split[0] == "PIM_Unit_Type")
                {
                    if (split[1] == "Pipeline") unit_type = PIM_Unit_Type.Pipeline;
                    else
                    {
                        unit_type = PIM_Unit_Type.Processors;
                    }
                    continue;
                }
                if ((split[1] == "Consistency_Model"))
                {
                    if (split[1] == "SpinLock") Consistency_Model = Consistency.SpinLock;
                    else
                    {
                        if (split[1] == "NoCache")
                        {
                            Consistency_Model = Consistency.NoCache;
                        }
                        else
                        {
                            Consistency_Model = Consistency.DontCheck;
                        }

                    }
                    continue;
                }
                if ((split[0] == "PIM_Fliter"))
                {
                    if (split[1] == "ALL")
                    {
                        PIM_Fliter = PIM_input_type.All;
                    }
                    else
                    {
                        PIM_Fliter = PIM_input_type.Specified;
                    }
                    continue;
                }
                if ((split[0] == "PIM_Ins_List"))
                {
                    string[] split_ins = split[1].Split(',');
                    foreach (var x in split_ins) PIM_Ins_List.Add(x);
                    continue;
                }
                if ((split[0] == "CU"))
                {
                    var cus = split[1].Split(',');
                    foreach(var x in cus)
                    {
                        CU_Name.Add(x);
                    }
                    continue;
                }
                    if ((split[0] == "Stage_List"))
                {
                    string[] split_stage = split[1].Split(',');
                    foreach(string s in split_stage)
                    {
                        stage_name.Add(s);
                    }
                    stage = stage_name.Count();
                    continue;
                }
                    SetValue(split[0], split[1]);

            }
            sr.Close();
            fs.Close();
        }
        public bool SetValue(string name, object value)
        {
            try
            {
                var s = typeof(PIMConfigs).GetField(name).GetValue(this);
                typeof(PIMConfigs).GetField(name).SetValue(this, Convert.ChangeType(value, s.GetType()));
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: Failed to set Parms:" + name + " = " + value.ToString() + ", plz check if necessary.");
                return false;
            }
            return true;
        }
    }
    public enum PIM_Unit_Type
    {
        Processors,
        Pipeline
    }
    
}
