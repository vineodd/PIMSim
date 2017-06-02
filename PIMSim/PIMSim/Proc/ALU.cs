#region Referance
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.Statistics;
#endregion 
namespace SimplePIM.Procs
{
    /// <summary>
    /// [Arithmetic Logic Unit]
    /// This 
    /// </summary>
    public class ALU : SimulatorObj
    {

        public Queue<KeyValuePair<UInt64, Instruction>> ins;
        public ALUSim_Type type;
        public Stage[] pipeline = new Stage[4];
        public Counter para_load;
        public Counter add_count;
        public Counter multi_count;

        private StringBuilder sb = new StringBuilder();
        //for statics 
        public UInt64 total_loaded = 0;
        public void add_ins(Instruction ins_)
        {
            total_loaded++;
            if (Config.DEBUG_ALU)     
                DEBUG.WriteLine("-- ALU : Feed Insts : " + ins_.ToString());
            ins.Enqueue(new KeyValuePair<ulong, Instruction>(cycle, ins_));
        }
        public ALU()
        {
            
            ins = new Queue<KeyValuePair<ulong, Instruction>>();

            add_count = new Counter(Config.add_ability, Config.add_ability);
            multi_count = new Counter(Config.multi_ability, Config.multi_ability);
            para_load = new Counter(Config.para_load, Config.para_load);
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
            var item_stage3 = new Stage_Computation();
            item_stage3.set_add_counter(ref add_count);
            item_stage3.set_multi_counter(ref multi_count);
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

            if (Config.DEBUG_ALU)
                DEBUG.WriteLine("-- ALU : Initialed.");

        }
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
            while (true)
            {
                if (ins.Count <= 0)
                    break;
                var to_add = ins.Peek();
                Instruction added;
                if (cycle >= to_add.Key)
                {

                    added = to_add.Value;
                }
                else
                {
                    continue;
                }
                if (!pipeline[0].input_ready)
                {
                    pipeline[0].set_input(added);
                    ins.Dequeue();
                }
                break;
            }
            for (int i = pipeline.Count()-1; i >=0; i--)
            {
                
                bool stall = pipeline[i].Step();
                if (Config.DEBUG_ALU)
                {
                    sb.Insert(0, "\n-- Pipieline Stage " + pipeline[i].ToString().Substring(16) + " " + (stall ? "UnStalled" : "Statlled"));
                }
                if (!stall)
                {
                    //stall++
                }
                if (i == pipeline.Count() - 1)
                {
                    final = stall;
                }
                
            }
            if (Config.DEBUG_ALU)
            {
                DEBUG.WriteLine(sb.ToString());
                DEBUG.WriteLine("---------------------------------------------");
            }

        }
    }
    public static class Cal_table
    {
        public static Tuple<string, int, int>[] table = {

             new Tuple<string, int, int>("subi", 1, 0),
            new Tuple<string, int, int>("add",1,0)
        };
        public static bool ContainOPs(string op)
        {
            if (table.Any(s => s.Item1 == op))
                return true;
            return false;
        }
        public static Tuple<string, int, int> GetItem(string op)
        {
            if (!table.Any(s => s.Item1 == op))
                return null;
            var item = table.Where(s => s.Item1 == op);
            if (item.Count() > 1)
                return null;
            return item.First();
        }
    }
    public enum ALUSim_Type
    {
        AVG,
        TABLE
    }
    
}
