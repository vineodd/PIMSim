#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.PIM;
using System.IO;
using PIMSim.Statistics;
using PIMSim.General;
#endregion

namespace PIMSim.Configs
{
    /// <summary>
    /// PIM config class
    /// </summary>
    public static class PIMConfigs
    {
        #region Public Variables

        /// <summary>
        /// PIM unit type
        /// </summary>
        public static PIM_Unit_Type unit_type;


        public static int PIM_adder_count = 2;
        public static int PIM_multi_count = 1;
        public static int PIM_clock_factor = 1;

        public static Consistency Consistency_Model = Consistency.SpinLock;

        /// <summary>
        /// how PIM get inputs
        /// </summary>
        public static PIM_input_type PIM_Fliter = PIM_input_type.Specified;

        /// <summary>
        /// ins type that must be executed at memory-side.
        /// used when PIM_Fliter = ALL
        /// </summary>
        public static List<string> PIM_Ins_List = new List<string>();

        /// <summary>
        /// MAX PIM unit count
        /// </summary>
        public static int max_pim_block = 15;


        public static int pim_cu_count => CU_Name.Count();

        /// <summary>
        /// PIMProc count
        /// used when unit_type = Processors
        /// </summary>
        public static int N = 1;

        

        /// <summary>
        /// used when unit_type = Processors
        /// </summary>
        public static int IPC = 1;

        /// <summary>
        /// used when unit_type = Processors
        /// </summary>
        public static bool use_l1_cache = true;

        /// <summary>
        /// CU names
        /// </summary>
        public static List<string> CU_Name = new List<string>();

        public static int stage = 0;

        /// <summary>
        /// used when unit_type = Processors
        /// </summary>
        public static bool writeback = true;

        /// <summary>
        /// Stages
        /// </summary>
        public static List<string> stage_name = new List<string>();

        /// <summary>
        /// Memory methods
        /// Bypass : addictional circuits added to suport none-conventional fast data storing and loading operations. 
        /// Conventional : add to MTRL
        /// </summary>
        public static PIM_Load_Method memory_method = PIM_Load_Method.Conventional;

        /// <summary>
        /// To indeicate PIM kernal addresses
        /// </summary>
        public static List<AddressRange> PIM_kernal = new List<AddressRange>();


        //cache
        public static int max_l1cache_bit = 16;
        public static int l1cache_size;
        public static int l1cache_assoc = 4;
        public static int ins_w_size = 256;
        public static int writeback_queue_size = 128;
        public static int mshr_size = 32;
        public static uint l1_cacheline_size = 64;

        public static UInt64 l1cache_hit_latency = 1;

        #endregion

        #region Methods

        /// <summary>
        /// read config file
        /// </summary>
        public static void initConfig()
        {
            FileStream fs = new FileStream(Config.pim_config_file, FileMode.Open);
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
                    DEBUG.WriteLine("Error in parsing line.");
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
                if ((split[0] == "Consistency_Model"))
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
                    foreach (var x in cus)
                    {
                        CU_Name.Add(x);
                    }
                    continue;
                }
                if ((split[0] == "Stage_List"))
                {
                    string[] split_stage = split[1].Split(',');
                    foreach (string s in split_stage)
                    {
                        stage_name.Add(s);
                    }
                    stage = stage_name.Count();
                    continue;
                }
                if ((split[0] == "PIM_Load_Method"))
                {
                    string split_method = split[1];
                    if (split_method == "Bypass")
                    {
                        memory_method = PIM_Load_Method.Bypass;
                    }
                    else
                    {
                        memory_method = PIM_Load_Method.Conventional;
                    }
                    continue;
                }
                if ((split[0] == "PIM_Kernal"))
                {
                    string split_method = split[1].Replace("(", ""); 
                    var kernal = split_method.Split(')').ToList().Where(x=>x!="").ToList();
                    kernal.ForEach(x => { var st = x.Split(',').ToList();  PIM_kernal.Add(new AddressRange(Convert.ToUInt64(st[0], 16), Convert.ToUInt64(st[1], 16))); });
                    continue;
                }
                SetValue(split[0], split[1]);

            }
            sr.Close();
            fs.Close();
            Calulate_CacheSize();
        }

        /// <summary>
        /// set value by variable name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetValue(string name, object value)
        {
            try
            {
                var s = typeof(Config).GetField(name).GetValue(name);
                typeof(Config).GetField(name).SetValue(name, Convert.ChangeType(value, s.GetType()));
            }
            catch 
            {
                DEBUG.WriteLine("WARNING: Failed to set Parms:" + name + " = " + value.ToString() + ", plz check if necessary.");
                return false;
            }
            return true;
        }
        public static bool Calulate_CacheSize()
        {
            try
            {

                if (use_l1_cache)
                {
                    l1cache_size = 1 << max_l1cache_bit;
                }
            }
            catch
            {
                DEBUG.WriteLine("ERROR : Faield to set $ and block size.");
                Environment.Exit(1);    //exit code 1: config set failed
                return false;
            }
            return true;
        }
        #endregion
    }
    public enum PIM_Unit_Type
    {
        Processors,     //Assume PIM CU are processors
        Pipeline        //Assume PIM CU are pipeline
    }
    public enum PIM_Load_Method
    {
        Bypass, //addictional circuits added to suport none-conventional data store and load operations.
        Conventional //traditional ways.
    }
}
