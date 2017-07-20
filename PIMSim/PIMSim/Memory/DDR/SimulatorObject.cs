using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.DDR
{
    public class DRAMSimObject
    {
        public UInt64 currentClockCycle;

        public void step() { currentClockCycle++; }
        public virtual void update() { }

    }
}
