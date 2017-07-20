#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// Consistency Model
    /// </summary>
    public enum Consistency
    {
        SpinLock,
        NoCache,
        DontCheck
    }
}
