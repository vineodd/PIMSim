using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Memory;
using SimplePIM.General;
using SimplePIM.Procs;

namespace SimplePIM
{
    class Program
    {
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (Config.fs != null)
            {
                Config.fs.Close();
                Config.sw.Close();
            }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            SimplePIMSimulator pimsim = new SimplePIMSimulator(args);
            pimsim.run();
            pimsim.PrintState();
            Console.ReadKey();

        }
    }
}
