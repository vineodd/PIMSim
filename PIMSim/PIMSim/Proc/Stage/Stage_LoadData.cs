using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;

namespace SimplePIM.Procs
{
    public class Stage_LoadData : Stage
    {

        public override void set_input(object obj)
        {
            if (last != null)
            {
                if (last[0].output_ready)
                {
                    var obj_ = new Instruction() as object;
                    last[0].get_output(ref obj_);
                    input = obj_ as Instruction;
                    input_ready = true;
                }
            }
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
            //add code to load data
            set_input(null);
            read_input();
            write_output();
            return true;
        }


    }
}
