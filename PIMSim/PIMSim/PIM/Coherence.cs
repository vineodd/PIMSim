using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PIMSim.Configs;
using System.Threading.Tasks;
using PIMSim.Procs;

namespace PIMSim.PIM
{
    public static class Coherence
    {
        public static SpinLock spin_lock;
        public static Consistency consistency;
        public static List<Proc> proc;
        public static List<UInt64> flush_queue = new List<ulong>();
        
        public static void init()
        {

            consistency = PIMConfigs.Consistency_Model;
            if(consistency== Consistency.SpinLock)
            {
                spin_lock = new SpinLock();
            }
        }
        public static bool flush(UInt64 addr, bool actual = false)
        {

            bool stall = true;
            foreach (var p in proc)
                stall = p.flush(addr, actual);
            if (stall)
                return true;
            else
                return false;

        }
        public static void linkproc(List<Proc> proc_)
        {
            proc = proc_;
        }
    }
}
