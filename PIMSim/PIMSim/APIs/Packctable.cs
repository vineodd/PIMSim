using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Protocols;
using Cycle = System.UInt64;
namespace PIMSim.APIs
{
    public interface Packctable
    {
        Cycle sendAtomicSnoop(ref Packet pkt);
        Cycle recvAtomicSnoop(Packet pkt);

        bool sendTimingSnoopResp(ref Packet pkt);
        bool recvTimingSnoopResp(Packet pkt);
        void recvTimingSnoopReq(Packet pkt);
        void sendTimingSnoopReq(ref Packet pkt);

        bool sendFunctionalSnoopResp(ref Packet pkt);
        bool recvFunctionalSnoopResp(Packet pkt);
        void recvFunctionalSnoopReq(Packet pkt);
        void sendFunctionalSnoopReq(ref Packet pkt);

        bool sendTimingReq(ref Packet pkt);
        bool recvTimingReq(Packet pkt);
        bool sendTimingResq(ref Packet pkt);
        bool recvTimingResp(Packet pkt);

        bool sendFunctionalReq(ref Packet pkt);
        bool recvFunctionalReq(Packet pkt);
        bool sendFunctionalResq(ref Packet pkt);
        bool recvFunctionalResp(Packet pkt);

        Cycle sendAtomic(ref Packet pkt);
        Cycle recvAtomic(Packet pkt);

        void sendReqRetry();
        void recvReqRetry();
        void sendRetryResp();
        void recvRetryResp();

        void recvRangeChange();


    }
}
