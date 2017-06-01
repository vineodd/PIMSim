using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;
using SimplePIM.Procs;
using SimplePIM.General;
using System.IO;
using SimplePIM.Statics;

namespace SimplePIM.PIM
{
    public class PIM_ : SimulatorObj
    {
        public List<ComputationalUnit> unit;
        public InsPartition ins_p;
        public List<Function> cur = new List<Function>();
        public StringBuilder sb = new StringBuilder();

        public PIM_(ref InsPartition ins_p_, ref Mctrl mctrl_)
        {
            if (Config.DEBUG_PIM)
                DEBUG.WriteLine("PIM Module Initialed.");
            ins_p = ins_p_;

            unit = new List<ComputationalUnit>();
            if (Config.pim_config.unit_type == PIM_Unit_Type.Processors)
            {
                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("PIM Unit Type : Processors.");
                for (int i = 0; i < Config.pim_config.N; i++)
                {
                    var p = new PIMProc(ref ins_p, i);
                    p.attach_memctrl(ref mctrl_);
                    unit.Add(p);

                }
            }
            else
            {
                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("PIM Unit Type : Pipeline.");
                //pipeline mode
                // When PIMSim runs into pipeline mode, input should always be a Function. 

                for (int i = 0; i < Config.pim_config.CU_Name.Count; i++)
                {
                    if (Config.pim_config.CU_Name[i] == "Customied")
                    {
                        //add here
                    }
                    else
                    {
                        if (Config.pim_config.CU_Name[i] == "Adder")
                        {
                            unit.Add(new Adder(i, ref ins_p) as ComputationalUnit);


                            return;
                        }

                    }
                }
            }

        }


        public override void Step()
        {

            for (int i = unit.Count - 1; i >= 0; i--)
            {
                unit[i].Step();
            }
        }

    }
}
