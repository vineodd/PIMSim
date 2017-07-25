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
        Cycle sendAtomicSnoop(PacketSource source, ref Packet pkt);
        Cycle recvAtomicSnoop(Packet pkt);

        bool sendTimingSnoopResp(PacketSource source, ref Packet pkt);
        bool recvTimingSnoopResp(Packet pkt);
        void recvTimingSnoopReq(Packet pkt);
        void sendTimingSnoopReq(PacketSource source, ref Packet pkt);

        bool sendFunctionalSnoopResp(PacketSource source, ref Packet pkt);
        bool recvFunctionalSnoopResp(Packet pkt);
        void recvFunctionalSnoopReq(Packet pkt);
        void sendFunctionalSnoopReq(PacketSource source, ref Packet pkt);

        bool sendTimingReq(PacketSource source, ref Packet pkt);
        bool recvTimingReq(Packet pkt);
        bool sendTimingResq(PacketSource source, ref Packet pkt);
        bool recvTimingResp(Packet pkt);

        bool sendFunctionalReq(PacketSource source, ref Packet pkt);
        bool recvFunctionalReq(Packet pkt);
        bool sendFunctionalResq(PacketSource source, ref Packet pkt);
        bool recvFunctionalResp(Packet pkt);

        Cycle sendAtomic(PacketSource source, ref Packet pkt);
        Cycle recvAtomic(Packet pkt);

        void sendReqRetry();
        void recvReqRetry();
        void sendRetryResp();
        void recvRetryResp();

        void recvRangeChange();


    }
}
