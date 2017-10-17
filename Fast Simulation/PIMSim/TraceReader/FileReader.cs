using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.General;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.TraceReader
{
    public class FileReader 
    {
        /// <summary>
        /// FileStream of input trace
        /// </summary>
        public List<FileStream> trace = new List<FileStream>();

        /// <summary>
        /// StreamReader of FileStream.
        /// You can replace it with GZIPReader.
        /// </summary>
        public List<StreamReader> sr = new List<StreamReader>();

        public FileReader()
        {
            SET_trace_path(Config.trace_path);
        }
        public bool SET_trace_path(string trace_file)
        {

            if (Directory.Exists(trace_file))
            {

                if (Config.DEBUG_TRACE)
                    DEBUG.WriteLine("-- Trace Fetcher : Set Trace File Path : " + trace_file);

                trace = new List<FileStream>(Config.N);
                sr = new List<StreamReader>(Config.N);
                for (int i = 0; i < Config.N; i++)
                {
                    trace.Add(new FileStream(trace_file + Path.DirectorySeparatorChar + "CPU" + i + ".trace", FileMode.Open));
                    sr.Add(new StreamReader(trace[i]));
                }

                return true;
            }
            return false;

        }

        public void CloseFileHandle()
        {
            foreach (var item in sr) { item.Close(); }
            foreach (var item in trace) { item.Close(); }
        }

        public virtual Input get_req(int pid_) { return new Instruction(); }
    }
}
