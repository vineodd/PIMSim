using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.General;

namespace PIMSim.Events
{
    public class EventManager :SimulatorObj
    {
        public new string name = "EventManager";

        public List<Event> event_queue = new List<Event>();

        public void Schedule()
        {
            event_queue = event_queue.OrderBy(x => x.cycle).ThenBy(x => x.type).ToList();
        }

        public override void Step()
        {
            Schedule();
            HandleEvent();
        }

        public void HandleEvent()
        {
            var cur_events=event_queue.Where(x=>x.cycle<=GlobalTimer.tick).OrderBy(x => x.type).ToList();
            cur_events.ForEach(x => x.Handle());
            event_queue = event_queue.Where(x => !cur_events.Contains(x)).ToList();
        }
    }

    
}
