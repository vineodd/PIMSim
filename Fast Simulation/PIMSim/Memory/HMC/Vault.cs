using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_vault
    {
        public List<hmc_bank> banks;	/*! HMC-SIM: HMC_VAULT_T: BANK STRUCTURE */


	    public List<hmc_queue> rqst_queue;	/*! HMC-SIM: HMC_VAULT_T: REQUEST PACKET QUEUE */
	    public  List<hmc_queue> rsp_queue;	/*! HMC-SIM: HMC_VAULT_T: REQUEST PACKET QUEUE */

	    public UInt32 id;			/*! HMC-SIM: HMC_VAULT_T: VAULT ID */
        public hmc_vault()
        {
            banks = new List<hmc_bank>();
            rqst_queue = new List<hmc_queue>();
            rsp_queue=new List<hmc_queue>();
        }
    }
}
