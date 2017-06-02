using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Configs;

namespace SimplePIM.Memory
{
    public static class MemorySelecter
    {

        public static readonly UInt64 NULL = UInt64.MaxValue;
        public static List<Tuple<UInt64, UInt64, MemObject>> MemoryInfo;
        static MemorySelecter()
        {
            MemoryInfo = new List<Tuple<ulong, ulong, MemObject>>();
        }
        public static uint log2(ulong value)
        {
            uint logbase2 = 0;
            ulong orig = value;
            value >>= 1;
            while (value > 0)
            {
                value >>= 1;
                logbase2++;
            }
            if ((uint)1 << (int)logbase2 < orig) logbase2++;
            return logbase2;
        }
        public static int get_mem_count()
        {
            return MemoryInfo.Count;
        }
        public static void add(int max,ref MemObject obj)
        {
            UInt64 max_ = (UInt64)1 << max;
            if (MemoryInfo.Count<=0)
            {
                MemoryInfo.Add(new Tuple<ulong, ulong, MemObject>(0,  (UInt64)max_-1, obj));
                return;
            }
            var last = MemoryInfo[MemoryInfo.Count - 1];
           
            MemoryInfo.Add(new Tuple<ulong, ulong, MemObject>(last.Item2 + 1, last.Item2 + (UInt64)max_-1, obj));
            
        }
        public static MemObject get_exact_obj(UInt64 address)
        {
            if (address < MemoryInfo[0].Item1 || address > MemoryInfo[MemoryInfo.Count - 1].Item2)
                return null;
            foreach(var item in MemoryInfo)
            {
                if (address >= item.Item1 && address <= item.Item2)
                    return item.Item3;
            }
            return null;
        }
        public static UInt64 get_exact_addr(UInt64 address)
        {
            if (address < MemoryInfo[0].Item1 || address > MemoryInfo[MemoryInfo.Count - 1].Item2)
                return NULL;
            foreach (var item in MemoryInfo)
            {
                if (address >= item.Item1 && address <= item.Item2)
                    return address - item.Item1;
            }
            return NULL;
        }
        public static int get_id(UInt64 address)
        {
            UInt64 res = address;
            if (address < 0)
            {
                //error
                Environment.Exit(1);
                return -1;
            }
            if (address > MemoryInfo[MemoryInfo.Count - 1].Item2)
            {
                //x64_86 system has 48 bit of address bus,but not the whole 48 bits are used
                res = resize(address);
            }
            for (int i = 0; i < MemoryInfo.Count; i++)
            {
                if (res >= MemoryInfo[i].Item1 && res <= MemoryInfo[i].Item2)
                    return i;
            }
            return -1;
        }
        public static UInt64 resize(UInt64 addr)
        {
            int max_ = (int)log2(MemoryInfo[MemoryInfo.Count - 1].Item2);
            string item = toBinary(addr);
            // item=item.Substring()

            item = toBinary(addr).Substring(item.Length - max_);
            return Convert.ToUInt64(item, 2);
        }
        public static UInt64 get_RAM_size()
        {
            return MemoryInfo.Last().Item2+1;
        }
        public static string toBinary(UInt64 i)
        {
            string s = "";
            foreach (var item in BitConverter.GetBytes(i))
            {
                s = Convert.ToString(item, 2).PadLeft(8, '0')+s ;  //可以把2改成8或者16，那么基数就是8和16了。
            }
            return s;
        }
    }
}
