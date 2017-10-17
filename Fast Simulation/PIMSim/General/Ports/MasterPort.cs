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

        public MasterPort(string name,  PortID _id = PortID.MaxValue) : base(name,  _id)
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

        public Cycle sendAtomic(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendAtomic request\n", name()));
            return 0;
        }

        public void sendFunctional(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendFunctional request\n", name()));

        }


        public bool sendTimingReq(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingReq request\n", name()));
            return false;
        }


        public bool sendTimingSnoopResp(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a sendTimingSnoopResp request\n", name()));
            return false;
        }


        public void sendRetryResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a sendRetryResp request\n", name()));
        }


        public virtual bool isSnooping() { return false; }

        public AddrRangeList getAddrRanges()
        {
            return _slavePort.getAddrRanges();
        }

        public void printAddr(Address a)
        {

        }

        public Cycle recvAtomicSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting an recvAtomicSnoop request\n", name()));
            return 0;
        }

        /**
         * Receive a functional snoop request packet from the slave port.
         */
        public  void recvFunctionalSnoop(Packet pkt)
        {
            Debug.Fail(String.Format("{0} was not expecting a recvFunctionalSnoop request\n", name()));
        }

        /**
         * Receive a timing response from the slave port.
         */
        public  bool recvTimingResp(Packet pkt)
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


        public void recvReqRetry()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvReqRetry request\n", name()));
        }


        public void recvRetrySnoopResp()
        {
            Debug.Fail(String.Format("{0} was not expecting a recvRetrySnoopResp request\n", name()));
        }

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
    }
}
