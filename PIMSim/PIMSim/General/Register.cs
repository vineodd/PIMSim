#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace PIMSim.General
{
    /// <summary>
    /// Processor Register Defination.
    /// </summary>
    public class Register 
    {
        #region Public Variables
        public string name;
        public UInt64 value;
        public UInt64 act_address; //actual addr
        public UInt64 block_address;
        public bool valid = false;
        public int avaliable_id;
        #endregion

        #region Public Methods
        public Register (string name_)
        {
            name = name_;
        }
        public Register(string name_, UInt64 value_,UInt64 actual,UInt64 block_addr=0)
        {
            name = name_;
            value = value_;
            act_address = actual;
            block_address = block_addr;
        }
        #endregion
    }


}
