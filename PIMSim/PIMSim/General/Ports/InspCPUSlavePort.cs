using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Protocols;
using PIMSim.Procs;
using System.Diagnostics;
using PortID = System.UInt16;


namespace PIMSim.General.Ports
{
    public class InspCPUSlavePort : SlavePort
    {
        public new InspCPUMasterPort _masterPort;

        public InspCPUSlavePort(string name, PortID id = PortID.MaxValue) : base(name,  id)
        {
            _masterPort = null;
        }
        ~InspCPUSlavePort() { }


        public void bind(ref InspCPUMasterPort master_port)
        {
            _baseMasterPort = master_port;
            _masterPort = master_port;
        }

        /**
         * Called by the master port to unbind. Should never be called
         * directly.
         */
        public override void unbind()
        {
            _baseMasterPort = null;
            _masterPort = null;
        }

      
        public new bool recvFunctionalResp(Packet pkt)
        {
            Debug.Assert(pkt.isRequest() && pkt.isResponse());
            pkt.ts_arrival = GlobalTimer.tick;
            return (owner as Proc).recvFunctionalResp(pkt);
        }
    }
}
