#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
#endregion

namespace PIMSim.Statistics
{
    /// <summary>
    /// Debug Class 
    /// </summary>
    public static class DEBUG
    {
        #region Private Variables
        /// <summary>
        /// overall streamwriter
        /// </summary>
        private static StreamWriter sw = null;
        #endregion

        #region Public Methods
        /// <summary>
        /// set streamwriter
        /// </summary>
        /// <param name="sw_"></param>
        public static void set_writer(ref StreamWriter sw_)
        {
            sw = sw_;
        }

        /// <summary>
        /// output
        /// </summary>
        /// <param name="s"></param>
        public static void WriteLine(string s="")
        {
            if (sw == null)
            {
                Console.WriteLine( s);
            }
            else
            {
                sw.WriteLine( s);
            }
        }

        /// <summary>
        /// output
        /// </summary>
        /// <param name="s"></param>
        public static void Write(string s)
        {
            if (sw == null)
            {
                Console.Write(s);
            }
            else
            {
                sw.Write(s);
            }
        }

        /// <summary>
        /// assert 
        /// </summary>
        /// <param name="s"></param>
        public static void Assert(bool s)
        {
            System.Diagnostics.Debug.Assert(s);
        }

        /// <summary>
        /// print error
        /// </summary>
        /// <param name="s"></param>
        public static void Error(string s)
        {
            if (sw == null)
            {
                Console.WriteLine("ERROR: " + s);
            }
            else
            {
                sw.WriteLine("ERROR: " + s);
            }
        }
        #endregion
    }
}
