using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PortID = System.UInt16;
using Cycle = System.UInt64;

namespace PIMSim.General.Ports
{
    public static class PortManager
    {
        public static List<Port> ports = new List<Port>();
        public static PortID id = 0;
        /**
* Bind this master port to a slave port. This also does the
* mirror action and binds the slave port to the master port.
*/
        public static void bind(ref MasterPort msp, ref SlavePort slp)
        {



            // bind on the level of the base ports
            Debug.Assert(!ports.Contains(msp as Port));
            Debug.Assert(!ports.Contains(slp as Port));
            msp.bind(ref slp);
            slp.bind(ref msp);
            ports.Add(msp);
            ports.Add(slp);

        }
        public static void bind(ref InspCPUMasterPort msp, ref InspCPUSlavePort slp)
        {



            // bind on the level of the base ports
            Debug.Assert(!ports.Contains(msp as Port));
            Debug.Assert(!ports.Contains(slp as Port));
            msp.bind(ref slp);
            slp.bind(ref msp);
            ports.Add(msp);
            ports.Add(slp);

        }
        public static void bind(ref TraceFetcherMasterPorts msp, ref TraceFetcherSlavePort slp)
        {

            // bind on the level of the base ports
            Debug.Assert(!ports.Contains(msp as Port));
            Debug.Assert(!ports.Contains(slp as Port));
            msp.bind(ref slp);
            slp.bind(ref msp);
            ports.Add(msp);
            ports.Add(slp);

        }
        public static PortID Allocate()
        {
            id++;
            return (PortID)(id - 1);
        }
        /**
         * Unbind this master port and the associated slave port.
         */
        public static void unbind(Port port)
        {
            Debug.Assert(ports.Contains(port as MasterPort) || ports.Contains(port as SlavePort));

            if (port is MasterPort)
            {
                ports.Remove((port as MasterPort)._slavePort);
                ports.Remove(port as MasterPort);
            }
            else
            {
                ports.Remove((port as SlavePort)._masterPort);
                ports.Remove(port as SlavePort);
            }
        }
    }
}
