using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.PIM
{
    public enum Consistency
    {
        SpinLock,
        Regs,
        NoCache,
        DontCheck
    }
}
