using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PIMSim.General.Protocols;
using PIMSim.APIs;
using PortID = System.UInt16;
using Cycle = System.UInt64;
using Address = System.UInt64;
using AddrRangeList = System.Collections.Generic.List<PIMSim.General.AddressRange>;

namespace PIMSim.General.Ports
{
    public class TraceFetcherSlavePort :SlavePort
    {
        public new TraceFetcherMasterPorts _masterPort;

        public TraceFetcherSlavePort(string name, ref Object owner, PortID id = PortID.MaxValue) : base(name, ref owner, id)
        {
            _masterPort = null;
        }
        ~TraceFetcherSlavePort() { }


    

        /**
         * Attempt to send a timing response to the master port by calling
         * its corresponding receive function. If the send does not
         * succeed, as indicated by the return value, then the sender must
         * wait for a recvRespRetry at which point it can re-issue a
         * sendTimingResp.
         *
         * @param pkt Packet to send.
         *
         * @return If the send was succesful or not.
        */
        public bool sendTimingResp(ref Packet pkt)
        {
            Debug.Assert(pkt.isResponse());
            return _masterPort.recvTimingResp(pkt);
        }



        /**
         * Send a retry to the master port that previously attempted a
         * sendTimingReq to this slave port and failed.
         */
        public void sendRetryReq()
        {
            _masterPort.recvReqRetry();
        }



        /**
         * Find out if the peer master port is snooping or not.
         *
         * @return true if the peer master port is snooping
         */
        public bool isSnooping() { return _masterPort.isSnooping(); }

        /**
         * Called by the owner to send a range change
         */
       public void sendRangeChange()
        {
            if (_masterPort == null)
                Debug.Fail(String.Format("{0} cannot sendRangeChange() without master port", name()));
            _masterPort.recvRangeChange();
        }

        /**
         * Get a list of the non-overlapping address ranges the owner is
         * responsible for. All slave ports must override this function
         * and return a populated list with at least one item.
         *
         * @return a list of ranges responded to
         */
        public new AddrRangeList getAddrRanges() { return null; }



        /**
         * Called by the master port to unbind. Should never be called
         * directly.
         */
        public override void unbind()
        {
            _baseMasterPort = null;
            _masterPort = null;
        }

        /**
         * Called by the master port to bind. Should never be called
         * directly.
         */
        public void bind(ref TraceFetcherMasterPorts master_port)
        {
            _baseMasterPort = master_port;
            _masterPort = master_port;
        }



        /**
         * Receive a functional request packet from the master port.
         */
        public void recvFunctionalReq(Packet pkt) { }

        /**
         * Receive a timing request from the master port.
         */
        public new bool recvTimingReq(Packet pkt) { return false; }



        /**
         * Called by the master port if sendTimingResp was called on this
         * slave port (causing recvTimingResp to be called on the master
         * port) and was unsuccesful.
         */
        public new void recvRespRetry() { }


    }
}
