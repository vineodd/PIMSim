using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;
namespace SimplePIM.PIM
{
    public class PIMStage_ADD : Stage
    {
        private Counter latency;
        public override void set_input(object obj)
        {
            if (last != null && last.Count > 0)
            {
                bool all_ready = true;
                foreach (var i in last)
                {
                    object tp = 0;
                    all_ready = i.Try_Fetch && all_ready;
                }
                if (all_ready)
                {
                    input_ready = true;
                }

            }
        }

        public override bool Step()
        {
            stall = false;
            set_input(null);
            if (read_input())
            {
                latency.WaitOne();
                if (latency.Zero)
                {
                    latency.Reset();

                    write_output();
                    return true;
                }
                return false;
            }
            return false;
        }
       public PIMStage_ADD(int i,object parent)
        {
            latency = new Counter(i, i);
            Parent = parent;
        }
        public override bool read_input()
        {
            if (input_ready )
            {
                input_ready = false;
                intermid = 0;
                return true;
            }
            return false;
        }

      
    }
}
