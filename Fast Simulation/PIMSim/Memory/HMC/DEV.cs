using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_dev
    {
        public List<hmc_link> links;        /* HMC-SIM: HMC_DEV_T: LINK STRUCTURE */

        public List<hmc_quad> quads;        /*! HMC-SIM: HMC_DEV_T: QUADRANT STRUCTURE */

        public List<hmc_xbar> xbar;     /*! HMC-SIM: HMC_DEV_T: CROSSBAR STRUCTURE */

        public hmc_reg[] regs;   /*! HMC-SIM: HMC_DEV_T: DEVICE CONFIGURATION REGISTERS */

        public UInt32 id;                /*! HMC-SIM: HMC_DEV_T: CUBE ID */

        public UInt16 seq;				/*! HMC-SIM: HMC_DEV_T: SEQUENCE NUMBER */
        public hmc_dev()
        {
            links = new List<hmc_link>();
            quads = new List<hmc_quad>();
            xbar = new List<hmc_xbar>();
            regs = new hmc_reg[Macros.HMC_NUM_REGS];
            for (int i = 0; i < Macros.HMC_NUM_REGS; i++)
                regs[i] = new hmc_reg();
        }
    }
}
