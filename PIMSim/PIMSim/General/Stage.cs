using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;

namespace SimplePIM.General
{
    public abstract class Stage
    {
        public object input = null;
        public object output = null;
        public object intermid = null;
        public bool input_ready = false;
        public bool output_ready = false;
        public List<Stage> last = new List<Stage>();
        public delegate void returnT();
        public abstract bool Step();
        public abstract void set_input(object obj);
        public abstract bool read_input();

        public bool Try_Fetch => output_ready;
        public void write_output()
        {
            if (intermid != null)
            {
                output = intermid;
                output_ready = true;
                intermid = null;
            }
            else
            {
                output_ready = false;
            }

        }
        public bool get_output(ref object out_)
        {
            if (output_ready)
            {
                output_ready = false;
                out_ = output;
                output = null;
                return true;
            }
            else
            {
                return false;
            }
        }
        public void set_link(ref Stage last_)
        {
            last.Add(last_);
        }
        
    }
    
   
    
    
}
