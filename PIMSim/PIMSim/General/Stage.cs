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
    /// <summary>
    /// Pipeline Stage Defination.
    /// </summary>
    public abstract class Stage
    {
        #region Public Variables

        /// <summary>
        /// inputs.
        /// </summary>
        public object input = null;

        /// <summary>
        /// outputs.
        /// </summary>
        public object output = null;

        /// <summary>
        /// intermid operands.
        /// </summary>
        public object intermid = null;

        //ready flags
        public bool input_ready = false;
        public bool output_ready = false;


        public bool stall = false;


        public Status status = Status.NoOP;
        /// <summary>
        /// Linked last pipeline stage.
        /// </summary>
        public List<Stage> last = new List<Stage>();

        /// <summary>
        /// Return delegate.
        /// </summary>
        public delegate void returnT();

        public object Parent;

        public int id = 0;

        #endregion

        #region Abstract Methods

        public abstract bool Step();
        public abstract void set_input(object obj);
        public abstract bool read_input();

        #endregion

        #region Public Methods

        public bool Try_Fetch => output_ready;

        public void write_output()
        {
            if (intermid != null)
            {
                output = intermid;
                output_ready = true;
                intermid = null;
            }
            else
            {
                output_ready = false;
            }

        }

        public bool get_output(ref object out_)
        {
            if (output_ready)
            {
                output_ready = false;
                out_ = output;
                output = null;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool get_output()
        {
            if (output_ready)
            {
                output_ready = false;
                output = null;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Set linked last pipeline stage.
        /// </summary>
        /// <param name="last_"></param>
        public void set_link(ref Stage last_)
        {
            last.Add(last_);
        }

        #endregion

    }


    public enum Status
    {
        NoOP,    //no load operations
        Outstanding,    //sent load operations
        Complete
    }

}
