using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using SimplePIM.PIM;
using SimplePIM.Memory.DDR;
using SimplePIM.Statics;

namespace SimplePIM.Configs
{
    public static class Config
    {
        //------------processor configs------------------
        static public int N = 1;
        static public int IPC = 1;
        static public bool wb = true;         //true: enable write-back mode   false :enable write-through mode
        static public bool shared_cache = true;    //true: 
        static public bool use_cache = true;
        static public int l1cache_size;
        static public int l1cache_assoc=4 ;
        static public int shared_cache_size;
        static public int shared_cache_assoc=16;
        static public int block_size;

        static public int max_l1cache_bit=16;
        static public int max_scache_bit=21;
        static public int max_block_size_bit=6;

        static public int ins_w_size = 256;
        static public int writeback_queue_size = 128;
        static public int mshr_size = 32;

        static public UInt64 l1cache_hit_latency = 1;
        static public UInt64 share_cache_hit_lantecy = 200;

        //MCRTL settings
        static public int crtl_queue_max = 128;
        static public int mc_latency = 0;

        //Instruction Partitioner configs
        static public int max_insp_count = 100;

        //DEBUG tag

        static public bool DEBUG_CACHE = true;
        static public bool DEBUG_TRACE = true;
        public static bool DEBUG_MTRL = true;
        public static bool DEBUG_COHERENCE = true;
        public static bool DEBUG_PROC = true;
        public static bool DEBUG_ALU = true;
        public static bool DEBUG_PIM = true;
        public static bool DEBUG_INSP = true;

        //trace settings
        static public string trace_path = "";
        static public string config_file = "";
        public static string output_file = "";
        public static string hmc_config_path => config_file + @"\hmc_config.ini";

        //static settings
        static public UInt64 static_period = 10000;

        //simulation settings
        static public UInt64 sim_cycle = 10000;
        static public SIM_TYPE sim_type = SIM_TYPE.cycle;


        //RAM settings
        static public int channel=1;
        static public int rank=0;
        static public int bank=0;
        static public UInt64 page_size = 4 * 1024;
        static public UInt64 block_max;

        static public int xbar_latency = 16;

        //ALU settings
        static public int add_ability = 2;
        static public int multi_ability = 1;
        static public int para_load = 2;

        static public FileStream fs;
        static public StreamWriter sw;

        //PIM settings
        public static PIMConfigs pim_config = new PIMConfigs();
        //
        public static DRAMConfig dram_config = null;

        public static HMCConfig hmc_config = null;
      //  public 
        public static bool SetValue(string name,object value)
        {
            try
            {
                var s = typeof(Config).GetField(name).GetValue(name);
                typeof(Config).GetField(name).SetValue(name, Convert.ChangeType(value, s.GetType()));
            }
            catch(Exception e)
            {
                Console.WriteLine("WARNING: Failed to set Parms:" + name + " = " + value.ToString() + ", plz check if necessary.");
                return false;
            }
            return true;
        }
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

        public static void initial()
        {
            Calulate_CacheSize();
            
            if(pim_config.ram_type == RAM_TYPE.DRAM|| pim_config.ram_type ==RAM_TYPE.PCM)
            {
                dram_config = new DRAMConfig();
                Config.dram_config.ReadIniFile(Config.dram_config. systemIniFilename);

                // If we have any overrides, set them now before creating all of the memory objects
                
                Config.dram_config.InitEnumsFromStrings();
                if (!Config.dram_config.CheckIfAllSet())
                {
                    Environment.Exit(-1);
                }
                if (Config.dram_config.NUM_CHANS == 0)
                {
                    Console.WriteLine("ERROR:  Zero channels");
                    Environment.Exit(-1);
                }
                if (Config.dram_config.NUM_RANKS == 0)
                {
                    Config.dram_config.NUM_RANKS = 1;
                }
            }
            else
            {
                if (pim_config.ram_type == RAM_TYPE.HMC)
                {
                    hmc_config = new HMCConfig();
                    hmc_config.initConfig(hmc_config_path);
                }
                else
                {
                    //hybrid


                }
            }
            checkAllReady();
            

        }
        public static bool Calulate_CacheSize()
        {
            try
            {
                block_size = 1 << max_block_size_bit;
                if (use_cache)
                {
                    l1cache_size = 1 << max_l1cache_bit;
                    if (shared_cache)
                        shared_cache_size = 1 << max_scache_bit;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR : Faield to set $ and block size.");
                Environment.Exit(1);    //exit code 1: config set failed
                return false;
            }
            return true;
        }
        public static void read_configs()
        {
            try
            {
                FileStream fs = new FileStream(config_file, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Substring(0, line.IndexOf(";"));
                    if (line.StartsWith("#"))
                        continue;
                    if (line.Contains("="))
                    {
                        string[] split = line.Substring(0,line.IndexOf(";")) .Replace(" ", "").Split('=');
                        if(line.Count()%2!=0)
                        {
                            Console.WriteLine("Please make sure the line is correct: " + line);
                            continue;
                        }
                        SetValue(split[0], split[1]);
                    }     
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR : cannot read configs. ");

            }
        }
         
    }
    public enum PIM_input_type { Specified, All }
    public enum SIM_TYPE { cycle, file }
    public enum RAM_TYPE
    {
        DRAM,
        PCM,
        HMC,
        HYBRID
    }
}
