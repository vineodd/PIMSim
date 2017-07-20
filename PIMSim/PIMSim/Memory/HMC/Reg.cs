using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_reg
    {
       public hmc_reg_def type;     /*! HMC-SIM: HMC_REG_T: REGISTER TYPE */
       public  UInt64 phy_idx;   /*! HMC-SIM: HMC_REG_T: REGISTER PHYSICAL DEVICE INDEX */
       public UInt64 reg;		/*! HMC-SIM: HMC_REG_T: REGISTER STORAGE */
    }
    public enum hmc_reg_def
    {
        HMC_RW,             /*! HMC-SIM: HMC_REG_DEF_T: READ+WRITE REGISTER */
        HMC_RO,             /*! HMC-SIM: HMC_REG_DEF_T: READ-ONLY REGISTER */
        HMC_RWS				/*! HMC-SIM: HMC_REG_DEF_T: CLEAR ON WRITE REGISTER */
    }
}
