using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Memory;
using SimplePIM.General;
using SimplePIM.Procs;
using SimplePIM.Memory.DDR;
namespace SimplePIM.Procs
{
    public class XBar : SimulatorObj
    {
        public List<MemRequest> proc_req;
        public List<MemRequest> mem_req;

        public MemObject mem;
        public XBar()
        {

            proc_req = new List<MemRequest>();
            mem_req = new List<MemRequest>();
        }
        public void attach_mem(ref MemObject mem_)
        {
            this.mem = mem_;
        }

        public override void Step()
        {
            cycle++;
            int processed = 0;
            for(int i = 0; i < proc_req.Count; i++)
            {
                if(cycle - proc_req[i].ts_departure< (ulong)Config.xbar_latency)
                {
                    //send to memory
                    processed++;
                    mem.addTransation(proc_req[0]);

                }

            }
            proc_req.RemoveRange(0, processed);

            processed = 0;
            for (int i = 0; i < proc_req.Count; i++)
            {
                if (cycle - mem_req[i].ts_departure < (ulong)Config.xbar_latency)
                {
                    //send back to cpu
                    processed++;
                }

            }
        }


    }
}
