#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;

#endregion

namespace PIMSim.Memory
{
    /// <summary>
    /// Memory Requets Defination
    /// </summary>
    public class MemRequest : Request
    {
        #region Public Variables

        public UInt64 address;
        public UInt64 data;
        public UInt64 block_addr;
        public MemReqType memtype;
        public UInt64 cycle = 0;
        public bool pim = false;
        public List<int> pid = new List<int>();
        public List<int> stage_id = new List<int>();

        #endregion

        #region Public Methods

        public MemRequest()
        {
            address = 0;
            data = 0;
            memtype = MemReqType.NULL;
        }

        public MemRequest(UInt64 address_, UInt64 data_,UInt64 block, MemReqType memtype_)
        {
            address = address_;
            data = data_;
            memtype = memtype_;
        }

        #endregion
    }

    public enum MemReqType
    {
        //basic memory requests
        READ,
        WRITE,
        RETURN_DATA,
        FLUSH,
        LOAD,
        STORE,
        NULL
    }
}
