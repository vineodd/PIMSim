#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Procs;

#endregion

namespace PIMSim.General
{
    #region Public Callback Delegate

    /// <summary>
    /// Callback when memory read complete.
    /// </summary>
    /// <param name="block_addr">Target block address.</param>
    /// <param name="act_addr">Target actual address.</param>
    public delegate void ReadCallBack(CallBackInfo callback);

    /// <summary>
    /// Callback when memory write complete.
    /// </summary>
    /// <param name="block_addr">Target block address.</param>
    /// <param name="act_addr">Target actual address.</param>
    public delegate void WriteCallBack(CallBackInfo callback);

    #endregion
}
