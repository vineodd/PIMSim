using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_quad
    {

        public List<hmc_vault> vaults;    /*! HMC-SIM: HMC_QUAD_T: VAULT STRUCTURE */

        public UInt32 id;        /*! HMC-SIM: HMC_QUAD_T: QUADRANT ID */
        public hmc_quad()
        {
            vaults = new List<hmc_vault>();
        }
    }
}
