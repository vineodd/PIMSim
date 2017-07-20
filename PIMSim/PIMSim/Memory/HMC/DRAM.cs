using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public  class hmc_dram
    {
       public UInt16[] elems=new UInt16[8];      /*! HMC-SIM: HMC_DRAM_T: DRAM ELEMENTS */

       public UInt32 id;			/*! HMC-SIM: HMC_DRAM_T: DRAM ID */
    }
}
