using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;

namespace SimplePIM.General
{
    public class ProcRequest : Request
    {
        public int pid;
        public UInt64 cycle;
        public RequestType type;

        //address
        public UInt64 actual_addr;
        public UInt64 block_addr;

        //timestamp
        public UInt64 ts_arrival=0;
        public UInt64 ts_departure=0;
        public UInt64 ts_issue=0;
        public int latency;
 
        public int queueing_latency;
        public bool ready = false;

        //


        public bool if_mem;
        public UInt64 pc;
        public void parse_ins(Instruction ins_)
        {
            pid = ins_.pid;
            switch (ins_.type)
            {
                case InstructionType.NOP:
                    type = RequestType.NOP;
                    break;
                case InstructionType.CALCULATION:
                    type = RequestType.CALCULATION;
                    break;
                case InstructionType.READ:
                    type = RequestType.READ;
                    break;
                case InstructionType.WRITE:
                    type = RequestType.WRITE;
                    break;
                default:
                    type = RequestType.NOP;
                    break;
            }
            pc = ins_.pc;
            if_mem = ins_.is_mem;
            cycle = ins_.cycle;
            actual_addr = ins_.address;
            block_addr = ins_.block_addr;
            
        }
    }

    public enum RequestType
    {
        READ,
        WRITE,
        NOP,
        CALCULATION
    }
}
