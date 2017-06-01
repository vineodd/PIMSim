using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimplePIM.Configs;
using System.Threading.Tasks;
using SimplePIM.Procs;

namespace SimplePIM.PIM
{
    public static class Coherence
    {
        public static SpinLock spin_lock;
        public static Consistency consistency;
        public static List<Proc> proc;
        
        public static void init()
        {

            consistency = Config.pim_config. Consistency_Model;
            if(consistency== Consistency.SpinLock)
            {
                spin_lock = new SpinLock();
            }
        }
        public static void flush(UInt64 addr)
        {
            foreach (var p in proc)
                p.flush(addr);
        }
        public static void linkproc(List<Proc> proc_)
        {
            proc = proc_;
        }
    }
}
