#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.PIM;
#endregion

namespace SimplePIM.Procs
{
    /// <summary>
    /// Callback information class used by processors and pimunits
    /// </summary>
    public class CallBackInfo
    {
        #region Public Variables
        /// <summary>
        /// actual address
        /// </summary>
        public UInt64 address;

        /// <summary>
        /// block address used in cache
        /// </summary>
        public UInt64 block_addr;

        /// <summary>
        /// returned cycle
        /// </summary>
        public UInt64 done_cycle;

        /// <summary>
        /// pim operations
        /// </summary>
        public bool pim;

        /// <summary>
        /// used data
        /// </summary>
        public UInt64 data;

        /// <summary>
        /// target pid
        /// </summary>
        public List<int> pid = new List<int>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Function
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="blockaddr"></param>
        /// <param name="addcycle"></param>
        /// <param name="p"></param>
        /// <param name="pid_"></param>
        public CallBackInfo(UInt64 addr,UInt64 blockaddr,UInt64 addcycle,bool p,List<int> pid_)
        {
            address = addr;
            block_addr = blockaddr;
            done_cycle = addcycle;
            pim = p;
            pid = pid_;
        }
        public CallBackInfo()
        {

        }

        /// <summary>
        /// Return target units by id
        /// </summary>
        /// <returns>target cores or pimunits</returns>
        public object getsource()
        {
            if (pim)
            {
                var s = (PIMSimulator)typeof(Program).GetField("pimsim").GetValue("pimsim");
                List<ComputationalUnit> res = new List<ComputationalUnit>();
                foreach(var item in pid)
                    res.Add(s.pim.unit[item]);
                return res;
            }
            else
            {
                var s=(PIMSimulator) typeof(Program).GetField("pimsim").GetValue("pimsim");
                List<Proc> res = new List<Proc>();
                foreach (var item in pid)
                    res.Add(s.proc[item]);
                return res;
            }
        }

        #endregion

    }
}
