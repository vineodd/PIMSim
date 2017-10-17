#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.Procs;
using PIMSim.General;
using System.IO;
using PIMSim.Statistics;
using System.Reflection;
using PIMSim.Partitioner;
#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// PIM defination
    /// </summary>
    public class PIM : SimulatorObj
    {
        #region Private Variables
        /// <summary>
        /// attached instruction partationer
        /// </summary>
        private InsPartition ins_p;

        #endregion

        #region Public Variables
        /// <summary>
        /// all computational unit includes procs and pipelines
        /// </summary>
        public List<ComputationalUnit> unit;



        #endregion

        #region Public Methods
        public PIM(ref InsPartition ins_p_)
        {
            if (Config.DEBUG_PIM)
                DEBUG.WriteLine("PIM Module Initialed.");
            ins_p = ins_p_;

            unit = new List<ComputationalUnit>();
            if (PIMConfigs.unit_type == PIM_Unit_Type.Processors)
            {
                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("PIM Unit Type : Processors.");
                for (int i = 0; i < PIMConfigs.N; i++)
                {
                    var p = new PIMProc(ref ins_p, i);
                    unit.Add(p);

                }
            }
            else
            {
                if (Config.DEBUG_PIM)
                    DEBUG.WriteLine("PIM Unit Type : Pipeline.");
                //pipeline mode
                // When PIMSim runs into pipeline mode, input should always be a Function. 

                for (int i = 0; i < PIMConfigs.CU_Name.Count; i++)
                {
                    if (PIMConfigs.CU_Name[i] == "Customied")
                    {
                        //add your code here
                    }
                    else
                    {
                        if (PIMConfigs.CU_Name[i] == "Adder")
                        {
                            unit.Add(new Adder(i, ref ins_p) as ComputationalUnit);
                            return;
                        }
                        else
                        {
                            if (PIMConfigs.CU_Name[i] == "Adder_Conventional")
                            {
                                unit.Add(new Adder_Conventional(i, ref ins_p) as ComputationalUnit);
                                return;
                            }
                            else
                            {
                                DEBUG.Error("No PIM Unit templates.");
                                Environment.Exit(2);
                            }
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
        #endregion

    }
}
