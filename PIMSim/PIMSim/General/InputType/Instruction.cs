#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using SimplePIM.General;
using SimplePIM.Configs;

#endregion

namespace SimplePIM.General
{
    /// <summary>
    /// Instruction Defination
    /// </summary>
    public class Instruction :InputType
    {
        #region Public Varibles

        public InstructionType type;
        public UInt64 address = 0;
        public UInt64 data = 0;
        public int pid;
        public string Operation = "";
        public string Operand1 = "";
        public string Operand2 = "";
        public string Operand3 = "";
        public UInt64 pc = 0;   
        public bool is_mem = false;
        public bool can_operated = false;
        public UInt64 block_addr = 0;
        public UInt64 page_addr = 0;
        public bool ready = false;
        public bool pim = false;
        public UInt64 served_cycle = 0;

        #endregion

        public Instruction(string ins, UInt64 pc_ = 0)
        {
            Operation = ins.Substring(0, ins.IndexOf(" ") + 1).Trim();
            string inst = ins.Substring(ins.IndexOf(" ")).Trim();
            string[] split = inst.Split(',');
            for (int i = 1; i <= split.Length; i++)
            {
                FieldInfo fi = this.GetType().GetField("Operand" + i);
                fi.SetValue(this, split[i - 1]);
            }
            is_mem = false;

        }
        public Instruction(string op, UInt64 cycles, string op1, string op2 = "", string op3 = "", UInt64 pc_ = 0)
        {
            Operation = op;
            cycle = cycles;
            Operand1 = op1;
            Operand2 = op2;
            Operand3 = op3;
            is_mem = false;
        }
        public override ulong Length()
        {
            return  Config.operationcode_length;
        }

        public Instruction( UInt64 pc_ = 0)
        {
            type = InstructionType.NOP;
            is_mem = false;
        }
        public Instruction(InstructionType type_)
        {
            type = type_;
            is_mem = false;
        }
        public override string ToString()
        {

            if (type == InstructionType.NOP)
                return "NOP";
            StringBuilder sb = new StringBuilder();
            if (Operation != "")
                sb.Append(Operation + " ");
            if (Operand1 != "")
                sb.Append(Operand1 + " ");
            if (Operand2 != "")
                sb.Append(Operand2 + " ");
            if (Operand3 != "")
                sb.Append(Operand3 + " ");
            sb.Append("Cycle [" + cycle+"]");
            sb.Append(" [" + type+"]");
            if (address != 0)
                sb.Append(" Addr=[0x" + address.ToString("X") + "]");
            if (data != 0)
                sb.Append(" Data=[0x" + data.ToString("X") + "]");
            return sb.ToString();

        }
    }
    public enum InstructionType { READ, WRITE, EOF, NOP,CALCULATION };
}
