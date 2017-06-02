#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Memory;
using SimplePIM.General;
using SimplePIM.Procs;
using System.Reflection;
using SimplePIM.Memory.DDR;
using System.IO;
using SimplePIM.PIM;
using SimplePIM.Memory.HMC;
using SimplePIM.Statistics;
#endregion
namespace SimplePIM.General
{
    public class PIMSimulator
    {

        public InsPartition ins_p;
        public List<Proc> proc;
        public MemObject mem;
        public TraceFetcher trace;
        public Mctrl[] mctrl;
        public PageConverter pg;
        public Shared_Cache shared_cache;
        public PIM_ pim;
        public PIMSimulator(string[] args)
        {
            // Config.config_file = Environment.CurrentDirectory + @"\Test\4cpu2pu_1function";
            initAllconfigs(args);
            mctrl = new Mctrl[2];
            trace = new TraceFetcher();
            trace.SET_trace_path(Config.trace_path);
            //    trace.SET_trace_path(Environment.CurrentDirectory+ @"\Test\4cpu2pu_1function");

            ins_p = new InsPartition();
            ins_p.attach_tracefetcher(ref trace);
            pg = new PageConverter();

            if (Config.shared_cache)
                shared_cache = new Shared_Cache();
            proc = new List<Proc>();
            mctrl[0] = new Mctrl();
            for (int i = 0; i < Config.N; i++)
            {
                Proc to_add = new Proc(ref ins_p, i);
                if (Config.shared_cache)
                    to_add.attach_shared_cache(ref shared_cache);
                to_add.attach_memctrl(ref mctrl[0]);
                to_add.attach_tlb(ref pg);
                proc.Add(to_add);
            }
            if (Config.pim_config.ram_type == RAM_TYPE.DRAM)
                mem = new DDRMem(ref proc, 0) as MemObject;
            else
            {
                if (Config.pim_config.ram_type == RAM_TYPE.HMC)
                {
                    mem = new HMCMem(ref proc) as MemObject;
                }
            }
            MemorySelecter.add(32, ref mem);
            mctrl[0].init_queue();
            mem.attach_mctrl(ref mctrl[0]);
            //   mem.attach_proc_return(ref proc);


            mctrl[1] = new Mctrl(true);
            mctrl[1].init_queue();
            mem.attach_mctrl(ref mctrl[1]);
            pim = new PIM_(ref ins_p, ref mctrl[1]);
            Coherence.init();
            Coherence.linkproc(proc);
            OverallClock.InitClock();
        }
        public void run()
        {
            for (UInt64 i = 0; i < Config.sim_cycle; i++)
            {
                trace.Step();
                ins_p.Step();
                for (int j = 0; j < Config.N; j++)
                {
                    if (OverallClock.ifProcStep(j))
                        proc[j].Step();
                }
                foreach (var m in mctrl)
                    m.Step();
                foreach (var mem in MemorySelecter.MemoryInfo)
                {
                    if (OverallClock.ifMemoryStep(0))
                        mem.Item3.Step();
                }
                if (OverallClock.ifPIMUnitStep(0))
                    pim.Step();
                if (i % 100 == 0)
                    Console.WriteLine("Cycle: " + i);
                OverallClock.Step();
            }
        }
        public void initAllconfigs(string[] args)
        {

            //before parsing args, overallconfig file should be initialed
            parse_args(args);

            Config.read_configs();
            Config.initial();
            Config.pim_config.initConfig();
            

        }
        private void Usage()
        {
            DEBUG.WriteLine("PIMSim Usage:");
            DEBUG.WriteLine("PIMSim -t tracefilepath -c configfilepath");
            DEBUG.WriteLine("  -t, -trace_path FILEPATH      specify the path folder of input trace.");
            DEBUG.WriteLine("  -c, -config_path FILEPATH     specify the path folder of input configs.");
            DEBUG.WriteLine("  -o, -output  FILENAME         specify the file name of output file.");
            DEBUG.WriteLine("  -n, -N  PROCCOUNT         specify the count of host proc.");
        }
        public bool parse_args(string[] args)
        {
            if (args.Count() % 2 != 0 || args.Count() == 0)
            {
                DEBUG.Error("Please make sure that all the args are input correctly.");
                Environment.Exit(2);
            }
            for (int i = 0; i < args.Count(); i += 2)
            {
                string command = args[i].Replace("-", "");
                if (command.Equals("trace_path", StringComparison.OrdinalIgnoreCase) || command.Equals("t"))
                {
                    Config.trace_path = args[i + 1];
                }
                else
                {
                    if (command.Equals("config_path", StringComparison.OrdinalIgnoreCase) || command.Equals("c"))
                    {
                        Config.config_file = args[i + 1];
                    }
                    else
                    {
                        if (command.Equals("output", StringComparison.OrdinalIgnoreCase) || command.Equals("o"))
                        {
                            Config.output_file = args[i + 1];
                        }
                        else
                        {
                            if (command.Equals("n", StringComparison.OrdinalIgnoreCase))
                            {
                                Config.N = Int16.Parse(args[i + 1]);
                            }
                            else
                            {
                                Usage();
                                Environment.Exit(1);
                            }
                        }
                    }
                }
                //bool add_res = setValue(command, args[i + 1]);
                //if(!add_res)
                //{
                //    Console.WriteLine("WARNING : setValue failed --- Command : -" + command + "   Value = " + args[i + 1]);
                //}
            }
            return true;

        }

        public bool setValue(string key_, object value_)
        {
            return Config.SetValue(key_, value_);
        }

        public void PrintStatus()
        {
            foreach (var item in proc)
                item.PrintStatus();
            ins_p.PrintStatus();   
        }
    }
}
