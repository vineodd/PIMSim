using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class HMC4LinkTec
    {
      public  float[] vault_rsp_power=new float[32];
        public float[] vault_rqst_power = new float[32];
        public float[] vault_ctrl_power = new float[32];
        public float[] xbar_rqst_power = new float[4];
        public float[] xbar_rsp_power = new float[4];
        public float[] xbar_route_extern_power = new float[4];
        public float[] link_local_route_power = new float[4];
        public float[] link_remote_route_power = new float[4];
        public float[] link_phy_power = new float[4];
        public float row_access_power;

        // thermal
        public float[] vault_rsp_btu = new float[32];
        public float[] vault_rqst_btu = new float[32];
        public float[] vault_ctrl_btu = new float[32];
        public float []xbar_rqst_btu = new float[4];
        public float []xbar_rsp_btu = new float[4];
        public float[] xbar_route_extern_btu = new float[4];
        public float []link_local_route_btu = new float[4];
        public float []link_remote_route_btu = new float[4];
        public float []link_phy_btu = new float[4];
        public float row_access_btu;
    }

    public class HMC8LinkTec
    {
        public float[] vault_rsp_power = new float[32];
        public float[] vault_rqst_power = new float[32];
        public float[] vault_ctrl_power = new float[32];
        public float[] xbar_rqst_power = new float[8];
        public float[] xbar_rsp_power = new float[8];
        public float[] xbar_route_extern_power = new float[8];
        public float[] link_local_route_power = new float[8];
        public float[] link_remote_route_power = new float[8];
        public float[] link_phy_power = new float[8];
        public float row_access_power;

        // thermal
        public float[] vault_rsp_btu = new float[32];
        public float[] vault_rqst_btu = new float[32];
        public float[] vault_ctrl_btu = new float[32];
        public float[] xbar_rqst_btu = new float[8];
        public float[] xbar_rsp_btu = new float[8];
        public float[] xbar_route_extern_btu = new float[8];
        public float[] link_local_route_btu = new float[8];
        public float[] link_remote_route_btu = new float[8];
        public float[] link_phy_btu = new float[8];
        public float row_access_btu;
    }
}
