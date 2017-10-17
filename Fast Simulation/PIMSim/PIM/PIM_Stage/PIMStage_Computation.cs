#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;

#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// PIM pipeline stage : computation
    /// </summary>
    public class PIMStage_Computation : Stage
    {
        #region Private Variables
        /// <summary>
        /// computing latency
        /// </summary>
        private Counter latency;
        #endregion

        #region Public Methods
        /// <summary>
        /// read input of all last stage
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            if (last != null && last.Count > 0)
            {
                bool all_ready = true;
                foreach (var i in last)
                {
                    all_ready = i.Try_Fetch && all_ready;
                }
                // if all last stage ready, start to work
                if (all_ready)
                {
                    foreach (var item in last)
                        item.get_output();
                    input_ready = true;
                }
                else
                {
                    input_ready = false;
                }

            }
        }

        /// <summary>
        /// internal step
        /// </summary>
        /// <returns></returns>
        public override bool Step()
        {
            stall = false;
            set_input(null);
            if (read_input())
            {
                latency.WaitOne();
                if (latency.Zero)
                {
                    latency.Reset();

                    write_output();
                    return true;
                }
                stall = true;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Construction Function
        /// </summary>
        /// <param name="parent">parent cu</param>
        /// <param name="id_">id</param>
        /// <param name="lat">latency</param>
        public PIMStage_Computation(object parent, int id_, int i = 0)
        {
            id = id_;
            latency = new Counter(i, i);
            Parent = parent;
        }

        /// <summary>
        /// read input
        /// </summary>
        /// <returns></returns>
        public override bool read_input()
        {
            if (input_ready)
            {
                input_ready = false;
                intermid = 0;
                return true;
            }
            return false;
        }
        #endregion

    }
}
