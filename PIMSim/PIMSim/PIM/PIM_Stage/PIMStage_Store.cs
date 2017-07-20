#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Statistics;
using PIMSim.Procs;
using PIMSim.Configs;

#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// PIM pipeline stage : Store
    /// </summary>
    public class PIMStage_Store : Stage
    {
        #region Private Variabels
        /// <summary>
        /// store latency cycle when use ByPass mode
        /// </summary>
        private int latency = 0;

        #endregion

        #region Public Variables
        /// <summary>
        /// Target Store address
        /// </summary>
        public UInt64 store_addr = 0;

        #endregion

        #region Public Methods
        /// <summary>
        /// read input of last stage
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            if (last != null)
            {
                //has last stage
                if (last[0].output_ready)
                {
                    // last stage output ready
                    last[0].get_output();
                    //set input as target store address
                    input = store_addr;
                    input_ready = true;
                }
                else
                    input_ready = false;
            }
            else
                input_ready = false;

        }

        /// <summary>
        /// Construction Function
        /// </summary>
        /// <param name="parent">parent cu</param>
        /// <param name="id_">id</param>
        /// <param name="lat">latency</param>
        public PIMStage_Store(object parent, int id_, int lat = 0)
        {
            id = id_;
            latency = lat;
            Parent = parent;
        }


        /// <summary>
        /// try to store data to target address
        /// </summary>
        /// <returns></returns>
        public override bool read_input()
        {
            if (status == Status.Complete)
            {
                //store complete
                intermid = input;
                input_ready = false;
                input = null;
                status = Status.NoOP;
                return true;
            }
            if (status == Status.NoOP)
            {
                //currently no operations
                if (input_ready && input != null)
                {
                    if (Coherence.consistency == Consistency.SpinLock)
                    {
                        //lock target address
                        Coherence.spin_lock.setlock((UInt64)input);

                        // try to flush data in host core
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
                        //bypass mode :store only cost latency cycles
                        latency--;
                        if (latency == 0)
                        {
                            intermid = input;
                            input_ready = false;
                            input = null;
                            return true;
                        }
                        stall = true;
                        return false;
                    }
                    else
                    {
                        //try to add store request to PIM memory controller
                        PIMRequest req = new PIMRequest();
                        req.actual_addr = (UInt64)input;
                        req.cycle = GlobalTimer.tick;
                        req.if_mem = true;
                        req.pid = (Parent as ComputationalUnit).id;
                        req.stage_id.Add(this.id);
                        req.type = RequestType.STORE;
                        PIMMctrl.add_to_mctrl(req);
                        status = Status.Outstanding;
                        stall = true;
                        return false;
                    }
                }
                return false;
            }

            //at this time, pipeline is waitting for store callback of memoryobjects.
            stall = true;
            return false;
        }

        public override bool Step()
        {
            stall = false;  //rest stall to false

            set_input(null);
            if (read_input())
            {
                //data stored 
                write_output();

                //statistics
                (Parent as ComputationalUnit).write_callback(null);


                return true;
            }


            return false;
        }

        #endregion
    }
}
