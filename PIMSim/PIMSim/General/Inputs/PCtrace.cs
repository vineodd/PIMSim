using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General.Maths;

namespace PIMSim.General
{
    public class PCTrace : Input
    {
        public UInt64 PC = 0;
        public UInt64 Combination = 0;

        public ProcRequest getProcRequestFromTrace()
        {
            ProcRequest result = new ProcRequest();
            char[] combine = GlobalMath.toBinary(Combination).ToArray();
            if (combine[0] == '1')
                result.type = RequestType.WRITE;
            else
                result.type = RequestType.READ;
            result.pc = PC;
            combine[0] = '0';
            result.actual_addr = Convert.ToUInt64(combine.ToString(), 2);
            return result;
        }
        public override ulong Length()
        {
            throw new NotImplementedException();
        }
        public PCTrace(string line)
        {
            string[] split = line.Split(' ');
            PC = Convert.ToUInt64(split[0]);
            Combination = Convert.ToUInt64(split[1]);
        }

        public PCTrace()
        {
            PC = 0;
            Combination = 0;
        }
    }
}
