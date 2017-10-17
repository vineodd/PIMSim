using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;
using Tick = System.UInt64;

namespace PIMSim.Events
{
    public class Event
    {
        //point to source unit;
        public SimulatorObj source;

        public Tick cycle;

        public SourceType type;
        public enum SourceType
        {
            TraceFetcher = 0,
            Insp = 1,
            CPU = 2,
            PIM = 3,
            Memory = 4,
            Invaild=5

        }
        public void Handle()
        {
            source.Step();
        }
    }
}
