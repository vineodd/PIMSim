using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Address = System.UInt64;
using PIMSim.General.Maths;

namespace PIMSim.General
{
    [Serializable]
    public class PCTrace : Input
    {
        public UInt64 PC = 0;
        public UInt64 Combination = 0;
        public int _id = 0; 

        public ProcRequest getProcRequestFromTrace()
        {
            ProcRequest result = new ProcRequest();
            char[] combine = GlobalMath.toBinary(Combination).ToArray();
            if (combine[0] == '1')
                result.type = RequestType.WRITE;
            else
                result.type = RequestType.READ;
            result.pc = PC;
            result.if_mem = true;
            combine[0] = '0';
            result.pid = _id;
            result.actual_addr = Convert.ToUInt64(String.Join("", combine), 2);

            return result;
        }

        public Instruction parsetoIns()
        {
            Instruction ins = new Instruction();
            char[] combine = GlobalMath.toBinary(Combination).ToArray();
            if (combine[0] == '1')
                ins.type = InstructionType.WRITE;
            else
                ins.type = InstructionType.READ;
            ins.pc = PC;
            ins.is_mem = true;
            ins.pid = _id;
            combine[0] = '0';
            ins.address = Convert.ToUInt64(String.Join("",combine), 2);
            return ins;
        }
        public override ulong Length()
        {
            return 2 * 1;
        }
        public PCTrace(string line,int id)
        {
            _id = id;
            string[] split = line.Split(' ');
            PC = Convert.ToUInt64(split[0],16);
            Combination = Convert.ToUInt64(split[1],16);
        }
        public Address ActualAddress()
        {
            char[] combine = GlobalMath.toBinary(Combination).ToArray();
            combine[0] = '0';
            return Convert.ToUInt64(combine.ToString(), 2);
        }
        public PCTrace()
        {
            PC = 0;
            Combination = 0;
        }
    }
}
