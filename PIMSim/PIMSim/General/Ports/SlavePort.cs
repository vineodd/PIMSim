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

using AddrRangeList = System.Collections.Generic.List<PIMSim.General.AddressRange>;

namespace PIMSim.General.Ports
{
    public class SlavePort : BaseSlavePort, Packctable
    {

        public MasterPort _masterPort;

        public SlavePort(string name, ref Object owner, PortID id = PortID.MaxValue) : base(name, ref owner, id)
        {
            _masterPort = null;
        }
        ~SlavePort() { }

        /**
         * Send an atomic snoop request packet, where the data is moved
         * and the state is updated in zero time, without interleaving
         * with other memory accesses.
         *
         * @param pkt Snoop packet to send.
         *
         * @return Estimated latency of access.
         */
        Cycle sendAtomicSnoop(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            return _masterPort.recvAtomicSnoop(pkt);
        }

        /**
         * Send a functional snoop request packet, where the data is
         * instantly updated everywhere in the memory system, without
         * affecting the current state of any block or moving the block.
         *
         * @param pkt Snoop packet to send.
         */
        void sendFunctionalSnoop(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            _masterPort.recvFunctionalSnoop(pkt);
        }

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
        bool sendTimingResp(ref Packet pkt)
        {
            Debug.Assert(pkt.isResponse());
            return _masterPort.recvTimingResp(pkt);
        }

        /**
         * Attempt to send a timing snoop request packet to the master port
         * by calling its corresponding receive function. Snoop requests
         * always succeed and hence no return value is needed.
         *
         * @param pkt Packet to send.
         */
        void sendTimingSnoopReq(ref Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            _masterPort.recvTimingSnoopReq(pkt);
        }

        /**
         * Send a retry to the master port that previously attempted a
         * sendTimingReq to this slave port and failed.
         */
        void sendRetryReq()
        {
            _masterPort.recvReqRetry();
        }

        /**
         * Send a retry to the master port that previously attempted a
         * sendTimingSnoopResp to this slave port and failed.
         */
        void sendRetrySnoopResp()
        {
            _masterPort.recvRetrySnoopResp();
        }

        /**
         * Find out if the peer master port is snooping or not.
         *
         * @return true if the peer master port is snooping
         */
        bool isSnooping() { return _masterPort.isSnooping(); }

        /**
         * Called by the owner to send a range change
         */
        void sendRangeChange()
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
        public virtual AddrRangeList getAddrRanges() { return null; }



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
        public void bind(ref MasterPort master_port)
        {
            _baseMasterPort = master_port;
            _masterPort = master_port;
        }

        /**
         * Receive an atomic request packet from the master port.
         */
        public virtual Cycle recvAtomic(Packet pkt) { return Cycle.MaxValue; }

        /**
         * Receive a functional request packet from the master port.
         */
        public virtual void recvFunctional(Packet pkt) { }

        /**
         * Receive a timing request from the master port.
         */
        public virtual bool recvTimingReq(Packet pkt) { return false; }

        /**
         * Receive a timing snoop response from the master port.
         */
        public virtual bool recvTimingSnoopResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a timing snoop response\n", name()));
            return false;
        }

        /**
         * Called by the master port if sendTimingResp was called on this
         * slave port (causing recvTimingResp to be called on the master
         * port) and was unsuccesful.
         */
        public virtual void recvRespRetry() { }

        ulong Packctable.sendAtomicSnoop(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public ulong recvAtomicSnoop(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void sendFunctional(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendTimingSnoopResp(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void recvFunctionalSnoop(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendTimingReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvTimingResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public ulong sendAtomic(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void sendRetryResp()
        {
            throw new NotImplementedException();
        }

        public void recvReqRetry()
        {
            throw new NotImplementedException();
        }

        public void recvTimingSnoopReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void recvRetrySnoopResp()
        {
            throw new NotImplementedException();
        }

        public void recvRangeChange()
        {
            throw new NotImplementedException();
        }

        void Packctable.sendTimingSnoopReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendFunctionalSnoopResp(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvFunctionalSnoopResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void recvFunctionalSnoopReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void sendFunctionalSnoopReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendTimingResq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendFunctionalReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvFunctionalReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendFunctionalResq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvFunctionalResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void sendReqRetry()
        {
            throw new NotImplementedException();
        }

        public void recvRetryResp()
        {
            throw new NotImplementedException();
        }
    }
}
