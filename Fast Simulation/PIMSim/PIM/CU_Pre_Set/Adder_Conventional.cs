#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using PIMSim.Procs;
using PIMSim.Statistics;
using PIMSim.Partitioner;
#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// Simple Implement of ADDer
    /// We divide ADD into 3-stage pipeline: Load-Add-Store
    /// Two Load operations can be parallel executed.
    /// This function used bypass data method.
    /// </summary>
    public class Adder_Conventional : ComputationalUnit, PIMInterface
    {
        /// <summary>
        /// Pipeline
        /// </summary>
        public Stage[] pipeline = new Stage[4];

        public string name = "ADDer";
        public int latency_op = 1;
        public InsPartition isp;
        public Function curr = null;

        #region Statistics Variables
        public double energy = 0;
        private UInt64 total_stall = 0;
        private UInt64 total_load = 0;
        private UInt64 total_latency = 0;
        private double avg_latency => total_load != 0 ? total_latency / total_load : 0;

        #endregion

        public Adder_Conventional(int id_, ref InsPartition insp_)
        {
            
            this.id = id_;
            input_count = 2;
            output_count = 1;
            isp = insp_;
            //********************************************************
            //**                                                    **
            //**           Stage 1-1:   Load Data                   **
            //**                                                    **
            //********************************************************
            var item_stage1 = new PIMStage_LoadData(this,0);
            pipeline[0] = (item_stage1 as Stage);
            item_stage1 = null;

            //********************************************************
            //**                                                    **
            //**                Stage 1-2:   Load data              **
            //**                                                    **
            //********************************************************
            var item_stage2 = new PIMStage_LoadData(this,1);
            pipeline[1] = item_stage2 as Stage;
            item_stage2 = null;

            //********************************************************
            //**                                                    **
            //**           Stage 3:     Calculation                 **
            //**                                                    **
            //********************************************************
            var item_stage3 = new PIMStage_Computation(this,2, latency_op);
            item_stage3.set_link(ref pipeline[0]);
            item_stage3.set_link(ref pipeline[1]);
            pipeline[2] = item_stage3 as Stage;

            //********************************************************
            //**                                                    **
            //**       Stage 4:     Write results back              **
            //**                                                    **
            //********************************************************
            var item_stage4 = new PIMStage_Store(this,3);
            item_stage4.set_link(ref pipeline[2]);
            pipeline[3] = item_stage4 as Stage;

            //init callback
            read_callback = new ReadCallBack(handle_read_callback);
            write_callback = new WriteCallBack(handle_write_callback);

        }

        private void handle_write_callback(CallBackInfo callback)
        {
            if (callback != null)
            {
                foreach (var id in callback.stage_id)
                {
                    pipeline[id].status = Status.Complete;
                }
            }
            bandwidth_bit += 64;
        }

        private void handle_read_callback(CallBackInfo callback)
        {
            if (callback != null)
            {
                foreach (var id in callback.stage_id)
                {
                    pipeline[id].status = Status.Complete;
                }
            }
            bandwidth_bit += 64;
        }

        public override void Step()
        {
            cycle++;


            if (curr == null)
            {
                get_input();
                //Nothing happend
                if (curr != null)
                {
                    total_load++;
                    curr.getServed(GlobalTimer.tick);
                    bandwidth_bit += curr.Length();
                    pipeline[0].set_input(curr.input[0]);
                    pipeline[1].set_input(curr.input[1]);
                    (pipeline[3] as PIMStage_Store).store_addr = curr.output[0];
                }

            }
            bool final= false;
            for (int i = pipeline.Count() - 1; i >= 0; i--)
            {

                pipeline[i].Step();
                bool stalled = pipeline[i].stall;
                if (stalled)
                {
                    //stall++
                    final = true;
                }
                if (i == pipeline.Count() - 1)
                {
                    if (pipeline[i].output_ready)
                    {
                        if (curr != null)
                        {


                            if (Coherence.consistency == Consistency.SpinLock)
                            {
                                Coherence.spin_lock.relese_lock(curr.input[0]);
                                Coherence.spin_lock.relese_lock(curr.input[1]);
                                Coherence.spin_lock.relese_lock(curr.output[0]);

                            }
                            pipeline[i].get_output();

                            total_latency += GlobalTimer.tick - curr.servetime;

                            curr = null;
                        }
                    }
                }

            }
            if (final)
            {
                total_stall++;
            }

        }



        public void get_input()
        {
            var input = isp.get_req(this.id, false);
            if (!(input is Function))
            {
                if ((input is Instruction) && (input as Instruction).type == InstructionType.NOP)
                    return;
                Environment.Exit(2);
            }
            curr = input as Function;
        }

        public object get_output()
        {
            throw new NotImplementedException();
        }

        public void get_output(ref object output)
        {
            throw new NotImplementedException();
        }

        public override void PrintStatus()
        {
            DEBUG.WriteLine("---------------- PIM Unit [" + name + "] Statistics -------------");
            DEBUG.WriteLine();
            DEBUG.WriteLine("    Total Functions served : " + total_load);
            DEBUG.WriteLine("    Average latency        : " + avg_latency);
            DEBUG.WriteLine("    Internal Bandwidth     : " + interal_bandwidth + " MB/s");
            DEBUG.WriteLine();
        }

        public override bool outstanding_requests()
        {
            return curr != null;
        }
    }
}
