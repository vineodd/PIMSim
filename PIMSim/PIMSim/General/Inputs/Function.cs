#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.PIM;
using PIMSim.Configs;

#endregion

namespace PIMSim.General
{
    /// <summary>
    /// [Function : InputType]
    /// Function is an abstract input to describe a sequence of code.
    /// Function only requests [Input cycle],[Duration],[Input addresses],[Output Addresses],[Name (optical)].
    /// <para>[Input cycle] Cycle when first operation that Function started.</para> 
    /// <para>[Duration] Duration time without data load or store.</para> 
    /// <para></para> 
    /// </summary>
    [Serializable]
    public class Function : Input
    {
        #region Public Variables

        public string name = "";
        public List<UInt64> input;
        public List<UInt64> output;
        public UInt64 latency = 0;
        public UInt64 servetime = NULL;

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Function
        /// </summary>
        public Function()
        {
            input = new List<ulong>();
            output = new List<ulong>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override UInt64 Length()
        {
            UInt64 total_length = 0;
            total_length = total_length + (ulong)input.Count() * Config.address_bit;
            total_length = total_length + (ulong)output.Count() * Config.address_bit;
            //in software simulation, we use string "name" to indicate corresponding units.
            //but in fact, we use id. Each fuction id take in log2(id) bit.
            total_length = total_length + (uint)(Math.Log(PIMConfigs.pim_cu_count) / Math.Log(2));
            return total_length;

        }
        public bool sanity_check(ComputationalUnit cu) => input.Count() == cu.input_count && 1 == cu.output_count;

        public int input_count => input.Count();

        public int output_count => output.Count();

        /// <summary>
        /// Set served time
        /// </summary>
        /// <param name="time"></param>
        public void getServed(UInt64 time)
        {
            servetime = time;
        }

        #endregion
    }
}
