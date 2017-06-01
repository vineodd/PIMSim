using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.General;
using SimplePIM.Statics;
namespace SimplePIM.Procs
{
    public class InsPartition : SimulatorObj
    {

        public int n = 0;

        public List<Queue<InputType>> all_ins;
        public List<Queue<InputType>> pim_ins;
        public TraceFetcher trace;
        public List<bool> eof;
        public InsPartition()
        {

            n = Config.N;
            all_ins = new List<Queue<InputType>>();
            eof = new List<bool>();
            for (int i = 0; i < n; i++)
            {
                all_ins.Add(new Queue<InputType>());
                eof.Add(false);
            }
            pim_ins = new List<Queue<InputType>>();
            for (int i = 0; i < Config.pim_config.pim_cu_count; i++)
            {
                pim_ins.Add(new Queue<InputType>());
            }

        }
        public void attach_tracefetcher(ref TraceFetcher trace_)
        {
            if (Config.DEBUG_INSP)
                DEBUG.WriteLine("-- InsP : Attached TraceFetch.");
            trace = trace_;
        }
        //    public Instruction send_to_cpu(int pid_)

        public InputType get_req(int pid, bool host = false)
        {
            if (host)
            {
                
                    
                if (all_ins[pid].Count == 0)
                {
                    return new Instruction();
                }
                InputType current = all_ins[pid].Peek();
                if (current.cycle > cycle - 1)
                    return new Instruction();
                else
                {

                    if (current.cycle <= cycle - 1)
                    {
                        if (current is Function)
                        {
                            //procs cannot process function
                            Environment.Exit(1);
                        }
                        //pop current ins
                        all_ins[pid].Dequeue();
                        return current;
                    }
                    else
                    {
                        //current.cycle < cycle
                        //if program runs into this part, exit in error
                        Console.WriteLine("ERROR : ");
                        Environment.Exit(1);
                        return null;
                    }
                }
            }
            else
            {
                if (pim_ins[pid].Count == 0)
                {
                    return new Instruction();
                }
                InputType current = pim_ins[pid].Peek();
                if (current.cycle > cycle - 1)
                    return new Instruction();
                else
                {
                    if (current.cycle <= cycle - 1)
                    {
                        pim_ins[pid].Dequeue();
                        return current;
                    }
                    else
                    {
                        //current.cycle < cycle
                        //if program runs into this part, exit in error
                        Console.WriteLine("ERROR : ");
                        Environment.Exit(1);
                        return null;
                    }
                }
            }
          
        }
        public int corresponding_unit(InputType ins_)
        {
            return 0;
        }
        public override void Step()
        {
            cycle++;
            for (int i = 0; i < Config.N; i++)
            {
                for (int j = 0; j < Config.IPC; j++)
                {
                    if (!eof[i])
                    {
                        if (all_ins[i].Count() >= Config.max_insp_count)
                        {
                            continue;
                        }
                        InputType to_add = trace.get_req(i);
                        if (to_add is Instruction)
                        {
                            if ((to_add as Instruction).type != InstructionType.EOF)
                            {
                                if(!(to_add as Instruction).pim)
                                    all_ins[i].Enqueue(to_add);
                                else
                                {
                                    pim_ins[corresponding_unit(null)].Enqueue(to_add);
                                }
                                to_add = null;
                            }
                            else
                            {
                                eof[i] = true;
                            }
                        }
                        else
                        {
                            pim_ins[corresponding_unit(null)].Enqueue(to_add);                   
                        }
                    }
                }
            }
           
        }
    }
}
