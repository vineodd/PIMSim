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
    public class Rank : DRAMSimObject
    {
        private int id;
        private Stream dramsim_log;
        private int incomingWriteBank;
        private int incomingWriteRow;
        private int incomingWriteColumn;
        private bool isPowerDown;

        public Rank(Stream dramsim_log_)
        {

            id = -1;
            dramsim_log = dramsim_log_;
            isPowerDown = false;
            refreshWaiting = false;
            readReturnCountdown = new List<uint>();
            banks = new List<Bank>((int)Config.dram_config.NUM_BANKS);
            bankStates = new List<BankState>((int)Config.dram_config.NUM_BANKS);
            for (int i = 0; i < (int)Config.dram_config.NUM_BANKS; i++)
            {
                banks.Add(new Bank(dramsim_log_));
                bankStates.Add(new BankState(dramsim_log_));
            }

            memoryController = null;
            outgoingDataPacket = null;
            dataCyclesLeft = 0;
            currentClockCycle = 0;

        }

        public void receiveFromBus(ref BusPacket packet)
        {
            if (Config.dram_config.DEBUG_BUS)
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine(" -- R" + this.id + " Receiving On Bus    : ");
                packet.print();
            }
            if (Config.dram_config.VERIFICATION_OUTPUT)
            {
                packet.print(currentClockCycle, false);
            }

            switch (packet.busPacketType)
            {
                case BusPacketType.READ:
                    //make sure a read is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.RowActive ||
                            currentClockCycle < bankStates[(int)packet.bank].nextRead ||
                            packet.row != bankStates[(int)packet.bank].openRowAddress)
                    {
                        packet.print();
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received a READ when not allowed");
                        Environment.Exit(1);
                    }

                    //update state table
                    bankStates[(int)packet.bank].nextPrecharge = Math.Max(bankStates[(int)packet.bank].nextPrecharge, currentClockCycle + Config.dram_config.READ_TO_PRE_DELAY);
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        bankStates[i].nextRead = Math.Max(bankStates[i].nextRead, currentClockCycle + Math.Max(Config.dram_config.tCCD, Config.dram_config.BL / 2));
                        bankStates[i].nextWrite = Math.Max(bankStates[i].nextWrite, currentClockCycle + Config.dram_config.READ_TO_WRITE_DELAY);
                    }

                    //get the read data and put it in the storage which delays until the appropriate time (RL)
                    if (Config.dram_config.NO_STORAGE)
                        banks[(int)packet.bank].read(ref packet);
                    else
                        packet.busPacketType = BusPacketType.DATA;

                    readReturnPacket.Add(packet);
                    readReturnCountdown.Add(Config.dram_config.RL);
                    break;
                case BusPacketType.READ_P:
                    //make sure a read is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.RowActive ||
                            currentClockCycle < bankStates[(int)packet.bank].nextRead ||
                            packet.row != bankStates[(int)packet.bank].openRowAddress)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR == Error - Rank " + id + " received a READ_P when not allowed");
                        Environment.Exit(1);
                    }

                    //update state table
                    bankStates[(int)packet.bank].currentBankState = CurrentBankState.Idle;
                    bankStates[(int)packet.bank].nextActivate = Math.Max(bankStates[(int)packet.bank].nextActivate, currentClockCycle + Config.dram_config.READ_AUTOPRE_DELAY);
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        //will set next read/write for all banks - including current (which shouldnt matter since its now idle)
                        bankStates[i].nextRead = Math.Max(bankStates[i].nextRead, currentClockCycle + Math.Max(Config.dram_config.BL / 2, Config.dram_config.tCCD));
                        bankStates[i].nextWrite = Math.Max(bankStates[i].nextWrite, currentClockCycle + Config.dram_config.READ_TO_WRITE_DELAY);
                    }

                    //get the read data and put it in the storage which delays until the appropriate time (RL)
                    if (Config.dram_config.NO_STORAGE)
                        banks[(int)packet.bank].read(ref packet);
                    else
                        packet.busPacketType = BusPacketType.DATA;


                    readReturnPacket.Add(packet);
                    readReturnCountdown.Add(Config.dram_config.RL);
                    break;
                case BusPacketType.WRITE:
                    //make sure a write is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.RowActive ||
                            currentClockCycle < bankStates[(int)packet.bank].nextWrite ||
                            packet.row != bankStates[(int)packet.bank].openRowAddress)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received a WRITE when not allowed");
                        bankStates[(int)packet.bank].print();
                        Environment.Exit(1);
                    }

                    //update state table
                    bankStates[(int)packet.bank].nextPrecharge = Math.Max(bankStates[(int)packet.bank].nextPrecharge, currentClockCycle + Config.dram_config.WRITE_TO_PRE_DELAY);
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        bankStates[i].nextRead = Math.Max(bankStates[i].nextRead, currentClockCycle + Config.dram_config.WRITE_TO_READ_DELAY_B);
                        bankStates[i].nextWrite = Math.Max(bankStates[i].nextWrite, currentClockCycle + Math.Max(Config.dram_config.BL / 2, Config.dram_config.tCCD));
                    }

                    //take note of where data is going when it arrives
                    incomingWriteBank = (int)packet.bank;
                    incomingWriteRow = (int)packet.row;
                    incomingWriteColumn = (int)packet.column;
                    // delete(packet);
                    // packet.
                    break;
                case BusPacketType.WRITE_P:
                    //make sure a write is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.RowActive ||
                            currentClockCycle < bankStates[(int)packet.bank].nextWrite ||
                            packet.row != bankStates[(int)packet.bank].openRowAddress)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received a WRITE_P when not allowed");
                        Environment.Exit(1);
                    }

                    //update state table
                    bankStates[(int)packet.bank].currentBankState = CurrentBankState.Idle;
                    bankStates[(int)packet.bank].nextActivate = Math.Max(bankStates[(int)packet.bank].nextActivate, currentClockCycle + Config.dram_config.WRITE_AUTOPRE_DELAY);
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        bankStates[i].nextWrite = Math.Max(bankStates[i].nextWrite, currentClockCycle + Math.Max(Config.dram_config.tCCD, Config.dram_config.BL / 2));
                        bankStates[i].nextRead = Math.Max(bankStates[i].nextRead, currentClockCycle + Config.dram_config.WRITE_TO_READ_DELAY_B);
                    }

                    //take note of where data is going when it arrives
                    incomingWriteBank = (int)packet.bank;
                    incomingWriteRow = (int)packet.row;
                    incomingWriteColumn = (int)packet.column;
                    //  delete(packet);
                    break;
                case BusPacketType.ACTIVATE:
                    //make sure activate is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.Idle ||
                            currentClockCycle < bankStates[(int)packet.bank].nextActivate)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received an ACT when not allowed");
                        packet.print();
                        bankStates[(int)packet.bank].print();
                        Environment.Exit(1);
                    }

                    bankStates[(int)packet.bank].currentBankState = CurrentBankState.RowActive;
                    bankStates[(int)packet.bank].nextActivate = currentClockCycle + Config.dram_config.tRC;
                    bankStates[(int)packet.bank].openRowAddress = (int)packet.row;

                    //if AL is greater than one, then posted-cas is enabled - handle accordingly
                    if (Config.dram_config.AL > 0)
                    {
                        bankStates[(int)packet.bank].nextWrite = currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL);
                        bankStates[(int)packet.bank].nextRead = currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL);
                    }
                    else
                    {
                        bankStates[(int)packet.bank].nextWrite = currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL);
                        bankStates[(int)packet.bank].nextRead = currentClockCycle + (Config.dram_config.tRCD - Config.dram_config.AL);
                    }

                    bankStates[(int)packet.bank].nextPrecharge = currentClockCycle + Config.dram_config.tRAS;
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        if (i != packet.bank)
                        {
                            bankStates[i].nextActivate = Math.Max(bankStates[i].nextActivate, currentClockCycle + Config.dram_config.tRRD);
                        }
                    }
                    //  delete(packet);
                    break;
                case BusPacketType.PRECHARGE:
                    //make sure precharge is allowed
                    if (bankStates[(int)packet.bank].currentBankState != CurrentBankState.RowActive ||
                            currentClockCycle < bankStates[(int)packet.bank].nextPrecharge)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received a PRE when not allowed");
                        Environment.Exit(1);
                    }

                    bankStates[(int)packet.bank].currentBankState = CurrentBankState.Idle;
                    bankStates[(int)packet.bank].nextActivate = Math.Max(bankStates[(int)packet.bank].nextActivate, currentClockCycle + Config.dram_config.tRP);
                    //  delete(packet);
                    break;
                case BusPacketType.REFRESH:
                    refreshWaiting = false;
                    for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
                    {
                        if (bankStates[i].currentBankState != CurrentBankState.Idle)
                        {
                            if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Rank " + id + " received a REF when not allowed");
                            Environment.Exit(1);
                        }
                        bankStates[i].nextActivate = currentClockCycle + Config.dram_config.tRFC;
                    }
                    //  delete(packet);
                    break;
                case BusPacketType.DATA:
                    // TODO: replace this check with something that works?
                    /*
                    if(packet->bank != incomingWriteBank ||
                         packet->row != incomingWriteRow ||
                         packet->column != incomingWriteColumn)
                        {
                            cout << "== Error - Rank " << id << " received a DATA packet to the wrong place" << endl;
                            packet->print();
                            bankStates[packet->bank].print();
                            exit(0);
                        }
                    */
                    if (Config.dram_config.NO_STORAGE)
                        banks[(int)packet.bank].write(ref packet);

                    // end of the line for the write packet

                    //  delete(packet);
                    break;
                default:
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Unknown BusPacketType trying to be sent to Bank");
                    Environment.Exit(1);
                    break;
            }
        }
        public void attachMemoryController(MemoryController memoryController)
        {
            this.memoryController = memoryController;
        }
        public int getId()
        {
            return this.id;
        }
        public void setId(int id)
        {
            this.id = id;
        }
        public override void update()
        {

            // An outgoing packet is one that is currently sending on the bus
            // do the book keeping for the packet's time left on the bus
            if (outgoingDataPacket != null)
            {
                dataCyclesLeft--;
                if (dataCyclesLeft == 0)
                {
                    //if the packet is done on the bus, call receiveFromBus and free up the bus
                    memoryController.receiveFromBus(ref outgoingDataPacket);
                    outgoingDataPacket = null;
                }
            }

            // decrement the counter for all packets waiting to be sent back
            for (int i = 0; i < readReturnCountdown.Count(); i++)
            {
                readReturnCountdown[i]--;
            }


            if (readReturnCountdown.Count() > 0 && readReturnCountdown[0] == 0)
            {
                // RL time has passed since the read was issued; this packet is
                // ready to go out on the bus

                outgoingDataPacket = readReturnPacket[0];
                dataCyclesLeft = Config.dram_config.BL / 2;

                // remove the packet from the ranks
                //   readReturnPacket.erase(readReturnPacket.begin());
                readReturnPacket.RemoveAt(0);
                // readReturnCountdown.erase(readReturnCountdown.begin());
                readReturnCountdown.RemoveAt(0);
                if (Config.dram_config.DEBUG_BUS)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine(" -- R" + this.id + " Issuing On Data Bus : ");
                    outgoingDataPacket.print();
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine();
                }

            }
        }
        public void powerUp()
        {
            if (!isPowerDown)
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Trying to power up rank " + id + " while it is not already powered down");
                Environment.Exit(1);
            }

            isPowerDown = false;

            for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
            {
                if (bankStates[i].nextPowerUp > currentClockCycle)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Trying to power up rank " + id + " before we're allowed to");
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine(bankStates[i].nextPowerUp + "    " + currentClockCycle);
                    Environment.Exit(1);
                }
                bankStates[i].nextActivate = currentClockCycle + Config.dram_config.tXP;
                bankStates[i].currentBankState = CurrentBankState.Idle;


            }
        }
        public void powerDown()
        {
            //perform checks
            for (int i = 0; i < Config.dram_config.NUM_BANKS; i++)
            {
                if (bankStates[i].currentBankState != CurrentBankState.Idle)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Trying to power down rank " + id + " while not all banks are idle");
                    Environment.Exit(1);
                }

                bankStates[i].nextPowerUp = currentClockCycle + Config.dram_config.tCKE;
                bankStates[i].currentBankState = CurrentBankState.PowerDown;
            }

            isPowerDown = true;
        }

        //fields
        public MemoryController memoryController;
        public BusPacket outgoingDataPacket;
        public uint dataCyclesLeft;
        public bool refreshWaiting;

        //these are vectors so that each element is per-bank
        public List<BusPacket> readReturnPacket = new List<BusPacket>();
        public List<uint> readReturnCountdown;
        public List<Bank> banks;
        public List<BankState> bankStates;

    }
}
