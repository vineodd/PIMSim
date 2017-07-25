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

        public SlavePort(string name, PortID id = PortID.MaxValue) : base(name, id)
        {
            _masterPort = null;
        }
        ~SlavePort() { }



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
         * Called by the master port if sendTimingResp was called on this
         * slave port (causing recvTimingResp to be called on the master
         * port) and was unsuccesful.
         */
        public Cycle recvAtomicSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting an recvAtomicSnoop request\n", name()));
            return 0;
        }

        /**
         * Receive a functional snoop request packet from the slave port.
         */
        public void recvFunctionalSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalSnoop request\n", name()));
        }

        /**
         * Receive a timing response from the slave port.
         */
        public bool recvTimingResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvTimingResp request\n", name()));
            return false;
        }

        /**
         * Receive a timing snoop request from the slave port.
         */
        public void recvTimingSnoopReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvTimingSnoopReq request\n", name()));
        }

        /**
         * Called by the slave port if sendTimingReq was called on this
         * master port (causing recvTimingReq to be called on the slave
         * port) and was unsuccesful.
         */
        public void recvReqRetry()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvReqRetry request\n", name()));
        }

        /**
         * Called by the slave port if sendTimingSnoopResp was called on this
         * master port (causing recvTimingSnoopResp to be called on the slave
         * port) and was unsuccesful.
         */
        public void recvRetrySnoopResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvRetrySnoopResp request\n", name()));
        }

        /**
         * Called to receive an address range change from the peer slave
         * port. The default implementation ignores the change and does
         * nothing. Override this function in a derived class if the owner
         * needs to be aware of the address ranges, e.g. in an
         * interconnect component like a bus.
         */
        public void recvRangeChange()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvRangeChange request\n", name()));
        }

        public ulong sendAtomicSnoop(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendAtomicSnoop request\n", name()));
            return 0;
        }

        public bool sendTimingSnoopResp(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingSnoopResp request\n", name()));
            return false;
        }

        public bool recvTimingSnoopResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvTimingSnoopResp request\n", name()));
            return false;
        }

        public void sendTimingSnoopReq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingSnoopReq request\n", name()));

        }

        public bool sendFunctionalSnoopResp(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendFunctionalSnoopResp request\n", name()));
            return false;
        }

        public bool recvFunctionalSnoopResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalSnoopResp request\n", name()));
            return false;
        }

        public void recvFunctionalSnoopReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalSnoopReq request\n", name()));
        }

        public void sendFunctionalSnoopReq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendFunctionalSnoopReq request\n", name()));
        }

        public bool sendTimingReq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingReq request\n", name()));
            return false;
        }

        public bool recvTimingReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvTimingReq request\n", name()));
            return false;
        }

        public bool sendTimingResq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingResq request\n", name()));
            return false;
        }

        public bool sendFunctionalReq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendFunctionalReq request\n", name()));
            return false;
        }

        public bool recvFunctionalReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalReq request\n", name()));
            return false;
        }

        public bool sendFunctionalResq(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendFunctionalResq request\n", name()));
            return false;
        }

        public bool recvFunctionalResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalResp request\n", name()));
            return false;
        }

        public ulong sendAtomic(PacketSource source, ref Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendAtomic request\n", name()));
            return 0;
        }

        public ulong recvAtomic(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvAtomic request\n", name()));
            return 0;
        }

        public void sendReqRetry()
        {
            Debug.Fail(String.Format("{0} was not expecting a sendReqRetry request\n", name()));
        }

        public void recvRetryResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvRetryResp request\n", name()));
        }

        public void sendRetryResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a sendRetryResp request\n", name()));
        }
    }
}
