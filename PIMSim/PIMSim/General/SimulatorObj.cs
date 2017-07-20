#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace PIMSim.General
{
    public abstract class SimulatorObj
    {
        #region Public Variables

        public UInt64 cycle = 0;

        public string name = "";

        public int id = 0;

        #endregion

        #region Static Variables
        /// <summary>
        /// NULL marks Invaild Data Or Blank Address.
        /// </summary>
        public static readonly UInt64 NULL = UInt64.MaxValue;


        #endregion

        #region Abstract Methods
        public abstract void Step();

        #endregion
    }
}
