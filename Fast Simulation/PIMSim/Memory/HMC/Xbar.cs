using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_xbar
    {
        public List<hmc_queue> xbar_rqst;   /*! HMC-SIM: HMC_XBAR_T: CROSSBAR REQUEST QUEUE */
        public List<hmc_queue> xbar_rsp;	/*! HMC-SIM: HMC_XBAR_T: CROSSBAR RESPONSE QUEUE */
        public hmc_xbar()
        {
            xbar_rqst = new List<hmc_queue>();
            xbar_rsp = new List<hmc_queue>();
        }
    }
}
