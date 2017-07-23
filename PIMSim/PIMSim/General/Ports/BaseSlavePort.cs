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
    public class BaseSlavePort : Port
    {
        public BaseMasterPort _baseMasterPort;

        public BaseSlavePort(string name, PortID _id = PortID.MaxValue) : base(name,  _id)
        {
            _baseMasterPort = null;
        }
        ~BaseSlavePort() { }

        public virtual void bind(ref BaseMasterPort master_port) { }
        public override void unbind() { }


        public BaseMasterPort getMasterPort()
        {
            if (_baseMasterPort == null)
                Debug.Fail("Cannot getMasterPort on slave port {0} that is not connected\n", name());

            return _baseMasterPort;
        }
        public bool isConnected()
        {
            return _baseMasterPort != null;
        }


    }
}
