#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Statistics;
using PIMSim.Configs;
#endregion

namespace PIMSim.Memory.DDR
{
    /// <summary>
    /// DRAM Configs
    /// </summary>
    public class DRAMConfig
    {
        #region Public Variables
        public bool RETURN_TRANSACTIONS = true;
        public bool LOG_OUTPUT = false;
        public FileStream cmd_verify_out; //used in Rank.cpp and MemoryController.cpp if VERIFICATION_OUTPUT is set
        public RowBufferPolicy rowBufferPolicy;
        public SchedulingPolicy schedulingPolicy;
        public AddressMappingScheme addressMappingScheme;
        public QueuingStructure queuingStructure;
        public const int HISTOGRAM_BIN_SIZE = 10;
        public UInt64 TOTAL_STORAGE;
        public uint NUM_BANKS;
        public uint NUM_BANKS_LOG;
        public uint NUM_CHANS;
        public uint NUM_CHANS_LOG;
        public uint NUM_ROWS;
        public uint NUM_ROWS_LOG;
        public uint NUM_COLS;
        public uint NUM_COLS_LOG;
        public uint DEVICE_WIDTH;
        public uint BYTE_OFFSET_WIDTH;
        public uint TRANSACTION_SIZE;
        public uint THROW_AWAY_BITS;
        public uint COL_LOW_BIT_WIDTH;

        public uint REFRESH_PERIOD;
        public float tCK;
        public float Vdd;
        public uint CL;
        public uint AL;
        public uint BL;
        public uint tRAS;
        public uint tRCD;
        public uint tRRD;
        public uint tRC;
        public uint tRP;
        public uint tCCD;
        public uint tRTP;
        public uint tWTR;
        public uint tWR;
        public uint tRTRS;
        public uint tRFC;
        public uint tFAW;
        public uint tCKE;
        public uint tXP;
        public uint tCMD;

        public uint IDD0;
        public uint IDD1;
        public uint IDD2P;
        public uint IDD2Q;
        public uint IDD2N;
        public uint IDD3Pf;
        public uint IDD3Ps;
        public uint IDD3N;
        public uint IDD4W;
        public uint IDD4R;
        public uint IDD5;
        public uint IDD6;
        public uint IDD6L;
        public uint IDD7;
        public bool NO_STORAGE = false;
        public uint READ_TO_PRE_DELAY=> (AL + BL / 2 + Math.Max(tRTP, tCCD) - tCCD); 
        public uint WRITE_TO_PRE_DELAY=>(WL + BL / 2 + tWR); 
        public uint READ_TO_WRITE_DELAY=>(RL + BL / 2 + tRTRS - WL); 
        public uint READ_AUTOPRE_DELAY=>(AL + tRTP + tRP); 
        public uint WRITE_AUTOPRE_DELAY=>(WL + BL / 2 + tWR + tRP); 
        public uint WRITE_TO_READ_DELAY_B=>(WL + BL / 2 + tWTR);  //interbank
        public uint WRITE_TO_READ_DELAY_R=>(WL + BL / 2 + tRTRS - RL);  //interrank
        public uint RL=>(CL + AL);
        
        public uint WL=>RL - 1;
        

        //in bytes
        public uint JEDEC_DATA_BUS_BITS;

        //Memory Controller related parameters
        public uint TRANS_QUEUE_DEPTH;
        public uint CMD_QUEUE_DEPTH;

        //cycles within an epoch
        public uint EPOCH_LENGTH;

        //row accesses allowed before closing (open page)
        public uint TOTAL_ROW_ACCESSES;

        // strings and their associated enums
        public string ROW_BUFFER_POLICY;
        public string SCHEDULING_POLICY;
        public string ADDRESS_MAPPING_SCHEME;
        public string QUEUING_STRUCTURE;

        public bool DEBUG_TRANS_Q;
        public bool DEBUG_CMD_Q;
        public bool DEBUG_ADDR_MAP;
        public bool DEBUG_BANKSTATE;
        public bool DEBUG_BUS;
        public bool DEBUG_BANKS;
        public bool DEBUG_POWER;
        public bool USE_LOW_POWER;


        public bool VERIFICATION_OUTPUT;

        public bool DEBUG_INI_READER = false;
        //  public Dictionary<string, string> OverrideMap;
        public List<ConfigMap> configMap;


        public uint NUM_DEVICES;
        public uint NUM_RANKS;
        public uint NUM_RANKS_LOG;
        #endregion

        #region Public Methods
        public uint log2(ulong value)
        {
            uint logbase2 = 0;
            ulong orig = value;
            value >>= 1;
            while (value > 0)
            {
                value >>= 1;
                logbase2++;
            }
            if ((uint)1 << (int)logbase2 < orig) logbase2++;
            return logbase2;
        }
        public DRAMConfig()
        {
            configMap = new List<ConfigMap>();
            SetType("NUM_BANKS", paramType.DEV_PARAM, varType.UINT);
            SetType("NUM_ROWS", paramType.DEV_PARAM, varType.UINT);
            SetType("NUM_COLS", paramType.DEV_PARAM, varType.UINT);
            SetType("DEVICE_WIDTH", paramType.DEV_PARAM, varType.UINT);
            SetType("REFRESH_PERIOD", paramType.DEV_PARAM, varType.UINT);
            SetType("tCK", paramType.DEV_PARAM, varType.FLOAT);
            SetType("CL", paramType.DEV_PARAM, varType.UINT);
            SetType("AL", paramType.DEV_PARAM, varType.UINT);
            SetType("BL", paramType.DEV_PARAM, varType.UINT);
            SetType("tRAS", paramType.DEV_PARAM, varType.UINT);
            SetType("tRCD", paramType.DEV_PARAM, varType.UINT);
            SetType("tRRD", paramType.DEV_PARAM, varType.UINT);
            SetType("tRC", paramType.DEV_PARAM, varType.UINT);
            SetType("tRP", paramType.DEV_PARAM, varType.UINT);
            SetType("tCCD", paramType.DEV_PARAM, varType.UINT);
            SetType("tRTP", paramType.DEV_PARAM, varType.UINT);
            SetType("tWTR", paramType.DEV_PARAM, varType.UINT);
            SetType("tWR", paramType.DEV_PARAM, varType.UINT);
            SetType("tRTRS", paramType.DEV_PARAM, varType.UINT);
            SetType("tRFC", paramType.DEV_PARAM, varType.UINT);
            SetType("tFAW", paramType.DEV_PARAM, varType.UINT);
            SetType("tCKE", paramType.DEV_PARAM, varType.UINT);
            SetType("tXP", paramType.DEV_PARAM, varType.UINT);
            SetType("tCMD", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD0", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD1", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD2P", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD2Q", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD2N", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD3Pf", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD3Ps", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD3N", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD4W", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD4R", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD5", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD6", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD6L", paramType.DEV_PARAM, varType.UINT);
            SetType("IDD7", paramType.DEV_PARAM, varType.UINT);
            SetType("Vdd", paramType.DEV_PARAM, varType.FLOAT);
            SetType("NUM_CHANS", paramType.SYS_PARAM, varType.UINT);
            SetType("JEDEC_DATA_BUS_BITS", paramType.SYS_PARAM, varType.UINT);

            //Memory Controller related parameters
            SetType("TRANS_QUEUE_DEPTH", paramType.SYS_PARAM, varType.UINT);
            SetType("CMD_QUEUE_DEPTH", paramType.SYS_PARAM, varType.UINT);
            SetType("EPOCH_LENGTH", paramType.SYS_PARAM, varType.UINT);

            //Power
            SetType("USE_LOW_POWER", paramType.SYS_PARAM, varType.BOOL);
            SetType("TOTAL_ROW_ACCESSES", paramType.SYS_PARAM, varType.UINT);
            SetType("ROW_BUFFER_POLICY", paramType.SYS_PARAM, varType.STRING);
            SetType("SCHEDULING_POLICY", paramType.SYS_PARAM, varType.STRING);
            SetType("ADDRESS_MAPPING_SCHEME", paramType.SYS_PARAM, varType.STRING);
            SetType("QUEUING_STRUCTURE", paramType.SYS_PARAM, varType.STRING);

            // debug flags

            SetType("DEBUG_TRANS_Q", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_CMD_Q", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_ADDR_MAP", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_BANKSTATE", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_BUS", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_BANKS", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEBUG_POWER", paramType.SYS_PARAM, varType.BOOL);
            SetType("VIS_FILE_OUTPUT", paramType.SYS_PARAM, varType.BOOL);
            SetType("DEFINE_BOOL_PARAM", paramType.SYS_PARAM, varType.BOOL);
        }
        public void SetType(string s, paramType p, varType v)
        {
            ConfigMap tp = new ConfigMap();
            tp.iniKey = s;
            tp.parameterType = p;
            tp.variableType = v;
            configMap.Add(tp);
        }
        public void SetKey(string key, string valueString, uint lineNumber = 0)
        {
            int i;
            uint intValue = 0;
            UInt64 int64Value = 0;
            float floatValue = 0;

            for (i = 0; i < configMap.Count; i++)
            {

                // match up the string in the config map with the key we parsed
                if (key.Equals(configMap[i].iniKey))
                {
                    switch (configMap[i].variableType)
                    {
                        //parse and set each type of variable
                        case varType.UINT:

                            try
                            {
                                intValue = UInt32.Parse(valueString);
                            }
                            catch 
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("could not parse line " + lineNumber + " (non-numeric value '" + valueString + "')?");
                            }
                            finally
                            {
                                configMap[i].variablePtr = intValue;
                                typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, intValue);
                            }



                            if (DEBUG_INI_READER)
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("      - SETTING " + configMap[i].iniKey + "=" + intValue);
                            }
                            break;
                        case varType.UINT64:
                            try
                            {
                                int64Value = UInt64.Parse(valueString);
                            }
                            catch 
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("could not parse line " + lineNumber + " (non-numeric value '" + valueString + "')?");
                            }
                            finally
                            {
                                configMap[i].variablePtr = int64Value;
                                typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, int64Value);
                            }


                            if (DEBUG_INI_READER)
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("      - SETTING " + configMap[i].iniKey + "=" + int64Value);
                            }
                            break;
                        case varType.FLOAT:
                            try
                            {
                                floatValue = float.Parse(valueString);
                            }
                            catch 
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("could not parse line " + lineNumber + " (non-numeric value '" + valueString + "')?");
                            }
                            finally
                            {
                                configMap[i].variablePtr = floatValue;
                                typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, floatValue);
                            }


                            if (DEBUG_INI_READER)
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("      - SETTING " + configMap[i].iniKey + "=" + floatValue);
                            }
                            break;
                        case varType.STRING:

                            configMap[i].variablePtr = valueString;
                            typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, valueString);
                            if (DEBUG_INI_READER)
                            {
                                if (Config.DEBUG_MEMORY)
                                    DEBUG.WriteLine("      - SETTING " + configMap[i].iniKey + "=" + valueString);
                            }

                            break;
                        case varType.BOOL:
                            if (valueString == "true" || valueString == "1")
                            {
                                configMap[i].variablePtr = true;
                                typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, true);
                            }
                            else
                            {
                                configMap[i].variablePtr = false;
                                typeof(DRAMConfig).GetField(configMap[i].iniKey).SetValue(this, false);
                            }

                            break;
                    }
                    // lineNumber == 0 implies that this is an override parameter from the command line, so don't bother doing these checks
                    // use the pointer stored in the config map to set the value of the variable
                    // to make sure all parameters are in the ini file
                    configMap[i].wasSet = true;
                    break;
                }
            }

       
        }
       
        public void ReadIniFile(string filename)
        {
            string line = "";
            FileStream fs = null;
            StreamReader sr = null;
            string key, valueString;
            int commentIndex;
            uint lineNumber = 0;
            try
            {

                fs = new FileStream( filename, FileMode.Open);
                sr = new StreamReader(fs);
            }
            catch 
            {

            }
            if (fs.CanRead)
            {
                while ((line = sr.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Replace(" ", "") == "")
                    {
                        continue;
                    }
                    commentIndex = line.IndexOf(";");
                    if (commentIndex == 0)
                    {
                        continue;
                    }
                    else {
                        if (commentIndex > 0)
                        {
                            line = line.Substring(0, commentIndex);
                        }
                        else {
                           
                        }
                    }
                    string[] line_s = line.Replace(" ", "").Replace("\t", "").Split('=');
                    key = line_s[0];
                    valueString = line_s[1];
                    SetKey(key, valueString, lineNumber);

                }
                sr.Close();
                fs.Close();
            }
            else
            {
                DEBUG.WriteLine("ERROR  : Unable to load ini file " + filename);
                Environment.Exit(1);
            }
            /* precompute frequently used values */
            NUM_BANKS_LOG = log2(NUM_BANKS);
            NUM_CHANS_LOG = log2(NUM_CHANS);
            NUM_ROWS_LOG = log2(NUM_ROWS);
            NUM_COLS_LOG = log2(NUM_COLS);
            BYTE_OFFSET_WIDTH = log2(JEDEC_DATA_BUS_BITS / 8);
            TRANSACTION_SIZE = JEDEC_DATA_BUS_BITS / 8 * BL;
            THROW_AWAY_BITS = log2(TRANSACTION_SIZE);
            COL_LOW_BIT_WIDTH = THROW_AWAY_BITS - BYTE_OFFSET_WIDTH;
        }
        public void InitEnumsFromStrings()
        {
            if (ADDRESS_MAPPING_SCHEME == "scheme1")
            {
                addressMappingScheme = AddressMappingScheme.Scheme1;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 1");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme2")
            {
                addressMappingScheme = AddressMappingScheme.Scheme2;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 2");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme3")
            {
                addressMappingScheme = AddressMappingScheme.Scheme3;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 3");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme4")
            {
                addressMappingScheme = AddressMappingScheme.Scheme4;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 4");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme5")
            {
                addressMappingScheme = AddressMappingScheme.Scheme5;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 5");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme6")
            {
                addressMappingScheme = AddressMappingScheme.Scheme6;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 6");
                }
            }
            else if (ADDRESS_MAPPING_SCHEME == "scheme7")
            {
                addressMappingScheme = AddressMappingScheme.Scheme7;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ADDR SCHEME: 7");
                }
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("WARNING: unknown address mapping scheme '" + ADDRESS_MAPPING_SCHEME + "'; valid values are 'scheme1'...'scheme7'. Defaulting to scheme1");
                addressMappingScheme = AddressMappingScheme.Scheme1;
            }

            if (ROW_BUFFER_POLICY == "open_page")
            {
                rowBufferPolicy =RowBufferPolicy. OpenPage;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ROW BUFFER: open page");
                }
            }
            else if (ROW_BUFFER_POLICY == "close_page")
            {
                rowBufferPolicy = RowBufferPolicy.ClosePage;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: ROW BUFFER: close page");
                }
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("WARNING: unknown row buffer policy '" + ROW_BUFFER_POLICY + "'; valid values are 'open_page' or 'close_page', Defaulting to Close Page.");
                rowBufferPolicy = RowBufferPolicy.ClosePage;
            }

            if (QUEUING_STRUCTURE == "per_rank_per_bank")
            {
                queuingStructure =QueuingStructure. PerRankPerBank;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: QUEUING STRUCT: per rank per bank");
                }
            }
            else if (QUEUING_STRUCTURE == "per_rank")
            {
                queuingStructure =QueuingStructure. PerRank;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: QUEUING STRUCT: per rank");
                }
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("WARNING: Unknown queueing structure '" + QUEUING_STRUCTURE + "'; valid options are 'per_rank' and 'per_rank_per_bank', defaulting to Per Rank Per Bank");
                queuingStructure =QueuingStructure. PerRankPerBank;
            }

            if (SCHEDULING_POLICY == "rank_then_bank_round_robin")
            {
                schedulingPolicy =SchedulingPolicy. RankThenBankRoundRobin;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: SCHEDULING: Rank Then Bank");
                }
            }
            else if (SCHEDULING_POLICY == "bank_then_rank_round_robin")
            {
                schedulingPolicy = SchedulingPolicy.BankThenRankRoundRobin;
                if (DEBUG_INI_READER)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("DEBUG: SCHEDULING: Bank Then Rank");
                }
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("WARNING: Unknown scheduling policy '" + SCHEDULING_POLICY + "'; valid options are 'rank_then_bank_round_robin' or 'bank_then_rank_round_robin'; defaulting to Bank Then Rank Round Robin");
                schedulingPolicy = SchedulingPolicy.BankThenRankRoundRobin;
            }
        }
        public bool CheckIfAllSet()
        {
            for (int i = 0; configMap[i].variablePtr != null; i++)
            {
                if (!configMap[i].wasSet)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("WARNING: KEY " + configMap[i].iniKey + " NOT FOUND IN INI FILE.");
                    switch (configMap[i].variableType)
                    {
                        //the string and bool values can be defaulted, but generally we need all the numeric values to be set to continue
                        case varType.UINT:
                        case varType.UINT64:
                        case varType.FLOAT:
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("ERROR: Cannot continue without key '" + configMap[i].iniKey + "' set.");
                            return false;

                        case varType.BOOL:
                            configMap[i].variablePtr = false;
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("DEBUG: \tSetting Default: " + configMap[i].iniKey + "=false");
                            break;
                        case varType.STRING:
                            break;
                    }
                }
            }
            return true;
        }
        public int getBool(string field, ref bool val)
        {
            for (int i = 0; i < configMap.Count; i++)
            {
                if (!field.Equals(configMap[i].iniKey))
                    continue;
                if ((bool)configMap[i].variablePtr != val)
                    return -1;
                val = (bool)configMap[i].variablePtr;
                return 0;
            }
            return -1;
        }
        public int getUint(string field, ref uint val)
        {
            for (int i = 0; i < configMap.Count; i++)
            {
                if (!field.Equals(configMap[i].iniKey))
                    continue;
                if ((uint)configMap[i].variablePtr != val)
                    return -1;
                val = (uint)configMap[i].variablePtr;
                return 0;
            }
            return -1;
        }
        public int getUint64(string field, ref UInt64 val)
        {

            for (int i = 0; i < configMap.Count; i++)
            {
                if (!field.Equals(configMap[i].iniKey))
                    continue;
                if ((UInt64)configMap[i].variablePtr != val)
                    return -1;
                val = (UInt64)configMap[i].variablePtr;
                return 0;
            }
            return -1;
        }
        public int getFloat(string field, ref float val)
        {
            for (int i = 0; i < configMap.Count; i++)
            {
                if (!field.Equals(configMap[i].iniKey))
                    continue;
                if ((float)configMap[i].variablePtr != val)
                    return -1;
                val = (float)configMap[i].variablePtr;
                return 0;
            }
            return -1;
        }
        #endregion
    }
    public enum varType { STRING, UINT, UINT64, FLOAT, BOOL }
    public enum paramType { SYS_PARAM, DEV_PARAM }
    public class ConfigMap
    {
        public string iniKey; //for example "tRCD"

        public object variablePtr;
        public varType variableType;
        public paramType parameterType;
        public bool wasSet;
    }
    public enum TraceType
    {
        k6,
        mase,
        misc
    }

    public enum AddressMappingScheme
    {
        Scheme1,
        Scheme2,
        Scheme3,
        Scheme4,
        Scheme5,
        Scheme6,
        Scheme7
    }
    public enum RowBufferPolicy
    {
        OpenPage,
        ClosePage
    }
    public enum QueuingStructure
    {
        PerRank,
        PerRankPerBank
    }
    public enum SchedulingPolicy
    {
        RankThenBankRoundRobin,
        BankThenRankRoundRobin
    }


}
