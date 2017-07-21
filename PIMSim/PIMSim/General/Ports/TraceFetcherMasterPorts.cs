using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Protocols;
using System.Diagnostics;
using PIMSim.APIs;
using PortID = System.UInt16;
using Cycle = System.UInt64;
using Address = System.UInt64;
using AddrRangeList = System.Collections.Generic.List<PIMSim.General.AddressRange>;

namespace PIMSim.General.Ports
{
    public class TraceFetcherMasterPorts :  MasterPort
    {
        public new TraceFetcherSlavePort _slavePort;

        public TraceFetcherMasterPorts(string name, ref object owner, PortID _id = PortID.MaxValue) : base(name, ref owner, _id)
        {
            _slavePort = null;
        }
        ~TraceFetcherMasterPorts() { }


        /**
         * Bind this master port to a slave port. This also does the
         * mirror action and binds the slave port to the master port.
         */
        //public void bind(ref TraceFetcherSlavePort slave_port)
        //{
        //    _baseSlavePort = slave_port;
        //    _slavePort = slave_port;
        //}

        /**
         * Unbind this master port and the associated slave port.
         */
        public override void unbind()
        {
            _baseSlavePort = null;
            _slavePort = null;
        }


        /**
         * Send a functional request packet, where the data is instantly
         * updated everywhere in the memory system, without affecting the
         * current state of any block or moving the block.
         *
         * @param pkt Packet to send.
         */
        public void sendFunctionalReq(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            pkt.ts_departure = GlobalTimer.tick;
            _slavePort.recvFunctional(pkt);
        }

        /**
         * Attempt to send a timing request to the slave port by calling
         * its corresponding receive function. If the send does not
         * succeed, as indicated by the return value, then the sender must
         * wait for a recvReqRetry at which point it can re-issue a
         * sendTimingReq.
         *
         * @param pkt Packet to send.
         *
         * @return If the send was succesful or not.
        */
        public bool sendTimingReq(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            pkt.ts_departure = GlobalTimer.tick;
            return _slavePort.recvTimingReq(pkt);
        }



        /**
         * Send a retry to the slave port that previously attempted a
         * sendTimingResp to this master port and failed. Note that this
         * is virtual so that the "fake" snoop response port in the
         * coherent crossbar can override the behaviour.
         */
        public new void sendRetryResp()
        {
            _slavePort.recvRespRetry();
        }

        /**
         * Determine if this master port is snooping or not. The default
         * implementation returns false and thus tells the neighbour we
         * are not snooping. Any master port that wants to receive snoop
         * requests (e.g. a cache connected to a bus) has to override this
         * function.
         *
         * @return true if the port should be considered a snooper
         */
        public new  bool isSnooping() { return false; }

        /**
         * Get the address ranges of the connected slave port.
         */
        public new AddrRangeList getAddrRanges()
        {
            return _slavePort.getAddrRanges();
        }


        /**
         * Receive a timing response from the slave port.
         */
        public new bool recvTimingResp(Packet pkt)
        {
            return false;
        }

        /**
         * Receive a timing snoop request from the slave port.
         */
        public new void recvTimingSnoopReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a timing snoop request\n", name()));
        }

        /**
         * Called by the slave port if sendTimingReq was called on this
         * master port (causing recvTimingReq to be called on the slave
         * port) and was unsuccesful.
         */
        public new void recvReqRetry()
        {

        }


        /**
         * Called to receive an address range change from the peer slave
         * port. The default implementation ignores the change and does
         * nothing. Override this function in a derived class if the owner
         * needs to be aware of the address ranges, e.g. in an
         * interconnect component like a bus.
         */
        public new void recvRangeChange() { }


        public void recvFunctionalResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public ulong recvAtomic(Packet pkt)
        {
            throw new NotImplementedException();
        }
    }
}
