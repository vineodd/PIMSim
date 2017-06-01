using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;

namespace SimplePIM.Procs
{
    public class Stage_Decode : Stage
    {


        public override void set_input(object obj)
        {
            input = (Instruction)obj;
            input_ready = true;
        }

        public override bool read_input()
        {
            if (input_ready && input != null)
            {
                intermid = input;
                input_ready = false;
                input = null;
                return true;
            }
            return false;
        }
        public override bool Step()
        {
            read_input();
            write_output();
            return true;
        }


    }
}
