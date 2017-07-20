using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_token
    {
        public uint status;                       /*! HMC-SIM: HMC_TOKEN_T: Status bits
                                          0 = unused; 1=set but no response;
                                          2 = set and response ready
                                          */
        public hmc_response rsp;                   /*! HMC-SIM: HMC_TOKEN_T: Response type */
        public UInt32 rsp_size;                    /*! HMC-SIM: HMC_TOKEN_T: Response data size */
        public UInt32 device;                      /*! HMC-SIM: HMC_TOKEN_T: Response device */
        public UInt32 link;                        /*! HMC-SIM: HMC_TOKEN_T: Response link */
        public UInt32 slot;                        /*! HMC-SIM: HMC_TOKEN_T: Response slot */
        public UInt64 en_clock;                    /*! HMC-SIM: HMC_TOKEN_T: Clock cycle of incoming packet */
        public uint[] data=new uint[256];                    /*! HMC-SIM: HMC_TOKEN_T: Response data */
    }
 
}
