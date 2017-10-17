using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.PIM
{
    public class PIMStage_LoadData : Stage
    {
        public int latency = 0;

        public PIMStage_LoadData(object parent,int id_, int lat=0)
        {
            id = id_;
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
            if(status == Status.Complete)
            {
                intermid = input;
                input_ready = false;
                input = null;
                status = Status.NoOP;
                return true;
            }
            if (status == Status.NoOP)
            {
                if (input_ready && input != null)
                {
                    if (Coherence.consistency == Consistency.SpinLock)
                    {

                        Coherence.spin_lock.setlock((UInt64)input);


                        if (!Coherence.flush((UInt64)input, true))
                        {
                            Coherence.spin_lock.relese_lock((UInt64)input);
                            DEBUG.WriteLine("-- Waiting Host cores flushing data : [0x" + ((UInt64)input).ToString("X") + "]");
                            stall = true;
                            return false;
                        }


                    }
                    if (PIMConfigs.memory_method == PIM_Load_Method.Bypass)
                    {
                        latency--;
                        if (latency == 0)
                        {
                            intermid = input;
                            input_ready = false;
                            input = null;
                            return true;
                        }
                        return false;
                    }
                    else
                    {

                        PIMRequest req = new PIMRequest();
                        req.actual_addr = (UInt64)input;
                        req.cycle = GlobalTimer.tick;
                        req.if_mem = true;
                        req.pid = (Parent as ComputationalUnit).id;
                        req.stage_id.Add(this.id);
                        req.type = RequestType.LOAD;
                        PIMMctrl.add_to_mctrl(req);
                        status = Status.Outstanding;
                        

                        stall = true;
                        return false;
                    }
                }
                return false;
            }
            stall = true;
            return false;
        }

        public override bool Step()
        {
            stall = false;
            //add code to load data
            if (read_input())
            {
                write_output();
                (Parent as ComputationalUnit).read_callback(null);
                return true;
            }
            return false;
        }

       
    }
}
