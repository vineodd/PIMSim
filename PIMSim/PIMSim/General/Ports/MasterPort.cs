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
    public class MasterPort : BaseMasterPort , Packctable
    {

        public SlavePort _slavePort;

        public MasterPort(string name, ref object owner, PortID _id = PortID.MaxValue) : base(name, ref owner, _id)
        {
            _slavePort = null;
        }
        ~MasterPort() { }


        /**
         * Bind this master port to a slave port. This also does the
         * mirror action and binds the slave port to the master port.
         */
        public void bind(ref SlavePort slave_port)
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

        /**
         * Send an atomic request packet, where the data is moved and the
         * state is updated in zero time, without interleaving with other
         * memory accesses.
         *
         * @param pkt Packet to send.
         *
         * @return Estimated latency of access.
         */
        public Cycle sendAtomic(Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            return _slavePort.recvAtomic(pkt);
        }

        /**
         * Send a functional request packet, where the data is instantly
         * updated everywhere in the memory system, without affecting the
         * current state of any block or moving the block.
         *
         * @param pkt Packet to send.
         */
        public void sendFunctional(Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
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
        public bool sendTimingReq(Packet pkt)
        {
            Debug.Assert(pkt.isRequest());
            return _slavePort.recvTimingReq(pkt);
        }

        /**
         * Attempt to send a timing snoop response packet to the slave
         * port by calling its corresponding receive function. If the send
         * does not succeed, as indicated by the return value, then the
         * sender must wait for a recvRetrySnoop at which point it can
         * re-issue a sendTimingSnoopResp.
         *
         * @param pkt Packet to send.
         */
        public bool sendTimingSnoopResp(Packet pkt)
        {
            Debug.Assert(pkt.isResponse());
            return _slavePort.recvTimingSnoopResp(pkt);
        }

        /**
         * Send a retry to the slave port that previously attempted a
         * sendTimingResp to this master port and failed. Note that this
         * is virtual so that the "fake" snoop response port in the
         * coherent crossbar can override the behaviour.
         */
        public virtual void sendRetryResp()
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
        public virtual bool isSnooping() { return false; }

        /**
         * Get the address ranges of the connected slave port.
         */
        public AddrRangeList getAddrRanges()
        {
            return _slavePort.getAddrRanges();
        }

        /** Inject a PrintReq for the given address to print the state of
         * that address throughout the memory system.  For debugging.
         */
        public void printAddr(Address a)
        {
          //  Request req = new Request(a, 1, new Flags(0), Request.MasterID.funcMasterId);
        //    Packet pkt = new Packet(ref req, new MemCmd(MemCmd.Command.PrintReq));
            //    Packet.PrintReqState prs=new Packet.PrintReqState(ref );
            //  pkt.senderState = &prs;

          //  sendFunctional(pkt);
        }



        /**
         * Receive an atomic snoop request packet from the slave port.
         */
        public virtual Cycle recvAtomicSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting an atomic snoop request\n", name()));
            return 0;
        }

        /**
         * Receive a functional snoop request packet from the slave port.
         */
        public virtual void recvFunctionalSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a functional snoop request\n", name()));
        }

        /**
         * Receive a timing response from the slave port.
         */
        public virtual bool recvTimingResp(Packet pkt)
        {
            return false;
        }

        /**
         * Receive a timing snoop request from the slave port.
         */
        public virtual void recvTimingSnoopReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a timing snoop request\n", name()));
        }

        /**
         * Called by the slave port if sendTimingReq was called on this
         * master port (causing recvTimingReq to be called on the slave
         * port) and was unsuccesful.
         */
        public virtual void recvReqRetry()
        {

        }

        /**
         * Called by the slave port if sendTimingSnoopResp was called on this
         * master port (causing recvTimingSnoopResp to be called on the slave
         * port) and was unsuccesful.
         */
        public virtual void recvRetrySnoopResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a snoop retry\n", name()));
        }

        /**
         * Called to receive an address range change from the peer slave
         * port. The default implementation ignores the change and does
         * nothing. Override this function in a derived class if the owner
         * needs to be aware of the address ranges, e.g. in an
         * interconnect component like a bus.
         */
        public virtual void recvRangeChange() { }

        public ulong sendAtomicSnoop(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool sendTimingSnoopResp(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvTimingSnoopResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public void sendTimingSnoopReq(ref Packet pkt)
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

        public bool sendTimingReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public bool recvTimingReq(Packet pkt)
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

        public ulong sendAtomic(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public ulong recvAtomic(Packet pkt)
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
