#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using SimplePIM.PIM;
using SimplePIM.Memory.DDR;
using SimplePIM.Statistics;
using SimplePIM.Memory;

#endregion

namespace SimplePIM.Configs
{
    public static class Config
    {
        #region Processor Config
        /// <summary>
        /// Processor count
        /// </summary>
        public static int N = 1;

        /// <summary>
        /// Instruction per cycle
        /// </summary>
        public static int IPC = 1;

        /// <summary>
        /// writeback enable
        /// true: enable write-back mode   false :enable write-through mode
        /// </summary>
        public static bool writeback = true;         

        /// <summary>
        /// Block size
        /// </summary>
        public static int block_size;

        /// <summary>
        /// Log2(block_size)
        /// </summary>
        public static int block_size_bit = 6;

        /// <summary>
        /// Instruction Window queue size
        /// </summary>
        public static int ins_w_size = 256;

        /// <summary>
        /// writeback queue depth
        /// </summary>
        public static int writeback_queue_size = 128;

        /// <summary>
        /// mshr entries
        /// </summary>
        public static int mshr_size = 32;

        /// <summary>
        /// memory queue depth
        /// </summary>
        public static int crtl_queue_max = 128;

        /// <summary>
        /// memory queue latency
        /// </summary>
        public static int mc_latency = 0;

        /// <summary>
        /// Page size
        /// </summary>
        public static UInt64 page_size = 4 * 1024;

        //ALU settings
        public static int adder_count = 2;

        public static int multi_count = 1;

        public static uint operationcode_length = 32;

        /// <summary>
        /// for x86 and x64 architecture, only 48 of 64 bit is vaild.
        /// </summary>
        public static uint address_bit = 48;

        /// <summary>
        /// clock factor
        /// </summary>
        public static double host_clock_factor = 1;

        /// <summary>
        /// core frequent
        /// </summary>
        public static UInt64 proc_frequent = 4 * (UInt64)Math.Pow(2, 30);


        #endregion

        #region Cache Config

        /// <summary>
        /// use shared cache
        /// </summary>
        public static bool shared_cache = true;

        /// <summary>
        /// use cache
        /// </summary>
        public static bool use_cache = true;

        /// <summary>
        /// private cache size
        /// </summary>
        public static int l1cache_size;

        /// <summary>
        /// log2(l1cache_size) 
        /// </summary>
        public static int max_l1cache_bit = 16;

        /// <summary>
        /// l1cache associativity
        /// </summary>
        public static int l1cache_assoc = 4;

        /// <summary>
        /// shared cache size
        /// </summary>
        public static int shared_cache_size;

        /// <summary>
        /// shared cache associativity
        /// </summary>
        public static int shared_cache_assoc = 16;

        /// <summary>
        /// log2(shared_cache_size)
        /// </summary>
        public static int max_scache_bit = 21;

        /// <summary>
        /// private hit latency
        /// </summary>
        public static UInt64 l1cache_hit_latency = 1;

        /// <summary>
        /// shared cache latency
        /// </summary>
        public static UInt64 share_cache_hit_latecy = 200;
        #endregion

        #region Instruction Partitioner Config

        /// <summary>
        /// max ins_p buffer queue
        /// </summary>
        public static int max_insp_waitting_queue = 100;

        #endregion

        #region Debug Flags


        public static bool DEBUG_CACHE = true;
        public static bool DEBUG_TRACE = true;
        public static bool DEBUG_MTRL = true;
        public static bool DEBUG_COHERENCE = true;
        public static bool DEBUG_PROC = true;
        public static bool DEBUG_ALU = true;
        public static bool DEBUG_ALU_PIPELINE = true;
        public static bool DEBUG_PIM = true;
        public static bool DEBUG_INSP = true;
        public static bool DEBUG_MEMORY = true;

        #endregion

        #region Trace Config
        /// <summary>
        /// trace folder path
        /// </summary>
        public static string trace_path = "";

        /// <summary>
        /// config_file path
        /// </summary>
        public static string config_path = "";

        /// <summary>
        /// output file 
        /// </summary>
        public static string output_file = "";

        public static string dram_config_file => config_path + Path.DirectorySeparatorChar+"DRAM.ini";
        public static string hmc_config_file => config_path + Path.DirectorySeparatorChar+"hmc_config.ini";
        public static string config_file => config_path + Path.DirectorySeparatorChar+"config.ini";

        public static string pim_config_file => Config.config_path + Path.DirectorySeparatorChar+"PIM_Settings.ini";


        public static Trace_Type trace_type = Trace_Type.Detailed;
        #endregion

        #region Statistics Config

        public static UInt64 proc_static_period = 10000;
        public static UInt64 pim_static_period = 10000;

        #endregion

        #region Memory Config
        public static RAM_TYPE ram_type = RAM_TYPE.DRAM;

        /// <summary>
        /// Memory list
        /// </summary>
        public static List<KeyValuePair<string, int>> memory = new List<KeyValuePair<string, int>>();
        public static int channel = 1;
        public static int rank = 0;
        public static int bank = 0;

        public static DRAMConfig dram_config = null;

        public static HMCConfig hmc_config = null;


        #endregion

        #region  Simulation Config

        public static UInt64 sim_cycle = 10000;
        public static SIM_TYPE sim_type = SIM_TYPE.file;
        public static bool use_pim = true;
        public static bool register_level_check = true;

        #endregion

        #region File Handle

        public static FileStream fs;
        public static StreamWriter sw;

        #endregion

        #region Public Methods

        /// <summary>
        /// Set value by name
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

        /// <summary>
        /// revise numbers
        /// </summary>
        public static void checkAllReady()
        {
            if (dram_config != null)
            {
                rank += (Int32)dram_config.NUM_RANKS;
                bank += (Int32)dram_config.NUM_BANKS;
            }
            if (hmc_config != null)
            {
                rank += (Int32)hmc_config.num_vaults;
                bank += (Int32)hmc_config.num_banks;
            }
            if (output_file != "")
            {
                fs = new FileStream(output_file, FileMode.Create);
                sw = new StreamWriter(fs);
                DEBUG.set_writer(ref sw);
            }

        }

        /// <summary>
        /// calculate cache size and init ram
        /// </summary>
        public static void initial()
        {
            Calulate_CacheSize();

            if (ram_type == RAM_TYPE.DRAM || ram_type == RAM_TYPE.PCM)
            {
                dram_config = new DRAMConfig();
                Config.dram_config.ReadIniFile(Config.dram_config_file);

                // If we have any overrides, set them now before creating all of the memory objects

                Config.dram_config.InitEnumsFromStrings();
                if (!Config.dram_config.CheckIfAllSet())
                {
                    Environment.Exit(-1);
                }
                if (Config.dram_config.NUM_CHANS == 0)
                {
                    DEBUG.WriteLine("ERROR:  Zero channels");
                    Environment.Exit(-1);
                }
                if (Config.dram_config.NUM_RANKS == 0)
                {
                    Config.dram_config.NUM_RANKS = 1;
                }
            }
            else
            {
                if (ram_type == RAM_TYPE.HMC)
                {
                    hmc_config = new HMCConfig();
                    hmc_config.initConfig(hmc_config_file);
                }
                else
                {
                    //hybrid
                    if (memory.Any(s => s.Key == "HMC"))
                    {
                        hmc_config = new HMCConfig();
                        hmc_config.initConfig(hmc_config_file);
                    }
                    if(memory.Any(s => s.Key == "DRAM")|| memory.Any(s => s.Key == "PCM"))
                    {
                        dram_config = new DRAMConfig();
                        Config.dram_config.ReadIniFile(Config.dram_config_file);

                        // If we have any overrides, set them now before creating all of the memory objects

                        Config.dram_config.InitEnumsFromStrings();
                        if (!Config.dram_config.CheckIfAllSet())
                        {
                            Environment.Exit(-1);
                        }
                        if (Config.dram_config.NUM_CHANS == 0)
                        {
                            DEBUG.WriteLine("ERROR:  Zero channels");
                            Environment.Exit(-1);
                        }
                        if (Config.dram_config.NUM_RANKS == 0)
                        {
                            Config.dram_config.NUM_RANKS = 1;
                        }
                    }

                }
            }
            checkAllReady();


        }

        /// <summary>
        /// calculate cache size
        /// </summary>
        /// <returns></returns>
        public static bool Calulate_CacheSize()
        {
            try
            {
                block_size = 1 << block_size_bit;
                if (use_cache)
                {
                    l1cache_size = 1 << max_l1cache_bit;
                    if (shared_cache)
                        shared_cache_size = 1 << max_scache_bit;
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

        /// <summary>
        /// read config file
        /// </summary>
        public static void read_configs()
        {
            try
            {
                FileStream fs = new FileStream(config_file, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains(";"))
                        line = line.Substring(0, line.IndexOf(";"));
                    if (line.StartsWith("#"))
                        continue;
                    if (line.Contains("="))
                    {

                        string[] split = line.Replace(" ", "").Split('=');
                        if (line.Count() % 2 != 0)
                        {
                            DEBUG.WriteLine("Please make sure the line is correct: " + line);
                            continue;
                        }
                        if (split[0] == "RAM")
                        {
                            string[] ram = split[1].Replace("[", "").Replace("]", "").Split(',');
                            if (ram.Count() % 2 != 0)
                            {
                                DEBUG.WriteLine("Please make sure the line is correct: " + line);
                                Environment.Exit(2);
                            }
                            for (int i = 0; i < ram.Count(); i += 2)
                            {
                                memory.Add(new KeyValuePair<string, int>(ram[i + 1], Int16.Parse(ram[i])));
                            }
                            if (memory.Count <= 0)
                            {
                                DEBUG.Error("No Memory?");
                                Environment.Exit(2);
                            }
                            else
                            {
                                if (memory.Count == 1)
                                {
                                    if (memory[0].Key == "HMC")
                                        Config.ram_type = RAM_TYPE.HMC;
                                    else
                                    {
                                        if (memory[0].Key == "DRAM" )
                                            Config.ram_type = RAM_TYPE.DRAM;
                                        else
                                        {
                                            if(memory[0].Key == "PCM")
                                            {
                                                Config.ram_type = RAM_TYPE.PCM;
                                            }
                                            else
                                            {
                                                DEBUG.Error("Error in input Memory Type.");
                                                Environment.Exit(2);
                                            }
                                                
                                        }
                                    }
                                }
                                else
                                {
                                    Config.ram_type = RAM_TYPE.HYBRID;
                                }
                            }
                            continue;
                        }
                        SetValue(split[0], split[1]);
                    }
                }
            }
            catch
            {
                DEBUG.WriteLine("ERROR : cannot read configs. ");

            }
        }

        #endregion

    }
    public enum PIM_input_type
    {
        Specified,  //use "PIM_" label to identify what should be executed at memory-side.
        All     //specify a type of ins so that they are all executed at memory-side
    }
    public enum SIM_TYPE
    {
        cycle,  
        file    //while running out of trace files
    }
    public enum RAM_TYPE
    {
        DRAM,
        PCM,
        HMC,
        HYBRID
    }
    public enum Trace_Type
    {
        Detailed,
        General
    }
}
