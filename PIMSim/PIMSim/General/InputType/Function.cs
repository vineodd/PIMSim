#region using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.PIM;
#endregion

namespace SimplePIM.General
{
    /// <summary>
    /// [Function : InputType]
    /// Function is an abstract input to describe a sequence of code.
    /// Function only requests [Input cycle],[Duration],[Input addresses],[Output Addresses],[Name (optical)].
    /// <para>[Input cycle] Cycle when first operation that Function started.</para> 
    /// <para>[Duration] Duration time without data load or store.</para> 
    /// <para></para> 
    /// </summary>
    public class Function : InputType
    {
        public string name = "";
        public int stage;
        public List<UInt64> input;
        public List<UInt64> output;
        public UInt64 latency = 0;
        public UInt64 servetime = NULL;
        public Function()
        {
            input = new List<ulong>();
            output = new List<ulong>();
        }

        public bool sanity_check(ComputationalUnit cu) => input.Count() == cu.input_count && 1 == cu.output_count;

        public int input_count => input.Count();

        public int output_count => output.Count();


    }
}
