#region Referance
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.General;
using PIMSim.Statistics;
#endregion 

namespace PIMSim.Procs
{
    /// <summary>
    /// [Arithmetic Logic Unit]
    /// </summary>
    public class ALU : SimulatorObj
    {
        #region Private Variables

        /// <summary>
        /// Processed instructions queue.
        /// </summary>
        private Queue<KeyValuePair<UInt64, Instruction>> ins;

        /// <summary>
        /// Pipeline.
        /// </summary>
        private Stage[] pipeline = new Stage[4];

        // calculation limitation.
        private int add_count;
        private int multi_count;

        #endregion

        #region Statistics Variables
        //for statistics 
        public UInt64 total_loaded = 0;
        private StringBuilder sb = new StringBuilder();
        public UInt64 total_stalled = 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Feed'n instructions to ALU.
        /// </summary>
        /// <param name="ins_"></param>
        public void add_ins(Instruction ins_)
        {
            total_loaded++;
            if (Config.DEBUG_ALU)     
                DEBUG.WriteLine("-- ALU : Feed Insts : " + ins_.ToString());
            ins.Enqueue(new KeyValuePair<ulong, Instruction>(cycle, ins_));
        }

        /// <summary>
        /// Construction Function.
        /// </summary>
        public ALU()
        {
            
            ins = new Queue<KeyValuePair<ulong, Instruction>>();

            add_count = Config.adder_count;
            multi_count = Config.multi_count;
            //init pipeline stage

            //********************************************************
            //**                                                    **
            //**              Stage 1:     Decode                   **
            //**                                                    **
            //********************************************************
            var item_stage1 = new Stage_Decode();
            pipeline[0] = item_stage1 as Stage;
        
            //********************************************************
            //**                                                    **
            //**        Stage 2:   Load data from registers         **
            //**                                                    **
            //********************************************************
            var item_stage2 = new Stage_LoadData();
            item_stage2.set_link(ref pipeline[0]);
            pipeline[1] = item_stage2 as Stage;
            item_stage2 = null;

            //********************************************************
            //**                                                    **
            //**           Stage 3:     Calculation                 **
            //**                                                    **
            //********************************************************
            var item_stage3 = new Stage_Computing();
            item_stage3.set_add_counter(add_count);
            item_stage3.set_multi_counter(multi_count);
            item_stage3.set_link(ref pipeline[1]);
            pipeline[2] = item_stage3 as Stage;
           

            //********************************************************
            //**                                                    **
            //**       Stage 4:     Write results back              **
            //**                                                    **
            //********************************************************
            var item_stage4 = new Stage_Writeback();
            item_stage4.set_link(ref pipeline[2]);
            pipeline[3] = item_stage4 as Stage;


        }

        /// <summary>
        /// 
        /// </summary>
        public override void Step()
        {
            cycle++;
            if (Config.DEBUG_ALU)
            {
                sb.Clear();
                DEBUG.WriteLine();
                DEBUG.WriteLine("---------- ALU Update [Cycle " + cycle + "]------------");
            }
            bool final = false;

            //feed instructions
            while (true)
            {
                if (ins.Count <= 0) //no inputs
                    break;
                var processed_instruction = ins.Peek();
                Instruction added;

                if (cycle >= processed_instruction.Key)
                {
                   
                    added = processed_instruction.Value;
                }
                else
                {
                    //input is waitting for processing.
                    continue;
                }

                if (!pipeline[0].input_ready)
                {
                    //feed input to pipeline 0
                    pipeline[0].set_input(added);
                    ins.Dequeue();
                }
                break;
            }

            //pipeline stages steps.
            for (int i = pipeline.Count()-1; i >=0; i--)
            {
                
                pipeline[i].Step();
                bool stall = pipeline[i].stall;
                if (Config.DEBUG_ALU_PIPELINE)
                {
                    sb.Insert(0, "\n-- Pipieline Stage " + pipeline[i].ToString().Substring(16) + " " + (stall ? "Statlled": "UnStalled" ));
                }
                if (!stall)
                {
                    final = true;
                }
                if (i == pipeline.Count() - 1)
                {
                    final = stall;
                }
                
            }
            if (final)
            {
                //ALU stalled
                total_stalled++;
            }
            if (Config.DEBUG_ALU_PIPELINE)
            {
                DEBUG.WriteLine(sb.ToString());
                DEBUG.WriteLine();
            }

        }

        #endregion
    }

    /// <summary>
    /// [ Calculation Table ]
    /// <para>This table is used to search for required Adders and Multipliers of a specified instruction.</para>
    /// <para>You may not use this table unless you want to simulate register behaviors.</para>
    /// </summary>
    public static class Cal_table
    {
        #region Private Variables

        /// <summary>
        /// Table Entries.
        /// <para> [1 string] Opeations.</para>
        /// <para> [2 int] Used Adders.</para>
        /// <para> [3 int] Used Multipliers.</para>
        /// </summary>
        private static Tuple<string, int, int>[] table = {

            new Tuple<string, int, int>("subi", 1, 0),
            new Tuple<string, int, int>("add", 1, 0)
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Scan if operations are recorded in the table.
        /// </summary>
        /// <param name="op">target operation.</param>
        /// <returns></returns>
        public static bool ContainOPs(string op)
        {
            if (table.Any(s => s.Item1 == op))
                return true;
            return false;
        }

        /// <summary>
        /// Get one item in the table
        /// </summary>
        /// <param name="op">target operation.</param>
        /// <returns></returns>
        public static Tuple<string, int, int> GetItem(string op)
        {
            if (!table.Any(s => s.Item1 == op))
                return null;
            var item = table.Where(s => s.Item1 == op);
            if (item.Count() > 1)
                return null;
            return item.First();
        }

        #endregion
    }

}
