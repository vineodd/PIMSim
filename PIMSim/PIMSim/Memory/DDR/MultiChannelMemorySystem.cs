using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Procs;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.Memory.DDR
{
    public class MultiChannelMemorySystem : DRAMSimObject
    {
        public List<Proc> proc;
        public void addressMapping(UInt64 physicalAddress, ref int newTransactionChan, ref int newTransactionRank, ref int newTransactionBank, ref int newTransactionRow, ref int newTransactionColumn)
        {
            UInt64 tempA, tempB;
            int transactionSize = (int)Config.dram_config.TRANSACTION_SIZE;
            UInt64 transactionMask = (UInt64)transactionSize - 1; //ex: (64 bit bus width) x (8 Burst Length) - 1 = 64 bytes - 1 = 63 = 0x3f mask
            int channelBitWidth = (int)Config.dram_config.NUM_CHANS_LOG;
            int rankBitWidth = (int)Config.dram_config.NUM_RANKS_LOG;
            int bankBitWidth = (int)Config.dram_config.NUM_BANKS_LOG;
            int rowBitWidth = (int)Config.dram_config.NUM_ROWS_LOG;
            int colBitWidth = (int)Config.dram_config.NUM_COLS_LOG;
            // this forces the alignment to the width of a single burst (64 bits = 8 bytes = 3 address bits for DDR parts)
            int byteOffsetWidth = (int)Config.dram_config.BYTE_OFFSET_WIDTH;
            // Since we're assuming that a request is for BL*BUS_WIDTH, the bottom bits
            // of this address *should* be all zeros if it's not, issue a warning

            if ((physicalAddress & transactionMask) != 0)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("WARNING: address 0x" + physicalAddress.ToString("X") + " is not aligned to the request size of " + transactionSize);
            }

            // each burst will contain JEDEC_DATA_BUS_BITS/8 bytes of data, so the bottom bits (3 bits for a single channel DDR system) are
            // 	thrown away before mapping the other bits
            physicalAddress >>= byteOffsetWidth;

            // The next thing we have to consider is that when a request is made for a
            // we've taken into account the granulaity of a single burst by shifting 
            // off the bottom 3 bits, but a transaction has to take into account the
            // burst length (i.e. the requests will be aligned to cache line sizes which
            // should be equal to transactionSize above). 
            //
            // Since the column address increments internally on bursts, the bottom n 
            // bits of the column (colLow) have to be zero in order to account for the 
            // total size of the transaction. These n bits should be shifted off the 
            // address and also subtracted from the total column width. 
            //
            // I am having a hard time explaining the reasoning here, but it comes down
            // this: for a 64 byte transaction, the bottom 6 bits of the address must be 
            // zero. These zero bits must be made up of the byte offset (3 bits) and also
            // from the bottom bits of the column 
            // 
            // For example: cowLowBits = log2(64bytes) - 3 bits = 3 bits 
            int colLowBitWidth = (int)Config.dram_config.COL_LOW_BIT_WIDTH;

            physicalAddress >>= colLowBitWidth;
            int colHighBitWidth = colBitWidth - colLowBitWidth;
            if (Config.dram_config.DEBUG_ADDR_MAP)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("Bit widths: ch:" + channelBitWidth + " r:" + rankBitWidth + " b:" + bankBitWidth
                        + " row:" + rowBitWidth + " colLow:" + colLowBitWidth
                        + " colHigh:" + colHighBitWidth + " off:" + byteOffsetWidth
                        + " Total:" + (channelBitWidth + rankBitWidth + bankBitWidth + rowBitWidth + colLowBitWidth + colHighBitWidth + byteOffsetWidth));
            }

            //perform various address mapping schemes
            if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme1)
            {
                //chan:rank:row:col:bank
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);

            }
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme2)
            {
                //chan:row:col:bank:rank
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);

            }
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme3)
            {
                //chan:rank:bank:col:row
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);

            }
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme4)
            {
                //chan:rank:bank:row:col
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);

            }
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme5)
            {
                //chan:row:col:rank:bank

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);


            }
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme6)
            {
                //chan:row:bank:rank:col

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);


            }
            // clone of scheme 5, but channel moved to lower bits
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme.Scheme6)
            {
                //row:col:rank:bank:chan
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> channelBitWidth;
                tempB = physicalAddress << channelBitWidth;
                newTransactionChan = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rankBitWidth;
                tempB = physicalAddress << rankBitWidth;
                newTransactionRank = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> colHighBitWidth;
                tempB = physicalAddress << colHighBitWidth;
                newTransactionColumn = (int)(tempA ^ tempB);

                tempA = physicalAddress;
                physicalAddress = physicalAddress >> rowBitWidth;
                tempB = physicalAddress << rowBitWidth;
                newTransactionRow = (int)(tempA ^ tempB);

            }

            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Error - Unknown Address Mapping Scheme");
                Environment.Exit(-1);
            }
            if (Config.dram_config.DEBUG_ADDR_MAP)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("Mapped Ch=" + newTransactionChan + " Rank=" + newTransactionRank
                        + " Bank=" + newTransactionBank + " Row=" + newTransactionRow
                        + " Col=" + newTransactionColumn + "\n");
            }

        }
        public uint dramsim_log2(ulong value)
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

        public bool isPowerOfTwo(ulong x)
        {
            return (1UL << (int)dramsim_log2(x)) == x;
        }
        public MultiChannelMemorySystem(string systemIniFilename_,  uint megsOfMemory_)
        {


            megsOfMemory = megsOfMemory_;

            systemIniFilename = systemIniFilename_;


            //  clockDomainCrosser = (new ClockDomain::Callback<MultiChannelMemorySystem, void>(this, &MultiChannelMemorySystem::actual_update));
            clockDomainCrosser = new ClockDomainCrosser(new ClockUpdateCB(this.actual_update));

            currentClockCycle = 0;

            if (!isPowerOfTwo(megsOfMemory))
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("ERROR:  Please specify a power of 2 memory size");
                Environment.Exit(1);
            }
            if(Config.DEBUG_MEMORY)
                DEBUG.WriteLine("DEBUG: == Loading system model file '" + systemIniFilename + "' == ");
            channels = new List<MemorySystem>();
            for (uint i = 0; i < Config.dram_config.NUM_CHANS; i++)
            {
                MemorySystem channel = new MemorySystem(i, megsOfMemory / Config.dram_config.NUM_CHANS, ref dramsim_log);
                channels.Add(channel);
            }
        }
        public DRAMConfig ir = new DRAMConfig();

        public bool addTransaction(Transaction trans)
        {
            // copy the transaction and send the pointer to the new transaction 
            return addTransaction(new Transaction(trans));
        }
        public bool addTransaction(ref Transaction trans)
        {
            uint channelNumber = findChannelNumber(trans.address);
            return channels[(int)channelNumber].addTransaction(trans);
        }
        public bool addTransaction(bool isWrite, UInt64 addr, CallBackInfo callback)
        {
            uint channelNumber = findChannelNumber(addr);
            return channels[(int)channelNumber].addTransaction(isWrite, addr, callback);
        }
        public bool willAcceptTransaction()
        {
            for (int c = 0; c < Config.dram_config.NUM_CHANS; c++)
            {
                if (!channels[c].WillAcceptTransaction())
                {
                    return false;
                }
            }
            return true;
        }
        public bool willAcceptTransaction(UInt64 addr)
        {
            int chan = 0, rank = 0, bank = 0, row = 0, col = 0;
            addressMapping(addr, ref chan, ref rank, ref bank, ref row, ref col);
            return channels[(int)chan].WillAcceptTransaction();
        }
        public override void update()
        {
            clockDomainCrosser.update();
        }
        public void printStats(bool finalStats = false)
        {
            for (int i = 0; i < Config.dram_config.NUM_CHANS; i++)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("==== Channel [" + i + "] ====");
                channels[i].printStats(finalStats);
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("//// Channel [" + i + "] ////");
            }
        }
        public FileStream getLogFile()
        {
            return (FileStream)dramsim_log;
        }
        //   public void RegisterCallbacks(TransactionCompleteCB readDone, TransactionCompleteCB* writeDone,  (*reportPower)(double bgpower, double burstpower, double refreshpower, double actprepower))){}
        public void RegisterCallbacks(Callback_t readDone, Callback_t writeDone, powerCallBack_t reportPower)
        {
            for (int i = 0; i < Config.dram_config.NUM_CHANS; i++)
            {
                channels[i].RegisterCallbacks(readDone, writeDone, reportPower);
            }
        }


        public int getIniBool(string field, bool val)
        {
            if (!Config.dram_config.CheckIfAllSet())
                Environment.Exit(1);
            return Config.dram_config.getBool(field, ref val);
        }


        public int getIniUint(string field, uint val)
        {
            if (!Config.dram_config.CheckIfAllSet())
                Environment.Exit(1);
            return Config.dram_config.getUint(field, ref val);
        }
        public int getIniUint64(string field, UInt64 val)
        {
            if (!Config.dram_config.CheckIfAllSet())

                Environment.Exit(1);
            return Config.dram_config.getUint64(field, ref val);
        }
        public int getIniFloat(string field, float val)
        {
            if (!Config.dram_config.CheckIfAllSet())
                Environment.Exit(1);
            return Config.dram_config.getFloat(field, ref val);
        }

        public void InitOutputFiles(string tracefilename)
        {


            string sim_description_str = null;


            string sim_description = Environment.GetEnvironmentVariable("SIM_DESC");
            if (sim_description != null)
            {
                sim_description_str = sim_description;
            }


            // create a properly named verification output file if need be and open it
            // as the stream 'cmd_verify_out'

            // This sets up the vis file output along with the creating the result
            // directory structure if it doesn't exist

            if (Config.dram_config.LOG_OUTPUT)
            {
                string dramsimLogFilename = "dramsim";
                if (sim_description != null)
                {
                    dramsimLogFilename += "." + sim_description_str;
                }
            }
        }
        public void setCPUClockSpeed(UInt64 cpuClkFreqHz)
        {
            UInt64 dramsimClkFreqHz = (UInt64)(1.0 / (Config.dram_config.tCK * 1e-9));
            clockDomainCrosser.clock1 = dramsimClkFreqHz;
            clockDomainCrosser.clock2 = (cpuClkFreqHz == 0) ? dramsimClkFreqHz : cpuClkFreqHz;
        }

        //output file
        public Stream visDataOut;
        public Stream dramsim_log;


        public uint findChannelNumber(UInt64 addr)
        {
            // Single channel case is a trivial shortcut case 
            if (Config.dram_config.NUM_CHANS == 1)
            {
                return 0;
            }

            if (!isPowerOfTwo(Config.dram_config.NUM_CHANS))
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("ERROR  We can only support power of two # of channels.\n" +
                        "I don't know what Intel was thinking, but trying to address map half a bit is a neat trick that we're not sure how to do");
                Environment.Exit(1);
            }

            // only chan is used from this set 
            int channelNumber = 0, rank = 0, bank = 0, row = 0, col = 0;
            addressMapping(addr, ref channelNumber, ref rank, ref bank, ref row, ref col);
            if (channelNumber >= Config.dram_config.NUM_CHANS)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("ERROR Got channel index " + channelNumber + " but only " + Config.dram_config.NUM_CHANS + " exist");
                Environment.Exit(1);
            }
            //DEBUG("Channel idx = "<<channelNumber<<" totalbits="<<totalBits<<" channelbits="<<channelBits); 

            return (uint)channelNumber;
        }
        public void actual_update()
        {
            if (currentClockCycle == 0)
            {
                InitOutputFiles(traceFilename);
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("DEBUG :  DRAMSim2 Clock Frequency =" + clockDomainCrosser.clock1 + "Hz, CPU Clock Frequency=" + clockDomainCrosser.clock2 + "Hz");
            }

            if (currentClockCycle % Config.dram_config.EPOCH_LENGTH == 0)
            {

                for (int i = 0; i < Config.dram_config.NUM_CHANS; i++)
                {
                    channels[i].printStats(false);
                }

            }

            for (int i = 0; i < Config.dram_config.NUM_CHANS; i++)
            {
                channels[i].update();
            }


            currentClockCycle++;
        }
        public List<MemorySystem> channels;
        public uint megsOfMemory;
        public string systemIniFilename;
        public string traceFilename;
        public ClockDomainCrosser clockDomainCrosser;
        public void mkdirIfNotExist(string path)
        {
            int i = 0;
            i++;
        }
        public bool fileExists(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }
            return false;
        }
        public string FilenameWithNumberSuffix(string filename, string extension, uint maxNumber = 100)
        {

            string currentFilename = filename + extension;
            if (!fileExists(currentFilename))
            {
                return currentFilename;
            }

            // otherwise, add the suffixes and test them out until we find one that works
            StringBuilder tmpNum = new StringBuilder();
            tmpNum.Append(".");//<<1; 
            for (uint i = 1; i < maxNumber; i++)
            {
                currentFilename = filename + tmpNum.ToString() + extension;
                if (fileExists(currentFilename))
                {
                    currentFilename = filename;
                    //tmpNum.seekp(0);
                    //tmpNum << "." << i;
                }
                else
                {
                    return currentFilename;
                }
            }
            // if we can't find one, just give up and return whatever is the current filename
            if (Config.DEBUG_MEMORY)
                DEBUG.WriteLine("ERROR  Warning: Couldn't find a suitable suffix for " + filename);
            return currentFilename;
        }
    }

}

