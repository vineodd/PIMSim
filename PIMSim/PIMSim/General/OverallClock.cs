using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Memory;

namespace SimplePIM.General
{
    public static class OverallClock 
    {
        private readonly static int reference_clock = 1;
        public static List<double> host_cpu_clock_factor = new List<double>();
        public static List<double> ram_clock_factor = new List<double>();
        public static List<double> pimunit_clock_factor = new List<double>();
        public static UInt64 cycle = 0;
        public static void InitClock()
        {
            for(int i=0;i<Config.N; i++)
            {
                host_cpu_clock_factor.Add(Config.host_clock_factor);
            }
            for(int i=0;i< MemorySelecter.get_mem_count(); i++)
            {
                ram_clock_factor.Add(60);
            }
            for (int i = 0; i < Config.pim_config.pim_cu_count; i++)
            {
                pimunit_clock_factor.Add(2);
            }

        }
        public static void Step()
        {
            cycle++;
        }
        public static bool ifProcStep(int pid)
        {
            if (cycle % host_cpu_clock_factor[pid] == 0)
                return true;
            return false;
        }
        public static bool ifMemoryStep(int pid)
        {
            if (cycle % ram_clock_factor[pid] == 0)
                return true;
            return false;
        }
        public static bool ifPIMUnitStep(int pid)
        {
            if (cycle % pimunit_clock_factor[pid] == 0)
                return true;
            return false;
        }
    }
}
