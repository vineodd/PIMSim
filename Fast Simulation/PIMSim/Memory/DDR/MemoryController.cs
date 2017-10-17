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
    public class MemoryController :  DRAMSimObject
{

       public List<Proc> proc;
       public  void addressMapping(UInt64 physicalAddress, ref int newTransactionChan, ref int newTransactionRank,ref  int newTransactionBank, ref int newTransactionRow, ref int newTransactionColumn)
        {
            UInt64 tempA, tempB;
            int transactionSize =(int)Config.dram_config.TRANSACTION_SIZE;
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
                    DEBUG.WriteLine("WARNING: address 0x"  + physicalAddress.ToString("X") + " is not aligned to the request size of " + transactionSize);
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
            if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme1)
            {
                //chan:rank:row:col:bank
                tempA = physicalAddress;
                physicalAddress = physicalAddress >> bankBitWidth;
                tempB = physicalAddress << bankBitWidth;
                newTransactionBank =(int)( tempA ^ tempB);

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
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme2)
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
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme3)
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
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme4)
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
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme5)
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
            else if (Config.dram_config.addressMappingScheme == AddressMappingScheme. Scheme6)
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
        public int SEQUENTIAL(int rank, int bank)
        {
            return (rank * (int)Config.dram_config. NUM_BANKS) + bank;
        }
        public MemoryController(MemorySystem parent, Stream dramsim_log_)
        {

            dramsim_log = dramsim_log_;
            bankStates = new List<List<BankState>>((int)Config.dram_config.NUM_RANKS);
            for(int i = 0; i < (int)Config.dram_config.NUM_RANKS; i++)
            {
                List<BankState> tp = new List<BankState>((int)Config.dram_config.NUM_BANKS);
                for(int j = 0; j < (int)Config.dram_config.NUM_BANKS; j++)
                {
                    tp.Add(new BankState(dramsim_log));
                }
                bankStates.Add(tp);
            }
            commandQueue = new CommandQueue(bankStates, dramsim_log);
            poppedBusPacket = null;
    
            totalTransactions = 0;
            refreshRank = 0;

            parentMemorySystem = parent;


            //bus related fields
            outgoingCmdPacket = null;
            outgoingDataPacket = null;
            dataCyclesLeft = 0;
            cmdCyclesLeft = 0;

            //set here to avoid compile errors
            currentClockCycle = 0;

            //reserve memory for vectors
            transactionQueue = new List<Transaction>((int)Config.dram_config.TRANS_QUEUE_DEPTH);
            powerDown = new List<bool>((int)Config.dram_config.NUM_RANKS);
            for(int i=0;i< powerDown.Capacity; i++)
            {
                powerDown.Add(false);
            }
            grandTotalBankAccesses = new List<ulong>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            for (int i = 0; i < grandTotalBankAccesses.Capacity; i++)
            {
                grandTotalBankAccesses.Add(0);
            }
            totalReadsPerBank = new List<ulong>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            for (int i = 0; i < totalReadsPerBank.Capacity; i++)
            {
                totalReadsPerBank.Add(0);
            }
            totalWritesPerBank = new List<ulong>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            for (int i = 0; i < totalWritesPerBank.Capacity; i++)
            {
                totalWritesPerBank.Add(0);
            }

            totalReadsPerRank = new List<ulong>((int)Config.dram_config.NUM_RANKS);
            for (int i = 0; i < totalReadsPerRank.Capacity; i++)
            {
                totalReadsPerRank.Add(0);
            }
            totalWritesPerRank = new List<ulong>((int)Config.dram_config.NUM_RANKS);

            for (int i = 0; i < totalWritesPerRank.Capacity; i++)
            {
                totalWritesPerRank.Add(0);
            }

            writeDataCountdown = new List<uint>((int)Config.dram_config.NUM_RANKS);
            writeDataToSend = new List<BusPacket>((int)Config.dram_config.NUM_RANKS);
            refreshCountdown = new List<uint>((int)Config.dram_config.NUM_RANKS);

            //Power related packets
            backgroundEnergy = new List<ulong>((int)Config.dram_config.NUM_RANKS);
            for (int i = 0; i < backgroundEnergy.Capacity; i++)
            {
                backgroundEnergy.Add(0);
            }



            burstEnergy = new List<ulong>((int)Config.dram_config.NUM_RANKS);
            for (int i = 0; i < burstEnergy.Capacity; i++)
            {
                burstEnergy.Add(0);
            }
            actpreEnergy = new List<ulong>((int)Config.dram_config.NUM_RANKS);
            for (int i = 0; i < actpreEnergy.Capacity; i++)
            {
                actpreEnergy.Add(0);
            }
            refreshEnergy = new List<ulong>((int)Config.dram_config.NUM_RANKS);
            for (int i = 0; i < refreshEnergy.Capacity; i++)
            {
                refreshEnergy.Add(0);
            }
            totalEpochLatency = new List<ulong>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            for (int i = 0; i < totalEpochLatency.Capacity; i++)
            {
                totalEpochLatency.Add(0);
            }
            //staggers when each rank is due for a refresh
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                refreshCountdown.Add((uint)((int)((Config.dram_config.REFRESH_PERIOD / Config.dram_config.tCK) / Config.dram_config.NUM_RANKS) * (i + 1)));
            }


        }

        public bool addTransaction(ref Transaction trans)
        {
            if (WillAcceptTransaction())
            {
                trans.timeAdded = currentClockCycle;
                transactionQueue.Add(trans);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WillAcceptTransaction()
        {
            return transactionQueue.Count() < Config.dram_config.TRANS_QUEUE_DEPTH;
        }
        public void returnReadData(Transaction trans)
        {
            if (parentMemorySystem.ReturnReadData != null)
            {
                parentMemorySystem.ReturnReadData(parentMemorySystem.systemID,trans.address,currentClockCycle, trans.callback);
            }
        }
        public void receiveFromBus(ref BusPacket bpacket)
        {
            if (bpacket.busPacketType !=BusPacketType. DATA)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Error - Memory Controller received a non-DATA bus packet from rank");
                bpacket.print();
                Environment.Exit(1);
            }

            if (Config.dram_config.DEBUG_BUS)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine(" -- MC Receiving From Data Bus : ");
                bpacket.print();
            }

            //add to return read data queue
            returnTransaction.Add(new Transaction(TransactionType. RETURN_DATA,bpacket.physicalAddress,bpacket.data, bpacket.callback));
            totalReadsPerBank[SEQUENTIAL((int)bpacket.rank, (int)bpacket.bank)]++;

            // this delete statement saves a mindboggling amount of memory
            //delete(bpacket);
        }
        public void attachRanks(List<Rank> ranks)
        {
            this.ranks = ranks;
        }
        public override void update()
        {
            //PRINT(" ------------------------- [" << currentClockCycle << "] -------------------------");

            //update bank states
            for (int i = 0; i < Config. dram_config.NUM_RANKS; i++)
            {
                for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                {
                    if (bankStates[i][j].stateChangeCountdown > 0)
                    {
                        //decrement counters
                        bankStates[i][j].stateChangeCountdown--;

                        //if counter has reached 0, change state
                        if (bankStates[i][j].stateChangeCountdown == 0)
                        {
                            switch (bankStates[i][j].lastCommand)
                            {
                                //only these commands have an implicit state change
                                case BusPacketType.WRITE_P:
                                case BusPacketType.READ_P:
                                    bankStates[i][j].currentBankState = CurrentBankState. Precharging;
                                    bankStates[i][j].lastCommand = BusPacketType.PRECHARGE;
                                    bankStates[i][j].stateChangeCountdown = (int)Config.dram_config.tRP;
                                    break;

                                case BusPacketType.REFRESH:
                                case BusPacketType.PRECHARGE:
                                    bankStates[i][j].currentBankState = CurrentBankState.Idle;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }


            //check for outgoing command packets and handle countdowns
            if (outgoingCmdPacket != null)
            {
                cmdCyclesLeft--;
                if (cmdCyclesLeft == 0) //packet is ready to be received by rank
                {
                    ranks[(int)outgoingCmdPacket.rank].receiveFromBus(ref outgoingCmdPacket);
                    outgoingCmdPacket = null;
                }
            }

            //check for outgoing data packets and handle countdowns
            if (outgoingDataPacket != null)
            {
                dataCyclesLeft--;
                if (dataCyclesLeft == 0)
                {
                    //inform upper levels that a write is done
                    if (parentMemorySystem.WriteDataDone != null)
                    {
                        parentMemorySystem.WriteDataDone(parentMemorySystem.systemID, outgoingDataPacket.physicalAddress, currentClockCycle,outgoingDataPacket.callback);//new CallBackInfo( , outgoingDataPacket.block_addr,currentClockCycle,outgoingDataPacket.pim,outgoingDataPacket.pid));
                    }

                    ranks[(int)outgoingDataPacket.rank].receiveFromBus(ref outgoingDataPacket);
                    outgoingDataPacket = null;
                }
            }


            //if any outstanding write data needs to be sent
            //and the appropriate amount of time has passed (WL)
            //then send data on bus
            //
            //write data held in fifo vector along with countdowns
            if (writeDataCountdown.Count() > 0)
            {
                for (int i = 0; i < writeDataCountdown.Count(); i++)
                {
                    writeDataCountdown[i]--;
                }

                if (writeDataCountdown[0] == 0)
                {
                    //send to bus and print debug stuff
                    if (Config. dram_config.DEBUG_BUS)
                    {
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine(" -- MC Issuing On Data Bus    : ");
                        writeDataToSend[0].print();
                    }

                    // queue up the packet to be sent
                    if (outgoingDataPacket != null)
                    {
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("== Error - Data Bus Collision");
                        Environment.Exit(1);
                    }

                    outgoingDataPacket = writeDataToSend[0];
                    dataCyclesLeft = Config. dram_config.BL / 2;

                    totalTransactions++;
                    totalWritesPerBank[SEQUENTIAL((int)writeDataToSend[0].rank, (int)writeDataToSend[0].bank)]++;

                    //writeDataCountdown.erase(writeDataCountdown.begin());
                    writeDataCountdown.RemoveAt(0);
                    // writeDataToSend.erase(writeDataToSend.begin());
                    writeDataToSend.RemoveAt(0);
                }
            }

            //if its time for a refresh issue a refresh
            // else pop from command queue if it's not empty
            if (refreshCountdown[(int)refreshRank] == 0)
            {
                commandQueue.needRefresh(refreshRank);
                ranks[(int)refreshRank].refreshWaiting = true;
                refreshCountdown[(int)refreshRank] = (uint)((int)Config.dram_config.REFRESH_PERIOD / Config.dram_config.tCK);
                refreshRank++;
                if (refreshRank == Config. dram_config.NUM_RANKS)
                {
                    refreshRank = 0;
                }
            }
            //if a rank is powered down, make sure we power it up in time for a refresh
            else if (powerDown[(int)refreshRank] && refreshCountdown[(int)refreshRank] <= Config.dram_config.tXP)
            {
                ranks[(int)refreshRank].refreshWaiting = true;
            }

            //pass a pointer to a poppedBusPacket

            //function returns true if there is something valid in poppedBusPacket
            if (commandQueue.pop(ref poppedBusPacket))
            {
                if (poppedBusPacket.busPacketType ==BusPacketType. WRITE || poppedBusPacket.busPacketType == BusPacketType.WRITE_P)
                {

                    writeDataToSend.Add(new BusPacket(BusPacketType. DATA, poppedBusPacket.physicalAddress, poppedBusPacket.column,
                                                        poppedBusPacket.row, (int)poppedBusPacket.rank, poppedBusPacket.bank,
                                                        poppedBusPacket.data,poppedBusPacket.callback, dramsim_log));
                    writeDataCountdown.Add(Config.dram_config.WL);
                }

                //
                //update each bank's state based on the command that was just popped out of the command queue
                //
                //for readability's sake
                int rank =(int) poppedBusPacket.rank;
                int bank =(int) poppedBusPacket.bank;
                switch (poppedBusPacket.busPacketType)
                {
                    case BusPacketType. READ_P:
                    case BusPacketType.READ:
                        //add energy to account for total
                        if (Config. dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding Read energy to total energy");
                        }
                        burstEnergy[rank] += (Config.dram_config.IDD4R - Config.dram_config.IDD3N) * Config.dram_config.BL / 2 * Config.dram_config.NUM_DEVICES;
                        if (poppedBusPacket.busPacketType ==BusPacketType. READ_P)
                        {
                            //Don't bother setting next read or write times because the bank is no longer active
                            //bankStates[rank][bank].currentBankState = Idle;
                            bankStates[rank][bank].nextActivate = Math.Max(currentClockCycle + Config.dram_config.READ_AUTOPRE_DELAY,
                                    bankStates[rank][bank].nextActivate);
                            bankStates[rank][bank].lastCommand =BusPacketType. READ_P;
                            bankStates[rank][bank].stateChangeCountdown = (int)Config.dram_config.READ_TO_PRE_DELAY;
                        }
                        else if (poppedBusPacket.busPacketType ==BusPacketType. READ)
                        {
                            bankStates[rank][bank].nextPrecharge = Math.Max(currentClockCycle + Config.dram_config.READ_TO_PRE_DELAY,
                                    bankStates[rank][bank].nextPrecharge);
                            bankStates[rank][bank].lastCommand =BusPacketType. READ;

                        }

                        for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
                        {
                            for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                            {
                                if (i != poppedBusPacket.rank)
                                {
                                    //check to make sure it is active before trying to set (save's time?)
                                    if (bankStates[i][j].currentBankState == CurrentBankState. RowActive)
                                    {
                                        bankStates[i][j].nextRead = Math.Max(currentClockCycle + Config.dram_config.BL / 2 + Config.dram_config.tRTRS, bankStates[i][j].nextRead);
                                        bankStates[i][j].nextWrite = Math.Max(currentClockCycle + Config.dram_config.READ_TO_WRITE_DELAY,
                                                bankStates[i][j].nextWrite);
                                    }
                                }
                                else
                                {
                                    bankStates[i][j].nextRead = Math.Max(currentClockCycle + Math.Max(Config.dram_config.tCCD, Config.dram_config.BL / 2), bankStates[i][j].nextRead);
                                    bankStates[i][j].nextWrite = Math.Max(currentClockCycle + Config.dram_config.READ_TO_WRITE_DELAY,
                                            bankStates[i][j].nextWrite);
                                }
                            }
                        }

                        if (poppedBusPacket.busPacketType ==BusPacketType. READ_P)
                        {
                            //set read and write to nextActivate so the state table will prevent a read or write
                            //  being issued (in cq.isIssuable())before the bank state has been changed because of the
                            //  auto-precharge associated with this command
                            bankStates[rank][bank].nextRead = bankStates[rank][bank].nextActivate;
                            bankStates[rank][bank].nextWrite = bankStates[rank][bank].nextActivate;
                        }

                        break;
                    case BusPacketType.WRITE_P:
                    case BusPacketType.WRITE:
                        if (poppedBusPacket.busPacketType ==BusPacketType. WRITE_P)
                        {
                            bankStates[rank][bank].nextActivate = Math.Max(currentClockCycle + Config.dram_config.WRITE_AUTOPRE_DELAY,
                                    bankStates[rank][bank].nextActivate);
                            bankStates[rank][bank].lastCommand =BusPacketType. WRITE_P;
                            bankStates[rank][bank].stateChangeCountdown = (int)Config.dram_config.WRITE_TO_PRE_DELAY;
                        }
                        else if (poppedBusPacket.busPacketType ==BusPacketType. WRITE)
                        {
                            bankStates[rank][bank].nextPrecharge = Math.Max(currentClockCycle + Config.dram_config.WRITE_TO_PRE_DELAY,
                                    bankStates[rank][bank].nextPrecharge);
                            bankStates[rank][bank].lastCommand =BusPacketType. WRITE;
                        }


                        //add energy to account for total
                        if (Config.dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding Write energy to total energy");
                        }
                        burstEnergy[rank] += (Config.dram_config.IDD4W - Config.dram_config.IDD3N) * Config.dram_config.BL / 2 * Config.dram_config.NUM_DEVICES;

                        for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
                        {
                            for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                            {
                                if (i != poppedBusPacket.rank)
                                {
                                    if (bankStates[i][j].currentBankState == CurrentBankState. RowActive)
                                    {
                                        bankStates[i][j].nextWrite = Math.Max(currentClockCycle + Config.dram_config.BL / 2 + Config.dram_config.tRTRS, bankStates[i][j].nextWrite);
                                        bankStates[i][j].nextRead = Math.Max(currentClockCycle + Config.dram_config.WRITE_TO_READ_DELAY_R,
                                                bankStates[i][j].nextRead);
                                    }
                                }
                                else
                                {
                                    bankStates[i][j].nextWrite = Math.Max(currentClockCycle + Math.Max(Config.dram_config.BL / 2, Config.dram_config.tCCD), bankStates[i][j].nextWrite);
                                    bankStates[i][j].nextRead = Math.Max(currentClockCycle + Config.dram_config.WRITE_TO_READ_DELAY_B,
                                            bankStates[i][j].nextRead);
                                }
                            }
                        }

                        //set read and write to nextActivate so the state table will prevent a read or write
                        //  being issued (in cq.isIssuable())before the bank state has been changed because of the
                        //  auto-precharge associated with this command
                        if (poppedBusPacket.busPacketType ==BusPacketType. WRITE_P)
                        {
                            bankStates[rank][bank].nextRead = bankStates[rank][bank].nextActivate;
                            bankStates[rank][bank].nextWrite = bankStates[rank][bank].nextActivate;
                        }

                        break;
                    case BusPacketType.ACTIVATE:
                        //add energy to account for total
                        if (Config.dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding Activate and Precharge energy to total energy");
                        }
                        actpreEnergy[rank] += ((Config.dram_config.IDD0 * Config.dram_config.tRC) - ((Config.dram_config.IDD3N * Config.dram_config.tRAS) + (Config.dram_config.IDD2N * (Config.dram_config.tRC - Config.dram_config.tRAS)))) * Config.dram_config.NUM_DEVICES;

                        bankStates[rank][bank].currentBankState =CurrentBankState.RowActive;
                        bankStates[rank][bank].lastCommand =BusPacketType. ACTIVATE;
                        bankStates[rank][bank].openRowAddress = (int)poppedBusPacket.row;
                        bankStates[rank][bank].nextActivate = Math.Max(currentClockCycle + Config.dram_config.tRC, bankStates[rank][bank].nextActivate);
                        bankStates[rank][bank].nextPrecharge = Math.Max(currentClockCycle + Config.dram_config.tRAS, bankStates[rank][bank].nextPrecharge);

                        //if we are using posted-CAS, the next column access can be sooner than normal operation

                        bankStates[rank][bank].nextRead = Math.Max(currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL), bankStates[rank][bank].nextRead);
                        bankStates[rank][bank].nextWrite = Math.Max(currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL), bankStates[rank][bank].nextWrite);

                        for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                        {
                            if (i != poppedBusPacket.bank)
                            {
                                bankStates[rank][i].nextActivate = Math.Max(currentClockCycle + Config.dram_config.tRRD, bankStates[rank][i].nextActivate);
                            }
                        }

                        break;
                    case BusPacketType.PRECHARGE:
                        bankStates[rank][bank].currentBankState =CurrentBankState. Precharging;
                        bankStates[rank][bank].lastCommand =BusPacketType. PRECHARGE;
                        bankStates[rank][bank].stateChangeCountdown = (int)Config.dram_config.tRP;
                        bankStates[rank][bank].nextActivate = Math.Max(currentClockCycle + Config.dram_config.tRP, bankStates[rank][bank].nextActivate);

                        break;
                    case BusPacketType.REFRESH:
                        //add energy to account for total
                        if (Config.dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding Refresh energy to total energy");
                        }
                        refreshEnergy[rank] += (Config.dram_config.IDD5 - Config.dram_config.IDD3N) * Config.dram_config.tRFC * Config.dram_config.NUM_DEVICES;

                        for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                        {
                            bankStates[rank][i].nextActivate = currentClockCycle + Config.dram_config.tRFC;
                            bankStates[rank][i].currentBankState = CurrentBankState. Refreshing;
                            bankStates[rank][i].lastCommand = BusPacketType. REFRESH;
                            bankStates[rank][i].stateChangeCountdown = (int)Config.dram_config.tRFC;
                        }

                        break;
                    default:
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("ERROR == Error - Popped a command we shouldn't have of type : " + poppedBusPacket.busPacketType);
                        Environment.Exit(1);
                        break;
                }

                //issue on bus and print debug
                if (Config.dram_config.DEBUG_BUS)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine(" -- MC Issuing On Command Bus : ");
                    poppedBusPacket.print();
                }

                //check for collision on bus
                if (outgoingCmdPacket != null)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("ERROR == Error - Command Bus Collision");
                    Environment.Exit(1);
                }
                outgoingCmdPacket = poppedBusPacket;
                cmdCyclesLeft = Config.dram_config.tCMD;

            }

            for (int i = 0; i < transactionQueue.Count(); i++)
            {
                //pop off top transaction from queue
                //
                //	assuming simple scheduling at the moment
                //	will eventually add policies here
                Transaction transaction = transactionQueue[i];

                //map address to rank,bank,row,col
                int newTransactionChan = 0, newTransactionRank = 0, newTransactionBank = 0, newTransactionRow = 0, newTransactionColumn = 0;

                // pass these in as references so they get set by the addressMapping function
                addressMapping(transaction.address, ref newTransactionChan, ref newTransactionRank, ref newTransactionBank, ref newTransactionRow, ref newTransactionColumn);

                //if we have room, break up the transaction into the appropriate commands
                //and add them to the command queue
                if (commandQueue.hasRoomFor(2, (uint)newTransactionRank,(uint) newTransactionBank))
                {
                    if (Config.dram_config.DEBUG_ADDR_MAP)
                    {
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("== New Transaction - Mapping Address [0x"  + transaction.address.ToString("X")  + "]");
                        if (transaction.transactionType == TransactionType. DATA_READ)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" (Read)");
                        }
                        else
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" (Write)");
                        }
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("  Rank : " + newTransactionRank);
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("  Bank : " + newTransactionBank);
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("  Row  : " + newTransactionRow);
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("  Col  : " + newTransactionColumn);
                    }



                    //now that we know there is room in the command queue, we can remove from the transaction queue
                    // transactionQueue.erase(transactionQueue.begin() + i);
                    transactionQueue.RemoveAt(i);

                    //create activate command to the row we just translated
                    BusPacket ACTcommand = new BusPacket(BusPacketType. ACTIVATE, transaction.address,
                           (uint) newTransactionColumn, (uint)newTransactionRow, newTransactionRank,
                           (uint)newTransactionBank, 0, null, dramsim_log);

                    //create read or write command and enqueue it
                    BusPacketType bpType = transaction.getBusPacketType();
                    BusPacket command = new BusPacket(bpType, transaction.address,
                            (uint)newTransactionColumn, (uint)newTransactionRow, newTransactionRank,
                           (uint)newTransactionBank, transaction.data, transaction.callback, dramsim_log);



                    commandQueue.enqueue(ACTcommand);
                    commandQueue.enqueue(command);

                    // If we have a read, save the transaction so when the data comes back
                    // in a bus packet, we can staple it back into a transaction and return it
                    if (transaction.transactionType ==TransactionType. DATA_READ)
                    {
                        pendingReadTransactions.Add(transaction);
                    }
                    else
                    {
                        // just delete the transaction now that it's a buspacket
                      //  delete transaction;
                    }
                    /* only allow one transaction to be scheduled per cycle -- this should
                     * be a reasonable assumption considering how much logic would be
                     * required to schedule multiple entries per cycle (parallel data
                     * lines, switching logic, decision logic)
                     */
                    break;
                }
                else // no room, do nothing this cycle
                {
                    //PRINT( "== Warning - No room in command queue" << endl;
                }
            }


            //calculate power
            //  this is done on a per-rank basis, since power characterization is done per device (not per bank)
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                if (Config.dram_config.USE_LOW_POWER)
                {
                    //if there are no commands in the queue and that particular rank is not waiting for a refresh...
                    if (commandQueue.isEmpty((uint)i) && !ranks[i].refreshWaiting)
                    {
                        //check to make sure all banks are idle
                        bool allIdle = true;
                        for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                        {
                            if (bankStates[i][j].currentBankState != CurrentBankState. Idle)
                            {
                                allIdle = false;
                                break;
                            }
                        }

                        //if they ARE all idle, put in power down mode and set appropriate fields
                        if (allIdle)
                        {
                            powerDown[i] = true;
                            ranks[i].powerDown();
                            for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                            {
                                bankStates[i][j].currentBankState = CurrentBankState. PowerDown;
                                bankStates[i][j].nextPowerUp = currentClockCycle + Config.dram_config.tCKE;
                            }
                        }
                    }
                    //if there IS something in the queue or there IS a refresh waiting (and we can power up), do it
                    else if (currentClockCycle >= bankStates[i][0].nextPowerUp && powerDown[i]) //use 0 since theyre all the same
                    {
                        powerDown[i] = false;
                        ranks[i].powerUp();
                        for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                        {
                            bankStates[i][j].currentBankState = CurrentBankState.Idle;
                            bankStates[i][j].nextActivate = currentClockCycle + Config.dram_config.tXP;
                        }
                    }
                }

                //check for open bank
                bool bankOpen = false;
                for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                {
                    if (bankStates[i][j].currentBankState == CurrentBankState. Refreshing ||
                            bankStates[i][j].currentBankState == CurrentBankState. RowActive)
                    {
                        bankOpen = true;
                        break;
                    }
                }

                //background power is dependent on whether or not a bank is open or not
                if (bankOpen)
                {
                    if (Config.dram_config.DEBUG_POWER)
                    {
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine(" ++ Adding IDD3N to total energy [from rank " + i + "]");
                    }
                    backgroundEnergy[i] += Config.dram_config.IDD3N * Config.dram_config.NUM_DEVICES;
                }
                else
                {
                    //if we're in power-down mode, use the correct current
                    if (powerDown[i])
                    {
                        if (Config.dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding dram_config.IDD2P to total energy [from rank " + i + "]");
                        }
                        backgroundEnergy[i] += Config.dram_config.IDD2P * Config.dram_config.NUM_DEVICES;
                    }
                    else
                    {
                        if (Config.dram_config.DEBUG_POWER)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine(" ++ Adding dram_config.IDD2N to total energy [from rank " + i + "]");
                        }
                        backgroundEnergy[i] += Config.dram_config.IDD2N * Config.dram_config.NUM_DEVICES;
                    }
                }
            }

            //check for outstanding data to return to the CPU
            if (returnTransaction.Count() > 0)
            {
                if (Config.dram_config.DEBUG_BUS)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine(" -- MC Issuing to CPU bus : " + returnTransaction[0]);
                }
                totalTransactions++;

                bool foundMatch = false;
                //find the pending read transaction to calculate latency
                for (int i = 0; i < pendingReadTransactions.Count(); i++)
                {
                    if (pendingReadTransactions[i].address == returnTransaction[0].address)
                    {
                        //if(currentClockCycle - pendingReadTransactions[i]->timeAdded > 2000)
                        //	{
                        //		pendingReadTransactions[i]->print();
                        //		exit(0);
                        //	}
                        int chan=0, rank=0, bank=0, row=0, col=0;
                        addressMapping(returnTransaction[0].address, ref chan, ref rank, ref bank, ref row, ref col);
                        insertHistogram((uint)(currentClockCycle - pendingReadTransactions[i].timeAdded),(uint) rank,(uint) bank);
                        //return latency
                        returnReadData(pendingReadTransactions[i]);

                        //* delete pendingReadTransactions[i];
                        pendingReadTransactions.RemoveAt(i);
                        //* pendingReadTransactions.erase(pendingReadTransactions.begin() + i);

                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("ERROR Can't find a matching transaction for 0x" + returnTransaction[0].address.ToString("X"));
                    Environment.Exit(0);
                }
                //* delete returnTransaction[0];
                returnTransaction.RemoveAt(0);
                //returnTransaction.erase(returnTransaction.begin());
            }

            //decrement refresh counters
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                refreshCountdown[i]--;
            }

            //
            //print debug
            //
            if (Config.dram_config.DEBUG_TRANS_Q)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Printing transaction queue");
                for (int i = 0; i < transactionQueue.Count(); i++)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("  " + i + "] " + transactionQueue[i]);
                }
            }

            if (Config.dram_config.DEBUG_BANKSTATE)
            {
                //TODO: move this to BankState.cpp
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("== Printing bank states (According to MC)");
                for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
                {
                    for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                    {
                        if (bankStates[i][j].currentBankState == CurrentBankState. RowActive)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("[" + bankStates[i][j].openRowAddress + "] ");
                        }
                        else if (bankStates[i][j].currentBankState == CurrentBankState.Idle)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("[idle] ");
                        }
                        else if (bankStates[i][j].currentBankState == CurrentBankState.Precharging)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("[pre] ");
                        }
                        else if (bankStates[i][j].currentBankState == CurrentBankState.Refreshing)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("[ref] ");
                        }
                        else if (bankStates[i][j].currentBankState == CurrentBankState.PowerDown)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("[lowp] ");
                        }
                    }
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine(""); // effectively just cout<<endl;
                }
            }

            if (Config.dram_config.DEBUG_CMD_Q)
            {
                commandQueue.print();
            }

            commandQueue.step();



        }
        public void printStats(bool finalStats = false)
        {

            uint myChannel = parentMemorySystem.systemID;

            //if we are not at the end of the epoch, make sure to adjust for the actual number of cycles elapsed

            UInt64 cyclesElapsed = (currentClockCycle % Config. dram_config.EPOCH_LENGTH == 0) ? Config.dram_config.EPOCH_LENGTH : currentClockCycle % Config.dram_config.EPOCH_LENGTH;
            uint bytesPerTransaction = (Config.dram_config.JEDEC_DATA_BUS_BITS * Config.dram_config.BL) / 8;
            UInt64 totalBytesTransferred = totalTransactions * bytesPerTransaction;
            double secondsThisEpoch = (double)cyclesElapsed * Config.dram_config.tCK * 1E-9;

            // only per rank
            List<double> backgroundPower = new List<double>((int)Config.dram_config.NUM_RANKS);
            List<double> burstPower = new List<double>((int)Config.dram_config.NUM_RANKS);
            List<double> refreshPower = new List<double>((int)Config.dram_config.NUM_RANKS);
            List<double> actprePower = new List<double>((int)Config.dram_config.NUM_RANKS);
            List<double> averagePower = new List<double>((int)Config.dram_config.NUM_RANKS);
            // per bank variables
            List<double> averageLatency = new List<double>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            List<double> bandwidth = new List<double>((int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS);
            for (int i=0; i < Config.dram_config.NUM_RANKS; i++)
            {
                backgroundPower.Add(0.0);
                burstPower.Add(0.0);
                refreshPower.Add(0.0);
                actprePower.Add(0.0);
                averagePower.Add(0.0);

            }
            for (int i = 0; i < (int)Config.dram_config.NUM_RANKS * (int)Config.dram_config.NUM_BANKS; i++)
            {
                averageLatency.Add(0.0);
                bandwidth.Add(0.0);
            }


                double totalBandwidth = 0.0;
            for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
            {
                for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                {
                    bandwidth[SEQUENTIAL(i, j)] = (((double)(totalReadsPerBank[SEQUENTIAL(i, j)] + totalWritesPerBank[SEQUENTIAL(i, j)]) * (double)bytesPerTransaction) / (1024.0 * 1024.0 * 1024.0)) / secondsThisEpoch;
                    averageLatency[SEQUENTIAL(i, j)] = ((float)totalEpochLatency[SEQUENTIAL(i, j)] / (float)(totalReadsPerBank[SEQUENTIAL(i, j)])) * Config.dram_config.tCK;
                    totalBandwidth += bandwidth[SEQUENTIAL(i, j)];
                    totalReadsPerRank[i] += totalReadsPerBank[SEQUENTIAL(i, j)];
                    totalWritesPerRank[i] += totalWritesPerBank[SEQUENTIAL(i, j)];
                }
            }
            //# ifdef LOG_OUTPUT
            //            dramsim_log.precision(3);
            //            dramsim_log.setf(ios::fixed, ios::floatfield);
            //#else
            //            cout.precision(3);
            //            cout.setf(ios::fixed, ios::floatfield);
            //#endif

            if (Config.DEBUG_MEMORY)
            {
                DEBUG.WriteLine(" =======================================================");
                DEBUG.WriteLine(" ============== Printing Statistics [id:" + parentMemorySystem.systemID + "]==============");
                DEBUG.Write("   Total Return Transactions : " + totalTransactions);
                DEBUG.WriteLine(" (" + totalBytesTransferred + " bytes) aggregate average bandwidth " + totalBandwidth + "GB/s");
            }
            for (int r = 0; r < Config.dram_config.NUM_RANKS; r++)
            {

                if (Config.DEBUG_MEMORY)
                {
                    DEBUG.WriteLine("      -Rank   " + r + " : ");
                    DEBUG.Write("        -Reads  : " + totalReadsPerRank[r]);
                    DEBUG.WriteLine(" (" + totalReadsPerRank[r] * bytesPerTransaction + " bytes)");
                    DEBUG.Write("        -Writes : " + totalWritesPerRank[r]);
                    DEBUG.WriteLine(" (" + totalWritesPerRank[r] * bytesPerTransaction + " bytes)");
                    for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                    {
                        DEBUG.WriteLine("        -Bandwidth / Latency  (Bank " + j + "): " + bandwidth[SEQUENTIAL(r, j)] + " GB/s\t\t" + averageLatency[SEQUENTIAL(r, j)] + " ns");
                    }

                }
                // factor of 1000 at the end is to account for the fact that totalEnergy is accumulated in mJ since IDD values are given in mA
                backgroundPower[r] = ((double)backgroundEnergy[r] / (double)(cyclesElapsed)) * Config.dram_config.Vdd / 1000.0;
                burstPower[r] = ((double)burstEnergy[r] / (double)(cyclesElapsed)) * Config.dram_config.Vdd / 1000.0;
                refreshPower[r] = ((double)refreshEnergy[r] / (double)(cyclesElapsed)) * Config.dram_config.Vdd / 1000.0;
                actprePower[r] = ((double)actpreEnergy[r] / (double)(cyclesElapsed)) * Config.dram_config.Vdd / 1000.0;
                averagePower[r] = ((backgroundEnergy[r] + burstEnergy[r] + refreshEnergy[r] + actpreEnergy[r]) / (double)cyclesElapsed) * Config.dram_config.Vdd / 1000.0;

                if (parentMemorySystem.ReportPower != null)
                {
                    parentMemorySystem.ReportPower(backgroundPower[r], burstPower[r], refreshPower[r], actprePower[r]);
                }

                if (Config.DEBUG_MEMORY)
                {
                    DEBUG.WriteLine(" == Power Data for Rank        " + r);
                    DEBUG.WriteLine("   Average Power (watts)     : " + averagePower[r]);
                    DEBUG.WriteLine("     -Background (watts)     : " + backgroundPower[r]);
                    DEBUG.WriteLine("     -Act/Pre    (watts)     : " + actprePower[r]);
                    DEBUG.WriteLine("     -Burst      (watts)     : " + burstPower[r]);
                    DEBUG.WriteLine("     -Refresh    (watts)     : " + refreshPower[r]);

                }
            }
           

            // only print the latency histogram at the end of the simulation since it clogs the output too much to print every epoch
            if (finalStats)
            {
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine(" ---  Latency list (" + latencies.Count() + ")");
                if (Config.DEBUG_MEMORY)
                    DEBUG.WriteLine("       [lat] : #");

                latencies = latencies.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
                foreach(var it in latencies) {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine("       [" + it.Key + "-" + (it.Key + (DRAMConfig. HISTOGRAM_BIN_SIZE - 1)) + "] : " + it.Value);
                    
                }
                if (currentClockCycle % Config. dram_config.EPOCH_LENGTH == 0)
                {
                    if (Config.DEBUG_MEMORY)
                        DEBUG.WriteLine(" --- Grand Total Bank usage list");
                    for (int i = 0; i < Config.dram_config.NUM_RANKS; i++)
                    {
                        if (Config.DEBUG_MEMORY)
                            DEBUG.WriteLine("Rank " + i + ":");
                        for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                        {
                            if (Config.DEBUG_MEMORY)
                                DEBUG.WriteLine("  b" + j + ": " + grandTotalBankAccesses[SEQUENTIAL(i, j)]);
                        }
                    }
                }

            }


            if (Config.DEBUG_MEMORY)
                DEBUG.WriteLine();
            if (Config.DEBUG_MEMORY)
                DEBUG.WriteLine(" == Pending Transactions : " + pendingReadTransactions.Count() + " (" + currentClockCycle + ")==");
            /*
            for(size_t i=0;i<pendingReadTransactions.size();i++)
                {
                    PRINT( i << "] I've been waiting for "<<currentClockCycle-pendingReadTransactions[i].timeAdded<<endl;
                }
            */
//# ifdef LOG_OUTPUT
//            dramsim_log.flush();
//#endif

            resetStats();
        }
        public void resetStats()
        {
            for (int i = 0; i < Config. dram_config.NUM_RANKS; i++)
            {
                for (int j = 0; j < Config.dram_config.NUM_BANKS; j++)
                {
                    //XXX: this means the bank list won't be printed for partial epochs
                    grandTotalBankAccesses[SEQUENTIAL(i, j)] += totalReadsPerBank[SEQUENTIAL(i, j)] + totalWritesPerBank[SEQUENTIAL(i, j)];
                    totalReadsPerBank[SEQUENTIAL(i, j)] = 0;
                    totalWritesPerBank[SEQUENTIAL(i, j)] = 0;
                    totalEpochLatency[SEQUENTIAL(i, j)] = 0;
                }

                burstEnergy[i] = 0;
                actpreEnergy[i] = 0;
                refreshEnergy[i] = 0;
                backgroundEnergy[i] = 0;
                totalReadsPerRank[i] = 0;
                totalWritesPerRank[i] = 0;
            }
        }


    //fields
    public List<Transaction> transactionQueue;
    private Stream dramsim_log;
	public List<List<BankState>> bankStates;
        //functions
        public void insertHistogram(uint latencyValue, uint rank, uint bank)
        {
            totalEpochLatency[SEQUENTIAL((int)rank, (int)bank)] += latencyValue;
            //poor man's way to bin things.
            //  latencies[(latencyValue /InitReader. HISTOGRAM_BIN_SIZE) * InitReader.HISTOGRAM_BIN_SIZE]++;
            if (latencies.ContainsKey((latencyValue / DRAMConfig.HISTOGRAM_BIN_SIZE) * DRAMConfig.HISTOGRAM_BIN_SIZE))
            {
                latencies[(latencyValue / DRAMConfig.HISTOGRAM_BIN_SIZE) * DRAMConfig.HISTOGRAM_BIN_SIZE]++;
            }
            else
            {
                latencies.Add((latencyValue / DRAMConfig.HISTOGRAM_BIN_SIZE) * DRAMConfig.HISTOGRAM_BIN_SIZE, 1);
            }
        }

    //fields
    public MemorySystem parentMemorySystem;

        public CommandQueue commandQueue;
        public BusPacket poppedBusPacket;
        public List<uint> refreshCountdown;
        public List<BusPacket> writeDataToSend;
        public List<uint> writeDataCountdown;
        public List<Transaction> returnTransaction=new List<Transaction>();
        public List<Transaction> pendingReadTransactions=new List<Transaction>();
        public Dictionary<uint, uint> latencies = new Dictionary<uint, uint>(); // latencyValue -> latencyCount
        public List<bool> powerDown;

        public List<Rank> ranks;

        //output file
       

	// these packets are counting down waiting to be transmitted on the "bus"
	public BusPacket outgoingCmdPacket;
        public uint cmdCyclesLeft;
        public BusPacket outgoingDataPacket;
        public uint dataCyclesLeft;

        public UInt64 totalTransactions;
        public List<UInt64> grandTotalBankAccesses;
        public List<UInt64> totalReadsPerBank;
        public List<UInt64> totalWritesPerBank;

        public List<UInt64> totalReadsPerRank;
        public List<UInt64> totalWritesPerRank;


        public List<UInt64> totalEpochLatency;

        public uint channelBitWidth;
        public uint rankBitWidth;
        public uint bankBitWidth;
        public uint rowBitWidth;
        public uint colBitWidth;
        public uint byteOffsetWidth;


        public uint refreshRank;


	// energy values are per rank -- SST uses these directly, so make these public 
	public List<UInt64> backgroundEnergy;
        public List<UInt64> burstEnergy;
        public List<UInt64> actpreEnergy;
        public List<UInt64> refreshEnergy;
        public powerCallBack_t ReportPower = null;
    }
    
}
