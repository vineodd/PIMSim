#region Reference 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Statistics;
#endregion

namespace PIMSim.Configs
{
    /// <summary>
    /// HMC configs
    /// </summary>
    public class HMCConfig
    {
        #region Public Variables
        public UInt32 num_devs = 0;
        public UInt32 num_links = 0;
        public UInt32 num_vaults = 0;
        public UInt32 queue_depth = 0;
        public UInt32 num_banks = 0;
        public UInt32 num_drams = 0;
        public UInt32 capacity = 0;
        public UInt32 xbar_depth = 0;
        public UInt32 bsize = 128;
        #endregion

        #region Public Methods
        public void initConfig(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Replace(" ", "");
                string[] temp = line.Split('=');
                SetValue(temp[0], temp[1]);
            }
            sr.Close();
            fs.Close();
        }
        public bool SetValue(string name, object value)
        {
            try
            {
                var s = typeof(HMCConfig).GetField(name).GetValue(this);
                typeof(HMCConfig).GetField(name).SetValue(this, Convert.ChangeType(value, s.GetType()));
            }
            catch 
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("WARNING: Failed to set Parms:" + name + " = " + value.ToString() + ", plz check if necessary.");

                return false;
            }
            return true;
        }
        #endregion
    }
}
