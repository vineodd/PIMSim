using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PIMSim.General.Protocols;
using PIMSim.APIs;
using PIMSim.Partitioner;
using PortID = System.UInt16;
using Cycle = System.UInt64;
using Address = System.UInt64;
using AddrRangeList = System.Collections.Generic.List<PIMSim.General.AddressRange>;

namespace PIMSim.General.Ports
{
    public class TraceFetcherSlavePort :SlavePort
    {
        public new TraceFetcherMasterPorts _masterPort;

        public TraceFetcherSlavePort(string name, PortID id = PortID.MaxValue) : base(name,  id)
        {
            _masterPort = null;
        }
        ~TraceFetcherSlavePort() { }



        public void bind(ref TraceFetcherMasterPorts master_port)
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


        public bool sendTimingReq(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest() && pkt.isRead());
            pkt.ts_departure = GlobalTimer.tick;
            _masterPort.addPacket(pkt);
            return true;
        }
        public bool sendFunctionalReq(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest() && pkt.isRead());
            pkt.ts_departure = GlobalTimer.tick;
            return _masterPort.recvFunctionalReq(pkt);
        }

        public new bool recvFunctionalResp(Packet pkt)
        {
            Debug.Assert(pkt.isRequest() && pkt.isResponse());
            pkt.ts_arrival= GlobalTimer.tick;
            return (owner as InsPartition).recvFunctionalResp(pkt);
        }
    }
}
