#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.Statistics;
using PIMSim.Memory;
#endregion

namespace PIMSim.PIM
{
    /// <summary>
    /// Spin Lock Defination
    /// Spin Lock is an overall data coherence method. By looking at spinlock table,Processors and PIM units can get informations that which block are used.
    /// </summary>
    public class SpinLock
    {
        #region static information
        /// <summary>
        /// Use string zero to avoid Ultra list index
        /// Max index of C# List is Int32.MaxValue
        /// </summary>
        private readonly string empty = "0000000000000000000000000000000000000000000000000000000000000000";

        private readonly char TRUE = '1';
        private readonly char FALSE = '0';

        #endregion
        /// <summary>
        /// the page index of an address
        /// </summary>
        public UInt64 page_index;

        /// <summary>
        /// Page Size
        /// </summary>
        public UInt64 size = Config.page_size;

        /// <summary>
        /// Lock table
        /// </summary>
        public List<string> lock_table;

        //for statistics
        private UInt64 total_set_lock = 0;
        private UInt64 total_get_lock = 0;
        private UInt64 total_unlock = 0;
        private UInt64 total_stalled = 0;
        private UInt64 total_unstalled = 0;
        public UInt64 total_request => total_get_lock + total_set_lock + total_unlock;

        public SpinLock()
        {
            page_index = MemorySelector.get_RAM_size() / size +1;

            lock_table = new List<string>();
            //foreach block entry, set lock table false.
            for (int i = 0; i < (Int64)(page_index / 64); i++)
                lock_table.Add(empty);
        }
        /// <summary>
        /// Set data locked.
        /// </summary>
        /// <param name="addr">Used address</param>
        public void setlock(UInt64 addr)
        {
            //resize address in case of address is out of range.
            var addr_ = MemorySelector.resize(addr);

            Int64 index_all = (Int64)(addr_ / size);
            int index = (int)(index_all / 64);
            int mod = (int)(index_all % 64);
            setbit(mod, index, true);
            total_set_lock++;
        }
        /// <summary>
        /// Get current lock state of an address.
        /// </summary>
        /// <param name="addr">Used address</param>
        /// <returns></returns>
        public bool get_lock_state(UInt64 addr)
        {
            total_get_lock++;
            var addr_ = MemorySelector.resize(addr);
            var index = addr_ / size;
            Int32 i = (Int32)(index / 64);
            int j = (int)(index % 64);
            //  return lock_table[(Int32)(addr / page_index)];
            if ((lock_table[i].ToArray())[j] == TRUE)
            {
                total_stalled++;
                return true;
            }
            total_unstalled++;
            return false;
        }
        public void relese_lock(UInt64 addr)
        {
            var addr_ = MemorySelector.resize(addr);
            var index = addr_ / size;
            Int32 i = (Int32)(index / 64);
            int j = (int)(index % 64);
            //  return lock_table[(Int32)(addr / page_index)];

            var item = lock_table[i].ToArray();
            item[j] = FALSE;
            lock_table[i] = new string(item);
            total_unlock++;
        }

        /// <summary>
        /// Set a bit/
        /// </summary>
        /// <param name="index">page index</param>
        /// <param name="i">page offset</param>
        /// <param name="lock_">lock state</param>
        public void setbit(Int64 index, Int32 i, bool lock_)
        {
            var data = lock_table[i].ToArray();
            char bit = lock_ ? TRUE : FALSE;
            data[index] = bit;
            lock_table[i] = new string(data);

        }
        /// <summary>
        /// Print current status.
        /// </summary>
        public void PrintStatus()
        {
            DEBUG.WriteLine("=====================SpinLock Statistics=====================");
            DEBUG.WriteLine();
            DEBUG.WriteLine("        Total Requests Served : " + total_request);
            DEBUG.WriteLine("        SetLock Requests      : " + total_set_lock);
            DEBUG.WriteLine("        GetLock Requests      : " + total_get_lock);
            DEBUG.WriteLine("        UnLock Requests       : " + total_unlock);
            DEBUG.WriteLine("      Total stalled/Unstalled : " + total_stalled + "/" + total_unstalled);
            DEBUG.WriteLine();
        }

    }
}