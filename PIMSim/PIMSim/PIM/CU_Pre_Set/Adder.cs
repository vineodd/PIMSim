#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;
using SimplePIM.Procs;

#endregion

namespace SimplePIM.PIM
{
    /// <summary>
    /// Simple Implement of ADDer
    /// We divide ADD into 3-stage pipeline: Load-Add-Store
    /// Two Load operations can be parallel executed.
    /// This function used bypass data method.
    /// </summary>
    public class Adder : ComputationalUnit, PIMInterface
    {
        /// <summary>
        /// Pipeline
        /// </summary>
        public Stage[] pipeline = new Stage[4];

        public int latency_load = 1;
        public int latnecy_store = 1;
        public int latency_op = 1;
        public InsPartition isp;
        public Function curr = null;
        //for static 
        public double energy = 0;

        public Adder(int id_,ref InsPartition insp_)
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
            var item_stage1 = new PIMStage_LoadData(latency_load);
            pipeline[0] = (item_stage1 as Stage);
            item_stage1 = null;

            //********************************************************
            //**                                                    **
            //**                Stage 1-2:   Load data              **
            //**                                                    **
            //********************************************************
            var item_stage2 = new PIMStage_LoadData(latency_load);
            pipeline[1] = item_stage2 as Stage;
            item_stage2 = null;

            //********************************************************
            //**                                                    **
            //**           Stage 3:     Calculation                 **
            //**                                                    **
            //********************************************************
            var item_stage3 = new PIMStage_ADD(latency_op);
            item_stage3.set_link(ref pipeline[0]);
            item_stage3.set_link(ref pipeline[1]);
            pipeline[2] = item_stage3 as Stage;

            //********************************************************
            //**                                                    **
            //**       Stage 4:     Write results back              **
            //**                                                    **
            //********************************************************
            var item_stage4 = new PIMStage_Store();
            item_stage4.set_link(ref pipeline[2]);
            pipeline[3] = item_stage4 as Stage;

        }
        public override void Step()
        {
            cycle++;
            
            if (curr == null)
            {
                get_input();
                //Nothing happend
                pipeline[0].set_input(curr.input[0]);
                pipeline[1].set_input(curr.input[1]);

            }
            
            for (int i = pipeline.Count() - 1; i >= 0; i--)
            {

                bool ok = pipeline[i].Step();
                if (!ok)
                {
                    //stall++
                }
                if (i == pipeline.Count() - 1)
                {
                    
                }
                if (i == 0)
                {

                    if (ok)
                    {
                        object addr = NULL;
                        pipeline[0].get_output(ref addr);
                        if (Coherence.consistency == Consistency.SpinLock)
                        {
                            Coherence.spin_lock.relese_lock(curr.input[0]);
                            Coherence.spin_lock.relese_lock(curr.input[1]);
                            Coherence.spin_lock.relese_lock(curr.output[0]);

                        }
                        curr = null;
                    }

                }

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
    }
}
