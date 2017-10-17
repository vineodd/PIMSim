#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PIMSim.General
{
    /// <summary>
    /// Abstract requests class
    /// </summary>
    public abstract class Request
    {
        #region Public Variables

        public UInt64 ts_arrival = 0;
        public UInt64 ts_departure = 0;
        public UInt64 ts_issue = 0;

        #endregion
    }
}
