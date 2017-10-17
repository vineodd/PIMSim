using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_bank
    {
        //struct hmc_dram_t *drams;	    /*! HMC-SIM: HMC_BANK_T: DRAMS */

        public UInt32 id;                /*! HMC-SIM: HMC_BANK_T: BANK ID */
                                         /* 16-BYTE BANK INTERLEAVE */
        public UInt32 delay;                     /*! HMC-SIM: HMC_BANK_T: CYCLES UNTIL BANK IS AVAILABLE */
        public UInt32 valid;                     /*! HMC-SIM: HMC_BANK_T: RESPONSE PACKET IS VALID */
        public UInt64[] packet; /*! HMC-SIM: HMC_BANK_T: RESPONSE PACKET */
        public hmc_bank()
        {
            packet  = new UInt64[Macros.HMC_MAX_UQ_PACKET];
            for (int i = 0; i < Macros.HMC_MAX_UQ_PACKET; i++)
                packet[i] = new ulong();
        }
    }
}
