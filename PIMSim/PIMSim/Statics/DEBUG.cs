using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace SimplePIM.Statics
{
    public static class DEBUG
    {
        private static StreamWriter sw = null;

        public static void set_writer(ref StreamWriter sw_)
        {
            sw = sw_;
        }
        public static void WriteLine(string s="")
        {
            if (sw == null)
            {
                Console.WriteLine( s);
            }
            else
            {
                sw.WriteLine( s);
            }
        }
        public static void Write(string s)
        {
            if (sw == null)
            {
                Console.Write(s);
            }
            else
            {
                sw.Write(s);
            }
        }

        public static void Assert(bool s)
        {
            System.Diagnostics.Debug.Assert(s);
        }

        public static void Error(string s)
        {
            if (sw == null)
            {
                Console.WriteLine("ERROR: " + s);
            }
            else
            {
                sw.WriteLine("ERROR: " + s);
            }
        }
    }
}
