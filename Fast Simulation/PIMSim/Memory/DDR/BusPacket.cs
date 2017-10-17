using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Procs;

namespace PIMSim.Memory.DDR
{
    public class BusPacket
    {

        public Stream dramsim_log;
        public BusPacketType busPacketType;
        public uint column;
        public uint row;
        public uint bank;
        public uint rank;
        public UInt64 physicalAddress;
        public UInt64 data;

        public CallBackInfo callback;
        public BusPacket(BusPacketType packtype, UInt64 physicalAddr, uint col, uint rw, int r, uint b, UInt64 dat,CallBackInfo callback_,Stream dramsim_log_)
        {
            dramsim_log = dramsim_log_;
            callback = callback_;
            busPacketType = packtype;
            column = col;
            row = rw;
            bank = b;
            physicalAddress = physicalAddr;
            data = dat;

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
