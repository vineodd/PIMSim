using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_queue
    {
       public UInt32 valid;             /*! HMC-SIM: HMC_QUEUE_T: VALID BIT */
        public UInt64[] packet;	/*! HMC-SIM: HMC_QUEUE_T: PACKET */
        public hmc_queue()
        {
          packet  = new UInt64[Macros.HMC_MAX_UQ_PACKET];
            for (int i = 0; i < Macros.HMC_MAX_UQ_PACKET; i++)
                packet[i] = new ulong();
        }

    }
}
