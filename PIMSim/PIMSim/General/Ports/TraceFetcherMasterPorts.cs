using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Protocols;
using System.Diagnostics;
using PIMSim.APIs;
using PIMSim.TraceReader;
using PortID = System.UInt16;
using Cycle = System.UInt64;
using Address = System.UInt64;
using AddrRangeList = System.Collections.Generic.List<PIMSim.General.AddressRange>;

namespace PIMSim.General.Ports
{
    public class TraceFetcherMasterPorts :  MasterPort
    {
        public new TraceFetcherSlavePort _slavePort;

        public TraceFetcherMasterPorts(string name,  PortID _id = PortID.MaxValue) : base(name,  _id)
        {
            _slavePort = null;
        }
        ~TraceFetcherMasterPorts() { }

        public void bind(ref TraceFetcherSlavePort slave_port)
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
            return (owner as TraceFetcher).recvFunctionalReq(pkt);
        }



    }
}
