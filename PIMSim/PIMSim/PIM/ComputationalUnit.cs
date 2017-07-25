using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Procs;
using PIMSim.Configs;

namespace PIMSim.PIM
{
    public abstract class ComputationalUnit:SimulatorObj
    {
        public int output_count = 0;
        public int input_count = 0;

        /// <summary>
        /// [Memory Read Callback]
        /// Triger by a memory read operation complete 
        /// </summary>
        public ReadCallBack read_callback;

        /// <summary>
        /// [Memory Write Callback]
        /// Triger by a memory write operation complete
        /// </summary>
        public WriteCallBack write_callback;
        public int id;
        public abstract void PrintStatus();
        public UInt64 bandwidth_bit = 0;
        public double interal_bandwidth => bandwidth_bit / 8 //byte
                / 1024//KB
                / 1024//MB
                * 1.0 / GlobalTimer.tick //MB/cycle
                * GlobalTimer.reference_clock;

        public abstract bool outstanding_requests();

        public virtual bool done() { return true; }
    }
   
}
