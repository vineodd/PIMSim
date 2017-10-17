#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Procs;

#endregion

namespace PIMSim.Memory
{
    public abstract class MemObject :SimulatorObj
    {
        #region Public Variables

        public int pid;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Add a memory operation to memory object
        /// </summary>
        /// <param name="req">requests</param>
        /// <returns></returns>
        public abstract bool addTransation(MemRequest req);


        /// <summary>
        /// function to set lock
        /// </summary>
        /// <param name="addr">target address</param>
        /// <returns></returns>
        public abstract int get_lock_index(UInt64 addr);

        /// <summary>
        /// Callback when memory operations done.
        /// This Function moved to PROC\CALLBACK.
        /// </summary>
        /// <param name="proc_"></param>
        //public abstract void attach_proc_return(ref List<Proc> proc_);

        public abstract bool done();
        #endregion
    }
}
