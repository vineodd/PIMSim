using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.General
{
    public abstract class SimulatorObj
    {
        public UInt64 cycle = 0;
        /// <summary>
        /// NULL marks Invaild Data Or Blank Address.
        /// </summary>
        public static readonly UInt64 NULL = UInt64.MaxValue;
        public abstract void Step();
    }
}
