#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.Procs;

#endregion

namespace SimplePIM.Memory
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
        /// Attach MTRL to get input requests
        /// </summary>
        /// <param name="mctrl_">attached MTRL</param>
        public abstract void attach_mctrl(ref Mctrl mctrl_);

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

        #endregion
    }
}
