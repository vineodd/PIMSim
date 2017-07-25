using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Configs;

namespace PIMSim.TraceReader
{
    public class PCTxtReader : FileReader
    {
        public override Input get_req(int pid_)
        {
            string currentline = "";
            while (true)
            {
                currentline = sr[pid_].ReadLine();
                if (currentline == null)
                {
                    Instruction res = new Instruction();
                    res.type = InstructionType.EOF;
                    return res;
                }
                if (currentline.StartsWith("#") || currentline.StartsWith(";"))
                    continue;
                if (currentline.Contains(";"))
                    currentline = currentline.Substring(0, currentline.IndexOf(";") + 1);
                return new PCTrace(currentline, pid_);

            }
        }
    }
}
