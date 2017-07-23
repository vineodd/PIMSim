#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.APIs;
using PIMSim.General.Protocols;

#endregion

namespace PIMSim.General
{
    public abstract class SimulatorObj: Packctable
    {
        #region Public Variables

        public UInt64 cycle = 0;

        public string name = "";

        public int id = 0;

        #endregion

        #region Static Variables
        /// <summary>
        /// NULL marks Invaild Data Or Blank Address.
        /// </summary>
        public static readonly UInt64 NULL = UInt64.MaxValue;


        #endregion

        #region Abstract Methods
        public abstract void Step();

        public virtual ulong sendAtomicSnoop(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual ulong recvAtomicSnoop(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendTimingSnoopResp(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvTimingSnoopResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual void recvTimingSnoopReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual void sendTimingSnoopReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendFunctionalSnoopResp(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvFunctionalSnoopResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual void recvFunctionalSnoopReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual void sendFunctionalSnoopReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendTimingReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvTimingReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendTimingResq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvTimingResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendFunctionalReq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvFunctionalReq(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool sendFunctionalResq(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual bool recvFunctionalResp(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual ulong sendAtomic(ref Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual ulong recvAtomic(Packet pkt)
        {
            throw new NotImplementedException();
        }

        public virtual void sendReqRetry()
        {
            throw new NotImplementedException();
        }

        public virtual void recvReqRetry()
        {
            throw new NotImplementedException();
        }

        public virtual void sendRetryResp()
        {
            throw new NotImplementedException();
        }

        public virtual void recvRetryResp()
        {
            throw new NotImplementedException();
        }

        public virtual void recvRangeChange()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
