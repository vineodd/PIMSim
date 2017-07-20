using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_link
    {
       public UInt32 id;            /*! HMC-SIM: HMC_LINK_T: LINK ID */
        public UInt32 quad;          /*! HMC-SIM: HMC_LINK_T: ASSOCIATED QUADRANT */
        public UInt32 src_cub;       /*! HMC-SIM: HMC_LINK_T: SOURCE CUB */
        public UInt32 dest_cub;      /*! HMC-SIM: HMC_LINK_T: DESTINATION CUB */

        public hmc_link_def type;		/*! HMC-SIM: HMC_LINK_T: LINK TYPE */
    }
    public enum hmc_link_def
    {
        HMC_LINK_DEV_DEV,       /*! HMC-SIM: HMC_LINK_DEF_T: DEVICE TO DEVICE LINK */
        HMC_LINK_HOST_DEV		/*! HMC-SIM: HMC_LINK_DEF_T: HOST TO DEVICE LINK */
    }
}
