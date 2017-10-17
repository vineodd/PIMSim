using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PortID = System.UInt16;
namespace PIMSim.General.Ports
{
    public class BaseMasterPort : Port

    {

        public BaseSlavePort _baseSlavePort;

        public BaseMasterPort(string name, PortID _id = PortID.MaxValue) : base(name, _id)
        {
            _baseSlavePort = null;
        }
        ~BaseMasterPort() { }


        public virtual void bind(ref BaseSlavePort slave_port) { }
        public override void unbind() { }
        public BaseSlavePort getSlavePort()
        {

            if (_baseSlavePort == null)
                Debug.Fail("Cannot getSlavePort on master port {0} that is not connected", name());

            return _baseSlavePort;
        }

        public bool isConnected()
        {
            return _baseSlavePort != null;
        }

    }
}
