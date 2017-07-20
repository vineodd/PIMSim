using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_power
    {
      public  float link_phy;           /*! HMC_POWER_T: POWER FOR EACH LINK PHY PER CLOCK */
        public float link_local_route;   /*! HMC_POWER_T: POWER FOR LOCAL LINK ROUTE TO QUAD */
        public float link_remote_route;  /*! HMC_POWER_T: POWER FOR REMOTE LINK ROUTE TO QUAD */
        public float xbar_rqst_slot;     /*! HMC_POWER_T: POWER FOR XBAR REQUEST SLOT */
        public float xbar_rsp_slot;      /*! HMC_POWER_T: POWER FOR XBAR RESPONSE SLOT */
        public float xbar_route_extern;  /*! HMC_POWER_T: POWER FOR ROUTED REQUEST TO EXTERNAL CUBE */
        public float vault_rqst_slot;    /*! HMC_POWER_T: POWER FOR VAULT REQUEST SLOT */
        public float vault_rsp_slot;     /*! HMC_POWER_T: POWER FOR VAULT RESPONSE SLOT */
        public float vault_ctrl;         /*! HMC_POWER_T: POWER FOR VAULT CONTROLLER PER ACTIVE CLOCK */
        public float row_access;         /*! HMC_POWER_T: POWER FOR ROW ACCESS */

        /* -- totals */
        public float t_link_phy;           /*! HMC_POWER_T: TOTAL POWER FOR EACH LINK PHY PER CLOCK */
        public float t_link_local_route;   /*! HMC_POWER_T: TOTAL POWER FOR LOCAL LINK ROUTE TO QUAD */
        public float t_link_remote_route;  /*! HMC_POWER_T: TOTAL POWER FOR REMOTE LINK ROUTE TO QUAD */
        public float t_xbar_rqst_slot;     /*! HMC_POWER_T: TOTAL POWER FOR XBAR REQUEST SLOT */
        public float t_xbar_rsp_slot;      /*! HMC_POWER_T: TOTAL POWER FOR XBAR RESPONSE SLOT */
        public float t_xbar_route_extern;  /*! HMC_POWER_T: TOTAL POWER FOR ROUTED REQUEST TO EXTERNAL CUBE */
        public float t_vault_rqst_slot;    /*! HMC_POWER_T: TOTAL POWER FOR VAULT REQUEST SLOT */
        public float t_vault_rsp_slot;     /*! HMC_POWER_T: TOTAL POWER FOR VAULT RESPONSE SLOT */
        public float t_vault_ctrl;         /*! HMC_POWER_T: TOTAL POWER FOR VAULT CONTROLLER PER ACTIVE CLOCK */
        public float t_row_access;         /*! HMC_POWER_T: TOTAL POWER FOR ROW ACCESS */

        /* -- output formats */
        public int tecplot;                /*! HMC_POWER_T: INDICATES TECPLOT OUTPUT FORMAT */
        public string prefix;          /*! HMC_POWER_T: TECPLOT FILE NAME PREFIX */

        /* -- tecplot output data */
        public HMC4LinkTec H4L=new HMC4LinkTec();     /*! HMC_POWER_T: 4Link Tecplot data */
        public HMC8LinkTec H8L=new HMC8LinkTec();     /*! HMC_POWER_T: 8Link Tecplot data */
        public hmc_power()
        {

        }
    }
}
