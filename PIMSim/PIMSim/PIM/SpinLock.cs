using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;

namespace SimplePIM.PIM
{
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
        public UInt64 page_index;
        public UInt64 size = Config.page_size;
        public List<string> lock_table;
        public SpinLock()
        {
            page_index = MemorySelecter.get_RAM_size() / size;

            lock_table = new List<string>();
            for (int i = 0; i < (Int64)(page_index / 64); i++)
                lock_table.Add(empty);
            //if()
        }
        public void setlock(UInt64 addr)
        {
            var addr_ = MemorySelecter.resize(addr);
            Int64 index_all = (Int64)(addr_ / size);
            int index = (int)(index_all / 64);
            int mod = (int)(index_all % 64);
            setbit(mod, index, true);
        }
        public bool get_lock_state(UInt64 addr)
        {
           var  addr_ = MemorySelecter.resize(addr);
            var index = addr_ / size;
            Int32 i = (Int32)( index/ 64);
            int j = (int)(index % 64);
            //  return lock_table[(Int32)(addr / page_index)];
            if ((lock_table[i].ToArray())[j] == TRUE)
                return true;
            return false;
        }
        public void relese_lock(UInt64 addr)
        {
            var addr_ = MemorySelecter.resize(addr);
            var index = addr_ / size;
            Int32 i = (Int32)(index / 64);
            int j = (int)(index % 64);
            //  return lock_table[(Int32)(addr / page_index)];

            var item = lock_table[i].ToArray();
            item[j] = FALSE;
            lock_table[i] = new string(item);
        }


        public void setbit(Int64 index, Int32 i, bool lock_)
        {
            var data = lock_table[i].ToArray();
            char bit = lock_ ? TRUE : FALSE;
            data[index] = bit;
            lock_table[i] = new string(data);

        }

        
    }
}
