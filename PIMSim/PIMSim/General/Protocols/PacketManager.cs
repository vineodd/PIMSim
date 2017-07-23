using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacketID = System.UInt64;

namespace PIMSim.General.Protocols
{
    public static class PacketManager
    {
        public static PacketID id = 0;

        public static PacketID Allocate()
        {
            id++;
            return (PacketID)(id - 1);
        }
        public static void Collect(Packet pkt)
        {

        }
    }
}
