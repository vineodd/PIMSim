using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Procs;

namespace PIMSim.Memory.DDR
{
    public delegate void CallBack();
    // public delegate void Callback_t(uint id, UInt64 address,UInt64 block_addr, UInt64 done_cycle,bool pim_);
    public delegate void Callback_t(uint id, UInt64 addr, UInt64 done_cycle, CallBackInfo callback);
    public delegate void powerCallBack_t(double bgpower, double burstpower, double refreshpower, double actprepower);

    public delegate void ClockUpdateCB();

}
