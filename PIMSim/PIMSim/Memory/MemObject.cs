using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.General;

using SimplePIM.Procs;
namespace SimplePIM.Memory
{
    public abstract class MemObject :SimulatorObj
    {

        public int pid;

        public abstract bool addTransation(MemRequest req);

        public abstract void attach_mctrl(ref Mctrl mctrl_);

        public abstract int get_lock_index(UInt64 addr);
      //  public abstract void attach_proc_return(ref List<Proc> proc_);
    }
}
