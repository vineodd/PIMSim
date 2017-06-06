using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimplePIM.Memory.DDR
{
    public class BusPacket
    {
        public BusPacket()
        {

        }
        public Stream dramsim_log;
        public BusPacketType busPacketType;
        public uint column;
        public uint row;
        public uint bank;
        public uint rank;
        public UInt64 physicalAddress;
        public UInt64 data;

        public UInt64 block_addr;
        public List<int> pid;
        public bool pim;
        public BusPacket(BusPacketType packtype, UInt64 physicalAddr, uint col, uint rw, int r, uint b, UInt64 dat,UInt64 block_add,List<int> pid_, bool pim_,Stream dramsim_log_)
        {
            dramsim_log = dramsim_log_;
            block_addr = block_add;
            busPacketType = packtype;
            column = col;
            row = rw;
            bank = b;
            physicalAddress = physicalAddr;
            data = dat;
            pid = pid_;
        }
        public void print()
        {

        }
        public void print(UInt64 currentClockCycle, bool dataStart)
        {

        }
        public void printData() { }

    }
    public enum BusPacketType
    {
        READ,
        READ_P,
        WRITE,
        WRITE_P,
        ACTIVATE,
        PRECHARGE,
        REFRESH,
        DATA
    }
}
