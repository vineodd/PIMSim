using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;
using SimplePIM.Procs;
using SimplePIM.Configs;

namespace SimplePIM.PIM
{
    public abstract class ComputationalUnit:SimulatorObj
    {
        public int output_count = 0;
        public int input_count = 0;
        public static readonly UInt64 NULL = UInt64.MaxValue;
        public int id;
    }
   
}
