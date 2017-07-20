#region Reference
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
    /// Write data back to cache or registers
    /// </summary>
    public class Stage_Writeback : Stage
    {
        #region Public Methods
        /// <summary>
        /// set input
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            if (last != null)
            {
                if (last[0].output_ready)
                {
                    var obj_ = new Instruction() as object;
                    last[0].get_output(ref obj_);
                    input = obj_ as Instruction;
                    input_ready = true;
                }
            }
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
            //add code to load data
            set_input(null);
            read_input();
            write_output();
            return true;
        }
        #endregion

    }
}
