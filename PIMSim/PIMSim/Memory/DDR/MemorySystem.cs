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
    public class MemorySystem : DRAMSimObject
    {
        public Stream dramsim_log;


        public MemorySystem(uint id, uint megsOfMemory, ref Stream dramsim_log_)
        {
            dramsim_log = dramsim_log_;
            ReturnReadData = null;
            WriteDataDone = null;


            currentClockCycle = 0;

            if(Config.DEBUG_MEMORY)DEBUG.WriteLine("===== MemorySystem " + systemID + " =====");

            UInt64 megsOfStoragePerRank = ((((UInt64)Config.dram_config.NUM_ROWS * (Config.dram_config.NUM_COLS * Config.dram_config.DEVICE_WIDTH) * Config.dram_config.NUM_BANKS) * ((UInt64)Config.dram_config.JEDEC_DATA_BUS_BITS / Config.dram_config.DEVICE_WIDTH)) / 8) >> 20;

            // If this is set, effectively override the number of ranks
            if (megsOfMemory != 0)
            {
                Config.dram_config.NUM_RANKS = (uint)(megsOfMemory / megsOfStoragePerRank);
                Config.dram_config.NUM_RANKS_LOG = Config.dram_config.log2(Config.dram_config.NUM_RANKS);
                if (Config.dram_config.NUM_RANKS == 0)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("WARNING: Cannot create memory system with " + megsOfMemory + "MB, defaulting to minimum size of " + megsOfStoragePerRank + "MB");
                    Config.dram_config.NUM_RANKS = 1;
                }
            }

            Config.dram_config.NUM_DEVICES = Config.dram_config.JEDEC_DATA_BUS_BITS / Config.dram_config.DEVICE_WIDTH;
            Config.dram_config.TOTAL_STORAGE = (Config.dram_config.NUM_RANKS * megsOfStoragePerRank);

            if(Config.DEBUG_MEMORY)DEBUG.WriteLine("CH. " + systemID + " TOTAL_STORAGE : " + Config.dram_config.TOTAL_STORAGE + "MB | " + Config.dram_config.NUM_RANKS + " Ranks | " + Config.dram_config.NUM_DEVICES + " Devices per rank");


            memoryController = new MemoryController(this, dramsim_log);

            // TODO: change to other vector constructor?
            ranks = new List<Rank>();

            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                Rank r = new Rank(dramsim_log);
                r.setId(i);
                r.attachMemoryController(memoryController);
                ranks.Add(r);
            }

            memoryController.attachRanks(ranks);
        }

        public void update()
        {

            //PRINT(" ----------------- Memory System Update ------------------");

            //updates the state of each of the objects
            // NOTE - do not change order
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                ranks[i].update();
            }

            //pendingTransactions will only have stuff in it if MARSS is adding stuff
            if (pendingTransactions.Count() > 0 && memoryController.WillAcceptTransaction())
            {
                Transaction tp = pendingTransactions.PeekFront();
                memoryController.addTransaction(ref tp);
                pendingTransactions.PopFront();
            }
            memoryController.update();

            //simply increments the currentClockCycle field for each object
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                ranks[i].step();
            }
            memoryController.step();
            this.step();

            //PRINT("\n"); // two new lines
        }
        public bool addTransaction(Transaction trans)
        {
            return memoryController.addTransaction(ref trans);
        }
        public bool addTransaction(bool isWrite, UInt64 addr, CallBackInfo callback)
        {
            TransactionType type = isWrite ? TransactionType.DATA_WRITE : TransactionType.DATA_READ;

            Transaction trans = new Transaction(type, addr, 0, callback);
            // push_back in memoryController will make a copy of this during
            // addTransaction so it's kosher for the reference to be local 

            if (memoryController.WillAcceptTransaction())
            {
                return memoryController.addTransaction(ref trans);
            }
            else
            {
                pendingTransactions.PushBack(trans);
                return true;
            }
        }
        public void printStats(bool finalStats)
        {
            memoryController.printStats(finalStats);
        }
        public bool WillAcceptTransaction()
        {
            return memoryController.WillAcceptTransaction();
        }
        public void RegisterCallbacks(Callback_t readCB, Callback_t writeCB, powerCallBack_t reportPower)
        {
            ReturnReadData = readCB;
            WriteDataDone = writeCB;
            ReportPower = reportPower;
        }

        //fields
        public MemoryController memoryController;
        List<Rank> ranks;
        Deque<Transaction> pendingTransactions = new Deque<Transaction>();


        //function pointers
        public Callback_t ReturnReadData;
        public Callback_t WriteDataDone;
        //TODO: make this a functor as well?
        public powerCallBack_t ReportPower;
        public uint systemID;


    }


}
