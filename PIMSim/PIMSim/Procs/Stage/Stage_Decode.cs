#region Refernence

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;

#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// Decode Stage
    /// </summary>
    public class Stage_Decode : Stage
    {
        #region Public Methods

        /// <summary>
        /// set input
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            input = (Instruction)obj;
            input_ready = true;
        }

        /// <summary>
        /// read input
        /// </summary>
        /// <returns></returns>
        public override bool read_input()
        {
            if (input_ready && input != null)
            {
                intermid = input;
                input_ready = false;
                input = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// internal step
        /// </summary>
        /// <returns></returns>
        public override bool Step()
        {
            read_input();
            write_output();
            return true;
        }

        #endregion
    }
}
