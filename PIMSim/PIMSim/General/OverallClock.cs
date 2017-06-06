#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Memory;

#endregion

namespace SimplePIM.General
{
    /// <summary>
    /// Overall Clock Synchronization.
    /// </summary>
    public static class OverallClock 
    {
        #region Static Varibales

        public readonly static UInt64 reference_clock = Config.proc_frequent;

        #endregion

        #region Private Vaiables

        private static List<double> host_cpu_clock_factor = new List<double>();
        private static List<double> ram_clock_factor = new List<double>();
        private static List<double> pimunit_clock_factor = new List<double>();


        #endregion

        #region Public Variables
        public static UInt64 cycle = 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Init all clock factor.
        /// </summary>
        public static void InitClock()
        {
            for(int i=0;i<Config.N; i++)
            {
                host_cpu_clock_factor.Add(Config.host_clock_factor);
            }
            for(int i=0;i< MemorySelector.get_mem_count; i++)
            {
                ram_clock_factor.Add(1);
            }
            for (int i = 0; i < PIMConfigs.pim_cu_count; i++)
            {
                pimunit_clock_factor.Add(1);
            }

        }

        /// <summary>
        /// cycle++
        /// </summary>
        public static void Step()
        {
            cycle++;
        }

        /// <summary>
        /// Processors Synchronous clock.
        /// </summary>
        /// <param name="pid">id of Processor.</param>
        /// <returns></returns>
        public static bool ifProcStep(int pid)
        {
            if (cycle % host_cpu_clock_factor[pid] == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Memory Synchronous clock.
        /// </summary>
        /// <param name="pid">id of memory objects.</param>
        /// <returns></returns>
        public static bool ifMemoryStep(int pid)
        {
            if (cycle % ram_clock_factor[pid] == 0)
                return true;
            return false;
        }

        /// <summary>
        /// PIM Unit Synchronous clock.
        /// </summary>
        /// <param name="pid">id of PIM Unit.</param>
        /// <returns></returns>
        public static bool ifPIMUnitStep(int pid)
        {
            if (cycle % pimunit_clock_factor[pid] == 0)
                return true;
            return false;
        }
        #endregion
    }
}
