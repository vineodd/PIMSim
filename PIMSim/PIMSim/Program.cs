using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.Memory;
using PIMSim.General;
using PIMSim.Procs;

namespace PIMSim
{
    class Program
    {
        /// <summary>
        /// When application is going to exit, close file handles.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (Config.fs != null)
            {
                Config.fs.Close();
                Config.sw.Close();
            }
            Console.ReadKey();
        }
        public static PIMSimulator pimsim;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            pimsim = new PIMSimulator(args);
            pimsim.run();
            pimsim.PrintStatus();
            Console.ReadKey();

        }
    }
}
