using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.General
{
    public abstract class InputType
    {
        public static readonly UInt64 NULL = UInt64.MaxValue;
        public UInt64 cycle;
    }
}
