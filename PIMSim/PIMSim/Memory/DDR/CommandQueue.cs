using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.Memory.DDR
{
   public  class CommandQueue :  DRAMSimObject
{


   public  Stream dramsim_log;

      //  public List<List<List<BusPacket>>> BusPacket3D;

    //functions
    public CommandQueue(List<List<BankState>> states, Stream dramsim_log_)
        {

            dramsim_log = dramsim_log_;
            bankStates = states;
            nextBank = 0;
            nextRank = 0;
            nextBankPRE = 0;
            nextRankPRE = 0;
            refreshWaiting = false;
            sendAct = true;

            //set here to avoid compile errors
            currentClockCycle = 0;

            //use numBankQueus below to create queue structure
            uint numBankQueues = 0;
            if (Config.dram_config. queuingStructure == QueuingStructure. PerRank)
            {
                numBankQueues = 1;
            }
            else if (Config.dram_config.queuingStructure == QueuingStructure.PerRankPerBank)
            {
                numBankQueues = Config.dram_config.NUM_BANKS;
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Error - Unknown queuing structure");
                Environment.Exit(1);
            }

            //vector of counters used to ensure rows don't stay open too long
            //rowAccessCounters = new List<List<uint>>(initreader.NUM_RANKS, new List<uint>(initreader.NUM_BANKS, 0));
            rowAccessCounters = new List<List<uint>>((int) Config.dram_config.NUM_RANKS);
            for (int i = 0; i <  Config.dram_config.NUM_RANKS; i++)
            {
                rowAccessCounters.Add( new List<uint>((int) Config.dram_config.NUM_BANKS));
                for (int j = 0; j <  Config.dram_config.NUM_BANKS; j++)
                {
                    rowAccessCounters[i].Add( 0); 
                }
            }

            //create queue based on the structure we want
            List<BusPacket> actualQueue;
            List<List<BusPacket>> perBankQueue = new List<List<BusPacket>>();
            queues = new List<List<List<BusPacket>>>();
            for (int rank = 0; rank <  Config.dram_config.NUM_RANKS; rank++)
            {
                //this loop will run only once for per-rank and NUM_BANKS times for per-rank-per-bank
                for (int bank = 0; bank < numBankQueues; bank++)
                {
                    actualQueue = new List<BusPacket>();
                    perBankQueue.Add(actualQueue);
                }
                queues.Add(perBankQueue);
            }


            //FOUR-bank activation window
            //	this will count the number of activations within a given window
            //	(decrementing counter)
            //
            //countdown vector will have decrementing counters starting at tFAW
            //  when the 0th element reaches 0, remove it
            tFAWCountdown = new List<List<uint>>((int) Config.dram_config.NUM_RANKS);
            for (int i = 0; i <  Config.dram_config.NUM_RANKS; i++)
            {
                //init the empty vectors here so we don't seg fault later
                //tFAWCountdown.push_back(vector<unsigned>());
                tFAWCountdown.Add(new List<uint>());
            }
        }

  public  void enqueue(BusPacket newBusPacket)
        {
            uint rank = newBusPacket.rank;
            uint bank = newBusPacket.bank;
            if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
            {
                queues[(int)rank][0].Add(newBusPacket);
                if (queues[(int)rank][0].Count() >  Config.dram_config.CMD_QUEUE_DEPTH)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("== Error - Enqueued more than allowed in command queue");
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("						Need to call .hasRoomFor(int numberToEnqueue, unsigned rank, unsigned bank) first");
                    Environment.Exit(1);
                }
            }
            else if ( Config.dram_config.queuingStructure == QueuingStructure.PerRankPerBank)
            {
                queues[(int)rank][(int)bank].Add(newBusPacket);
                if (queues[(int)rank][(int)bank].Count() >  Config.dram_config.CMD_QUEUE_DEPTH)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("== Error - Enqueued more than allowed in command queue");
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("						Need to call .hasRoomFor(int numberToEnqueue, unsigned rank, unsigned bank) first");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Error - Unknown queuing structure");
                Environment.Exit(1);
            }
        }
   public bool pop(ref BusPacket busPacket)
        {
            //this can be done here because pop() is called every clock cycle by the parent MemoryController
            //	figures out the sliding window requirement for tFAW
            //
            //deal with tFAW book-keeping
            //	each rank has it's own counter since the restriction is on a device level
            for (int i = 0; i <  Config.dram_config.NUM_RANKS; i++)
            {
                //decrement all the counters we have going
                for (int j = 0; j < tFAWCountdown[i].Count(); j++)
                {
                    tFAWCountdown[i][j]--;
                }

                //the head will always be the smallest counter, so check if it has reached 0
                if (tFAWCountdown[i].Count() > 0 && tFAWCountdown[i][0] == 0)
                {
                    // tFAWCountdown[i].erase(tFAWCountdown[i].begin());
                    tFAWCountdown[i].RemoveAt(0);
                }
            }

            /* Now we need to find a packet to issue. When the code picks a packet, it will set
                 *busPacket = [some eligible packet]

                 First the code looks if any refreshes need to go
                 Then it looks for data packets
                 Otherwise, it starts looking for rows to close (in open page)
            */

            if ( Config.dram_config.rowBufferPolicy == RowBufferPolicy. ClosePage)
            {
                bool sendingREF = false;
                //if the memory controller set the flags signaling that we need to issue a refresh
                if (refreshWaiting)
                {
                    bool foundActiveOrTooEarly = false;
                    //look for an open bank
                    for (uint b = 0; b <  Config.dram_config.NUM_BANKS; b++)
                    {
                        List<BusPacket> queue = getCommandQueue(refreshRank, (int)b);
                        //checks to make sure that all banks are idle
                        if (bankStates[(int)refreshRank][(int)b].currentBankState == CurrentBankState. RowActive)
                        {
                            foundActiveOrTooEarly = true;
                            //if the bank is open, make sure there is nothing else
                            // going there before we close it
                            for (int j = 0; j < queue.Count(); j++)
                            {
                                BusPacket packet = queue[j];
                                if (packet.row == bankStates[(int)refreshRank][(int)b].openRowAddress &&
                                        packet.bank == b)
                                {
                                    if (packet.busPacketType != BusPacketType. ACTIVATE && isIssuable(packet))
                                    {
                                        busPacket = packet;
                                        //queue.erase(queue.begin() + j);
                                        queue.RemoveAt(j);
                                        sendingREF = true;
                                    }
                                    break;
                                }
                            }

                            break;
                        }
                        //	NOTE: checks nextActivate time for each bank to make sure tRP is being
                        //				satisfied.	the next ACT and next REF can be issued at the same
                        //				point in the future, so just use nextActivate field instead of
                        //				creating a nextRefresh field
                        else if (bankStates[(int)refreshRank][(int)b].nextActivate > currentClockCycle)
                        {
                            foundActiveOrTooEarly = true;
                            break;
                        }
                    }

                    //if there are no open banks and timing has been met, send out the refresh
                    //	reset flags and rank pointer
                    if (!foundActiveOrTooEarly && bankStates[(int)refreshRank][0].currentBankState != CurrentBankState. PowerDown)
                    {
                        busPacket = new BusPacket(BusPacketType.REFRESH, 0, 0, 0, refreshRank, 0, 0,null, dramsim_log);
                        refreshRank = -1;
                        refreshWaiting = false;
                        sendingREF = true;
                    }
                } // refreshWaiting

                //if we're not sending a REF, proceed as normal
                if (!sendingREF)
                {
                    bool foundIssuable = false;
                    uint startingRank = nextRank;
                    uint startingBank = nextBank;
                    do
                    {
                        List<BusPacket> queue = getCommandQueue((int)nextRank, (int)nextBank);
                        //make sure there is something in this queue first
                        //	also make sure a rank isn't waiting for a refresh
                        //	if a rank is waiting for a refesh, don't issue anything to it until the
                        //		refresh logic above has sent one out (ie, letting banks close)
                        if (!(queue.Count()<=0) && !((nextRank == refreshRank) && refreshWaiting))
                        {
                            if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
                            {

                                //search from beginning to find first issuable bus packet
                                for (int i = 0; i < queue.Count(); i++)
                                {
                                    if (isIssuable(queue[i]))
                                    {
                                        //check to make sure we aren't removing a read/write that is paired with an activate
                                        if (i > 0 && queue[i - 1].busPacketType == BusPacketType. ACTIVATE &&
                                                queue[i - 1].physicalAddress == queue[i].physicalAddress)
                                            continue;

                                        busPacket = queue[i];
                                        //queue.erase(queue.begin() + i);
                                        queue.RemoveAt(i);
                                        foundIssuable = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (isIssuable(queue[0]))
                                {

                                    //no need to search because if the front can't be sent,
                                    // then no chance something behind it can go instead
                                    busPacket = queue[0];
                                    // queue.erase(queue.begin());
                                    queue.RemoveAt(0);
                                    foundIssuable = true;
                                }
                            }

                        }

                        //if we found something, break out of do-while
                        if (foundIssuable) break;

                        //rank round robin
                        if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
                        {
                            nextRank = (nextRank + 1) %  Config.dram_config.NUM_RANKS;
                            if (startingRank == nextRank)
                            {
                                break;
                            }
                        }
                        else
                        {
                            nextRankAndBank(ref nextRank, ref nextBank);
                            if (startingRank == nextRank && startingBank == nextBank)
                            {
                                break;
                            }
                        }
                    }
                    while (true);

                    //if we couldn't find anything to send, return false
                    if (!foundIssuable) return false;
                }
            }
            else if ( Config.dram_config.rowBufferPolicy == RowBufferPolicy. OpenPage)
            {
                bool sendingREForPRE = false;
                if (refreshWaiting)
                {
                    bool sendREF = true;
                    //make sure all banks idle and timing met for a REF
                    for (int b = 0; b <  Config.dram_config.NUM_BANKS; b++)
                    {
                        //if a bank is active we can't send a REF yet
                        if (bankStates[refreshRank][b].currentBankState == CurrentBankState. RowActive)
                        {
                            sendREF = false;
                            bool closeRow = true;
                            //search for commands going to an open row
                            List<BusPacket> refreshQueue = getCommandQueue(refreshRank, b);

                            for (int j = 0; j < refreshQueue.Count(); j++)
                            {
                                BusPacket packet = refreshQueue[j];
                                //if a command in the queue is going to the same row . . .
                                if (bankStates[refreshRank][b].openRowAddress == packet.row &&
                                        b == packet.bank)
                                {
                                    // . . . and is not an activate . . .
                                    if (packet.busPacketType != BusPacketType. ACTIVATE)
                                    {
                                        closeRow = false;
                                        // . . . and can be issued . . .
                                        if (isIssuable(packet))
                                        {
                                            //send it out
                                            busPacket = packet;
                                            //refreshQueue.erase(refreshQueue.begin() + j);
                                            refreshQueue.RemoveAt( j);
                                            sendingREForPRE = true;
                                        }
                                        break;
                                    }
                                    else //command is an activate
                                    {
                                        //if we've encountered another act, no other command will be of interest
                                        break;
                                    }
                                }
                            }

                            //if the bank is open and we are allowed to close it, then send a PRE
                            if (closeRow && currentClockCycle >= bankStates[refreshRank][b].nextPrecharge)
                            {
                                rowAccessCounters[refreshRank][b] = 0;
                                busPacket = new BusPacket(BusPacketType. PRECHARGE, 0, 0, 0, refreshRank, (uint)b,  0,null, dramsim_log);
                                sendingREForPRE = true;
                            }
                            break;
                        }
                        //	NOTE: the next ACT and next REF can be issued at the same
                        //				point in the future, so just use nextActivate field instead of
                        //				creating a nextRefresh field
                        else if (bankStates[refreshRank][b].nextActivate > currentClockCycle) //and this bank doesn't have an open row
                        {
                            sendREF = false;
                            break;
                        }
                    }

                    //if there are no open banks and timing has been met, send out the refresh
                    //	reset flags and rank pointer
                    if (sendREF && bankStates[refreshRank][0].currentBankState != CurrentBankState. PowerDown)
                    {
                        busPacket = new BusPacket(BusPacketType. REFRESH, 0, 0, 0, refreshRank, 0, 0,null, dramsim_log);
                        refreshRank = -1;
                        refreshWaiting = false;
                        sendingREForPRE = true;
                    }
                }

                if (!sendingREForPRE)
                {
                    uint startingRank = nextRank;
                    uint startingBank = nextBank;
                    bool foundIssuable = false;
                    do // round robin over queues
                    {
                        List<BusPacket> queue = getCommandQueue((int)nextRank, (int)nextBank);
                        //make sure there is something there first
                        if (!(queue.Count()<=0) && !((nextRank == refreshRank) && refreshWaiting))
                        {
                            //search from the beginning to find first issuable bus packet
                            for (int i = 0; i < queue.Count(); i++)
                            {
                                BusPacket packet = queue[i];
                                if (isIssuable(packet))
                                {
                                    //check for dependencies
                                    bool dependencyFound = false;
                                    for (int j = 0; j < i; j++)
                                    {
                                        BusPacket prevPacket = queue[j];
                                        if (prevPacket.busPacketType != BusPacketType. ACTIVATE &&
                                                prevPacket.bank == packet.bank &&
                                                prevPacket.row == packet.row)
                                        {
                                            dependencyFound = true;
                                            break;
                                        }
                                    }
                                    if (dependencyFound) continue;

                                    busPacket = packet;

                                    //if the bus packet before is an activate, that is the act that was
                                    //	paired with the column access we are removing, so we have to remove
                                    //	that activate as well (check i>0 because if i==0 then theres nothing before it)
                                    if (i > 0 && queue[i - 1].busPacketType == BusPacketType. ACTIVATE)
                                    {
                                        rowAccessCounters[(int)busPacket.rank][(int)busPacket.bank]++;
                                        // i is being returned, but i-1 is being thrown away, so must delete it here 
                                        // delete(queue[i - 1]);
                                        // queue.RemoveAt(i - 1);
                                        // remove both i-1 (the activate) and i (we've saved the pointer in *busPacket)
                                        //queue.erase(queue.begin() + i - 1, queue.begin() + i + 1);
                                        //queue.RemoveRange(i - 1, i + 1);
                                        queue.RemoveAt(i);
                                        queue.RemoveAt(i - 1);
                                    }
                                    else // there's no activate before this packet
                                    {
                                        //or just remove the one bus packet
                                        // queue.erase(queue.begin() + i);
                                        queue.RemoveAt(i);
                                    }

                                    foundIssuable = true;
                                    break;
                                }
                            }
                        }

                        //if we found something, break out of do-while
                        if (foundIssuable) break;

                        //rank round robin
                        if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
                        {
                            nextRank = (nextRank + 1) %  Config.dram_config.NUM_RANKS;
                            if (startingRank == nextRank)
                            {
                                break;
                            }
                        }
                        else
                        {
                            nextRankAndBank(ref nextRank, ref nextBank);
                            if (startingRank == nextRank && startingBank == nextBank)
                            {
                                break;
                            }
                        }
                    }
                    while (true);

                    //if nothing was issuable, see if we can issue a PRE to an open bank
                    //	that has no other commands waiting
                    if (!foundIssuable)
                    {
                        //search for banks to close
                        bool sendingPRE = false;
                        uint startingRank1 = nextRankPRE;
                        uint startingBank1 = nextBankPRE;

                        do // round robin over all ranks and banks
                        {
                            List<BusPacket> queue = getCommandQueue((int)nextRankPRE, (int)nextBankPRE);
                            bool found = false;
                            //check if bank is open
                            if (bankStates[(int)nextRankPRE][(int)nextBankPRE].currentBankState == CurrentBankState. RowActive)
                            {
                                for (int i = 0; i < queue.Count(); i++)
                                {
                                    //if there is something going to that bank and row, then we don't want to send a PRE
                                    if (queue[i].bank == nextBankPRE &&
                                            queue[i].row == bankStates[(int)nextRankPRE][(int)nextBankPRE].openRowAddress)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                //if nothing found going to that bank and row or too many accesses have happend, close it
                                if (!found || rowAccessCounters[(int)nextRankPRE][(int)nextBankPRE] ==  Config.dram_config.TOTAL_ROW_ACCESSES)
                                {
                                    if (currentClockCycle >= bankStates[(int)nextRankPRE][(int)nextBankPRE].nextPrecharge)
                                    {
                                        sendingPRE = true;
                                        rowAccessCounters[(int)nextRankPRE][(int)nextBankPRE] = 0;
                                        busPacket = new BusPacket(BusPacketType. PRECHARGE, 0, 0, 0, (int)nextRankPRE, nextBankPRE,  0,null, dramsim_log);
                                        break;
                                    }
                                }
                            }
                            nextRankAndBank(ref nextRankPRE, ref nextBankPRE);
                        }
                        while (!(startingRank1 == nextRankPRE && startingBank1 == nextBankPRE));

                        //if no PREs could be sent, just return false
                        if (!sendingPRE) return false;
                    }
                }
            }

            //sendAct is flag used for posted-cas
            //  posted-cas is enabled when AL>0
            //  when sendAct is true, when don't want to increment our indexes
            //  so we send the column access that is paid with this act
            if ( Config.dram_config.AL > 0 && sendAct)
            {
                sendAct = false;
            }
            else
            {
                sendAct = true;
                nextRankAndBank(ref nextRank, ref nextBank);
            }

            //if its an activate, add a tfaw counter
            if (busPacket.busPacketType ==BusPacketType. ACTIVATE)
            {
                tFAWCountdown[(int)busPacket.rank].Add( Config.dram_config.tFAW);
            }

            return true;
        }
   public bool hasRoomFor(uint numberToEnqueue, uint rank, uint bank)
        {
            List < BusPacket > queue = getCommandQueue((int)rank, (int)bank);
            return ( Config.dram_config.CMD_QUEUE_DEPTH - queue.Count() >= numberToEnqueue);
        }
   public bool isIssuable(BusPacket busPacket)
        {
            switch (busPacket.busPacketType)
            {
                case BusPacketType.  REFRESH:

                    break;
                case BusPacketType.ACTIVATE:
                    if ((bankStates[(int)busPacket.rank][(int)busPacket.bank].currentBankState ==CurrentBankState. Idle ||
                            bankStates[(int)busPacket.rank][(int)busPacket.bank].currentBankState ==CurrentBankState. Refreshing) &&
                            currentClockCycle >= bankStates[(int)busPacket.rank][(int)busPacket.bank].nextActivate &&
                            tFAWCountdown[(int)busPacket.rank].Count() < 4)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case BusPacketType.WRITE:
                case BusPacketType.WRITE_P:
                    if (bankStates[(int)busPacket.rank][(int)busPacket.bank].currentBankState == CurrentBankState.RowActive &&
                            currentClockCycle >= bankStates[(int)busPacket.rank][(int)busPacket.bank].nextWrite &&
                            busPacket.row == bankStates[(int)busPacket.rank][(int)busPacket.bank].openRowAddress &&
                            rowAccessCounters[(int)busPacket.rank][(int)busPacket.bank] <  Config.dram_config.TOTAL_ROW_ACCESSES)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case BusPacketType.READ_P:
                case BusPacketType.READ:
                    if (bankStates[(int)busPacket.rank][(int)busPacket.bank].currentBankState == CurrentBankState. RowActive &&
                            currentClockCycle >= bankStates[(int)busPacket.rank][(int)busPacket.bank].nextRead &&
                            busPacket.row == bankStates[(int)busPacket.rank][(int)busPacket.bank].openRowAddress &&
                            rowAccessCounters[(int)busPacket.rank][(int)busPacket.bank] <  Config.dram_config.TOTAL_ROW_ACCESSES)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case BusPacketType.PRECHARGE:
                    if (bankStates[(int)busPacket.rank][(int)busPacket.bank].currentBankState ==CurrentBankState. RowActive &&
                            currentClockCycle >= bankStates[(int)busPacket.rank][(int)busPacket.bank].nextPrecharge)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("ERROT == Error - Trying to issue a crazy bus packet type : ");
                    busPacket.print();
                    Environment.Exit(0);
                    break;
            }
            return false;
        }
   public bool isEmpty(uint rank)
        {
            if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
            {
                return queues[(int)rank][0].Count()<=0;
            }
            else if ( Config.dram_config.queuingStructure == QueuingStructure. PerRankPerBank)
            {
                for (int i = 0; i <  Config.dram_config.NUM_BANKS; i++)
                {
                    if (!(queues[(int)rank][(int)i].Count()<=0)) return false;
                }
                return true;
            }
            else
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("DEBUG: Invalid Queueing Stucture");
                Environment.Exit(1);
                return false;
            }
        }
        public void needRefresh(uint rank)
        {
            refreshWaiting = true;
            refreshRank = (int)rank;
        }
       public  void print() { }
       public void update()
        {
            //do nothing since pop() is effectively update(),
            //needed for SimulatorObject
            //TODO: make CommandQueue not a SimulatorObject

        }//SimulatorObject requirement
        public List<BusPacket> getCommandQueue(int rank, int bank)
        {
            if ( Config.dram_config.queuingStructure == QueuingStructure. PerRankPerBank)
            {
                return queues[rank][bank];
            }
            else if ( Config.dram_config.queuingStructure == QueuingStructure. PerRank)
            {
                return queues[rank][0];
            }
            else
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR : Unknown queue structure");
                Environment.Exit(1);
                return null;
            }
        }

        //fields

        public List<List<List<BusPacket>>> queues; // 3D array of BusPacket pointers
        public List<List<BankState>> bankStates;

	public void nextRankAndBank(ref uint rank,ref uint bank)
        {

            if ( Config.dram_config.schedulingPolicy == SchedulingPolicy. RankThenBankRoundRobin)
            {
                rank++;
                if (rank ==  Config.dram_config.NUM_RANKS)
                {
                    rank = 0;
                    bank++;
                    if (bank ==  Config.dram_config.NUM_BANKS)
                    {
                        bank = 0;
                    }
                }
            }
            //bank-then-rank round robin
            else if ( Config.dram_config.schedulingPolicy == SchedulingPolicy.BankThenRankRoundRobin)
            {
                bank++;
                if (bank ==  Config.dram_config.NUM_BANKS)
                {
                    bank = 0;
                    rank++;
                    if (rank ==  Config.dram_config.NUM_RANKS)
                    {
                        rank = 0;
                    }
                }
            }
            else
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Unknown scheduling policy");
                Environment.Exit(1);
            }
        }
        //fields
       public uint nextBank;
      public  uint nextRank;

      public  uint nextBankPRE;
      public  uint nextRankPRE;

       public int refreshRank;
   public bool refreshWaiting;

    public    List<List<uint>> tFAWCountdown;
      public  List<List<uint>> rowAccessCounters;

   public bool sendAct;
}
}
