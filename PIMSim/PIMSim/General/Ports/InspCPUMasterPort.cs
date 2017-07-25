using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Protocols;
using System.Diagnostics;
using PIMSim.Partitioner;
using PortID = System.UInt16;
namespace PIMSim.General.Ports
{
    public class InspCPUMasterPort : MasterPort
    {
        public InspCPUMasterPort(string _name, PortID _id = PortID.MaxValue) : base(_name, _id)
        {
            _slavePort = null;
        }
        public new InspCPUSlavePort _slavePort;
        ~InspCPUMasterPort() { }
        public void bind(ref InspCPUSlavePort slave_port)
        {
            _baseSlavePort = slave_port;
            _slavePort = slave_port;
        }
        /**
         * Unbind this master port and the associated slave port.
         */
        public override void unbind()
        {
            _baseSlavePort = null;
            _slavePort = null;
        }

        public new bool recvFunctionalReq(Packet pkt)
        {
            Debug.Assert(owner != null);
            Debug.Assert(pkt.isRead() && pkt.isRequest());
            return (owner as InsPartition).recvFunctionalReq(pkt);
        }
        public new bool recvTimingReq(Packet pkt)
        {
            Debug.Assert(owner != null);
            Debug.Assert(pkt.isRead() && pkt.isRequest());
            addPacket(pkt);
            return true;
        }

        public new bool sendFunctionalResq(ref Packet pkt)
        {
            Debug.Assert(owner != null);
            Debug.Assert(pkt.hasData() && pkt.isRequest());
            pkt.ts_departure = GlobalTimer.tick;
            return _slavePort.recvFunctionalResp(pkt);
        }

        public new bool sendTimingResq(ref Packet pkt)
        {
            Debug.Assert(owner != null);
            Debug.Assert(pkt.hasData() && pkt.isRequest());
            pkt.ts_departure = GlobalTimer.tick;
            return _slavePort.recvTimingResp(pkt);
        }

    }
}
