using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.General.Maths
{
   public static class GlobalMath
    {
        public static string toBinary(UInt64 i)
        {
            string s = "";
            foreach (var item in BitConverter.GetBytes(i))
            {
                s = Convert.ToString(item, 2).PadLeft(8, '0') + s;
            }
            return s;
        }
    }
}
