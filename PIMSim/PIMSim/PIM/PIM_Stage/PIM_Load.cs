using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;

namespace SimplePIM.PIM
{
    public class PIMStage_LoadData : Stage
    {
        public int latency = 0;
        public PIMStage_LoadData(int lat,object parent)
        {
            latency = lat;
            Parent = parent;
        }
        public override void set_input(object obj)
        {
            input = (UInt64)obj;
            input_ready = true;
        }

        public override bool read_input()
        {
            
            if (input_ready && input != null)
            {
                if (Coherence.consistency == Consistency.SpinLock)
                {
                   var locked= Coherence.spin_lock.get_lock_state((UInt64)input);
                    if (locked)
                        return false;
                    Coherence.spin_lock.setlock((UInt64)input);
                }
                latency--;
                if (latency == 0)
                {
                    intermid = input;
                    input_ready = false;
                    input = null;
                    return true;
                }
            }
            return false;
        }

        public override bool Step()
        {
            //add code to load data
            if (read_input())
            {
                write_output();
                (Parent as ComputationalUnit).read_callback(0, 0);
                return true;
            }
            return false;
        }

       
    }



}
