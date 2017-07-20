#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.Memory;
using PIMSim.General;
using PIMSim.Procs;
using System.Reflection;
using PIMSim.Memory.DDR;
using System.IO;
using PIMSim.PIM;
using PIMSim.Memory.HMC;
using PIMSim.Statistics;
#endregion

namespace PIMSim.General
{
    /// <summary>
    /// PIMSimulator
    /// </summary>
    public class PIMSimulator
    {
        #region Public Methods

        /// <summary>
        /// Instruction Partitioner
        /// </summary>
        public InsPartition ins_p;

        /// <summary>
        /// host-side proceeors
        /// </summary>
        public List<Proc> proc;


        public TraceFetcher trace;
        public PageConverter pg;
        public Shared_Cache shared_cache;
        public PIM.PIM pim;

        #endregion
        public PIMSimulator(string[] args)
        {

            initAllconfigs(args);
            trace = new TraceFetcher();
            trace.SET_trace_path(Config.trace_path);


            ins_p = new InsPartition();
            ins_p.attach_tracefetcher(ref trace);
            pg = new PageConverter();

            if (Config.shared_cache)
                shared_cache = new Shared_Cache();
            proc = new List<Proc>();

            for (int i = 0; i < Config.N; i++)
            {
                Proc to_add = new Proc(ref ins_p, i);
                if (Config.shared_cache)
                    to_add.attach_shared_cache(ref shared_cache);
                to_add.attach_tlb(ref pg);
                proc.Add(to_add);
            }
            int count = 0;
            foreach(var item in Config.memory)
            {
                
                if (item.Key.Equals("HMC"))
                {
                    var tp = new HMCMem(count++) as MemObject;
                    MemorySelector.add(item.Value, ref tp);
                }
                else
                {
                    if (item.Key.Equals("DRAM")|| item.Key.Equals("PCM"))
                    {
                        var tp= new DDRMem(count++) as MemObject;
                        MemorySelector.add(item.Value, ref tp);
                    }
                    else
                    {
                        //error
                        DEBUG.Error("Unknown Memory Type.");
                        Environment.Exit(3);

                    }
                }
            }
            
            Mctrl.init_queue();

            PIMMctrl.init_queue();
 
         
            pim = new PIM.PIM(ref ins_p);
            Coherence.init();
            Coherence.linkproc(proc);
            GlobalTimer.InitClock();
        }
        public void run()
        {
            UInt64 execution = UInt64.MaxValue;
            if (Config.sim_type == SIM_TYPE.cycle)
                execution = Config.sim_cycle;

            for (UInt64 i = 0; i < execution; i++)
            {
                trace.Step();
                ins_p.Step();
                for (int j = 0; j < Config.N; j++)
                {
                    if (GlobalTimer.ifProcStep(j))
                        proc[j].Step();
                }
                Mctrl.Step();
                PIMMctrl.Step();
                foreach (var mem in MemorySelector.MemoryInfo)
                {
                    if (GlobalTimer.ifMemoryStep(0))
                        mem.Item3.Step();
                }
                if (GlobalTimer.ifPIMUnitStep(0))
                    pim.Step();

                if (Config.sim_type == SIM_TYPE.file)
                {
                    bool done = false;
                    done = ins_p.trace_done();
                    proc.ForEach(x => done = done || x.outstanding_requests());
                    if (Config.use_pim)
                        pim.unit.ForEach(x => done = done || x.outstanding_requests());
                    if (!done)
                        return;
                    
                }
                GlobalTimer.Step();
            }

        }
        public void initAllconfigs(string[] args)
        {

            //before parsing args, overallconfig file should be initialed
            parse_args(args);

            Config.read_configs();
            Config.initial();
            PIMConfigs.initConfig();
            

        }
        private void Usage()
        {
            DEBUG.WriteLine("PIMSim Usage:");
            DEBUG.WriteLine("PIMSim -t tracefilepath -config configfilepath –o outputfile –n processorcount –c cycle");
            DEBUG.WriteLine("  -t, -trace FILEPATH      specify the path folder of input trace.");
            DEBUG.WriteLine("  -config FILEPATH     specify the path folder of input configs.");
            DEBUG.WriteLine("  -o, -output  FILENAME         specify the file name of output file.");
            DEBUG.WriteLine("  -n, -N  PROCCOUNT         specify the count of host proc.");
            DEBUG.WriteLine("  -c, -cycle CYCLES         specify the execution cycles .");
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
                if (command.Equals("trace", StringComparison.OrdinalIgnoreCase) || command.Equals("t"))
                {
                    Config.trace_path = args[i + 1];
                }
                else
                {
                    if (command.Equals("config", StringComparison.OrdinalIgnoreCase))
                    {
                        Config.config_path = args[i + 1];
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
                                if (command.Equals("c", StringComparison.OrdinalIgnoreCase)|| command.Equals("cycle", StringComparison.OrdinalIgnoreCase))
                                {
                                    Config.sim_type = SIM_TYPE.cycle;
                                    Config.sim_cycle = UInt64.Parse(args[i + 1]);
                                }
                                Usage();
                                Environment.Exit(1);
                            }
                        }
                    }
                }
              
            }
            return true;

        }

        public bool setValue(string key_, object value_)
        {
            return Config.SetValue(key_, value_);
        }

        public void PrintStatus()
        {
            DEBUG.WriteLine();
            DEBUG.WriteLine();
            DEBUG.WriteLine("++++++++++++++++++++++     Statistics    ++++++++++++++++++++");
            DEBUG.WriteLine();
            DEBUG.WriteLine();
            foreach (var item in proc)
            {
                item.PrintStatus();
                
            }
            Mctrl.PrintStatus();
            if (Config.use_pim)
                PIMMctrl.PrintStatus();
                
            
            ins_p.PrintStatus();
            
            foreach (var item in pim.unit)
            {
                item.PrintStatus();
                
            }
        }
    }
}
