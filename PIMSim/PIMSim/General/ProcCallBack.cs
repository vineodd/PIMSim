using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.General
{
    public delegate void ReadCallBack(UInt64 block_addr,UInt64 act_addr);
    public delegate void WriteCallBack(UInt64 block_addr, UInt64 act_addr);
}
