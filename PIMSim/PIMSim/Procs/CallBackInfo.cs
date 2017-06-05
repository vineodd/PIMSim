using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;
using SimplePIM.Configs;
using SimplePIM.PIM;

namespace SimplePIM.Procs
{
    public class CallBackInfo
    {
        public object source;
        public UInt64 address;
        public UInt64 block_addr;
        public UInt64 done_cycle;
        public bool pim;

        public object getsource => source;
        
    }
}
