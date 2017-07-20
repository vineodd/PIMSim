#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PIMSim.PIM
{

    public interface PIMInterface
    {
        #region Interface
        /// <summary>
        /// Stage read its input port and get input data
        /// </summary>
        void get_input();

        /// <summary>
        /// get output of this stage
        /// </summary>
        /// <returns></returns>
        object get_output();

        /// <summary>
        /// another implement
        /// </summary>
        /// <param name="output"></param>
        void get_output(ref object output);
        #endregion
    }
}
