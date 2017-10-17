using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.General
{
    public class FlagBit
    {
        public string _flags;

        public FlagBit()
        {
            _flags = "";
        }
        public FlagBit(UInt32 flags)
        {
            _flags = tostring(flags);
            
        }
        public FlagBit(UInt16 flags)
        {
            _flags = tostring(flags);
        }
        public FlagBit(UInt64 flags)
        {

            _flags = tostring(flags);
        }
        public FlagBit(int bit)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bit; i++)
                sb.Append("0");
            _flags = sb.ToString();
        }
        public FlagBit(int bits,bool b)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bits; i++)
                sb.Append(b ? "1" : "0");
            _flags = sb.ToString();

        }
        public string tostring(UInt32 flags)
        {
           return  Convert.ToString(flags, 2);
        }
        public string tostring(UInt64 flags)
        {
            string s = string.Empty;

            byte[] bytes = BitConverter.GetBytes(flags);

            for (int k = bytes.Length - 1; k >= 0; k--)
            {
                s += Convert.ToString(bytes[k], 2);
            }
            return s;
        }
        public string tostring(UInt16 flags)
        {
            return Convert.ToString(flags, 2);
        }
        public FlagBit(string flag)
        {
            _flags = flag;
        }


        public bool isSet() { return _flags.Any(x => x == '1'); }

        public bool isSet(UInt32 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            // return (_flags & flags) > 0;
            return sb.ToString().Any(x => x == '1');
        }
        public bool isSet(UInt16 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            // return (_flags & flags) > 0;
            return sb.ToString().Any(x => x == '1');
        }
        public bool isSet(UInt64 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            // return (_flags & flags) > 0;
            return sb.ToString().Any(x => x == '1');
        }

        public bool isSet(string flags)
        {
            string s = flags;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            // return (_flags & flags) > 0;
            return sb.ToString().Any(x => x == '1');
        }
        public string And(char a,char b)
        {
            if (a == '1' && b == '1')
                return "1";
            return "0";
        }
        public string Or(char a, char b)
        {
            if (a == '0' && b == '0')
                return "0";
            return "1";
        }
        public string Not(char s)
        {
            switch (s)
            {
                case '1':
                    return "0";
                case '0':
                    return "1";
                default:
                    return "0";
            }
        }


        public bool allSet()
        {
          return   !String.Join("", _flags.Select(x => Not(x)).ToArray()).Any(x => x == '1');
            //  return !(~_flags == 0);
        }
        public bool allSet(UInt32 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !String.Join("", sb.ToString().Select(x => Not(x)).ToArray()).Any(x => x == '1');
           // return (_flags & flags) == flags;
        }
        public bool allSet(UInt16 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !String.Join("", sb.ToString().Select(x => Not(x)).ToArray()).Any(x => x == '1');
            // return (_flags & flags) == flags;
        }
        public bool allSet(UInt64 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !String.Join("", sb.ToString().Select(x => Not(x)).ToArray()).Any(x => x == '1');
            // return (_flags & flags) == flags;
        }

        public bool allSet(string flags)
        {
            string s = flags;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !String.Join("", sb.ToString().Select(x => Not(x)).ToArray()).Any(x => x == '1');
            // return (_flags & flags) == flags;
        }



        public bool noneSet()
        {
            return !_flags.Any(x => x == '1');
           // return _flags == 0;
        }
        public bool noneSet(UInt32 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !sb.ToString().Any(x => x == '1');
            //   { return (_flags & flags) == 0; }
        }
        public bool noneSet(UInt16 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !sb.ToString().Any(x => x == '1');
            //   { return (_flags & flags) == 0; }
        }
        public bool noneSet(UInt64 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !sb.ToString().Any(x => x == '1');
            //   { return (_flags & flags) == 0; }
        }
        public bool noneSet(string flags)
        {
            string s = flags;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            return !sb.ToString().Any(x => x == '1');
            //   { return (_flags & flags) == 0; }
        }
        public void clear()
        {
            // { _flags = 0; }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
                sb.Append("0");
            _flags = sb.ToString();
        }
        public void clear(UInt32 flags)
        {
            string s = String.Join("", tostring(flags).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            //  { _flags &= ~flags; }
        }
        public void clear(UInt16 flags)
        {
            string s = String.Join("", tostring(flags).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            //  { _flags &= ~flags; }
        }
        public void clear(UInt64 flags)
        {
            string s = String.Join("", tostring(flags).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            //  { _flags &= ~flags; }
        }

        public void clear(string flags)
        {
            string s = String.Join("", flags.Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            //  { _flags &= ~flags; }
        }

        public void set(UInt32 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                sb.Append(Or(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            // { _flags |= flags; }
        }
        public void set(UInt16 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(Or(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            // { _flags |= flags; }
        }
        public void set(UInt64 flags)
        {
            string s = tostring(flags);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 64; i++)
            {
                sb.Append(Or(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            // { _flags |= flags; }
        }
        public void set(string flags)
        {
            string s = flags;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < flags.Count(); i++)
            {
                sb.Append(Or(_flags[i], s[i]));
            }
            _flags = sb.ToString();
            // { _flags |= flags; }
        }

        public void set(UInt32 f, bool val)
        {
            string s = String.Join("", tostring(f).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();
            string s1 = tostring(f);
            sb.Clear();
            for (int i = 0; i < s.Count(); i++)
            {
                sb.Append(Or(s[i], val? s1[i] :'0'));
            }
            _flags = sb.ToString();

            //{ _flags = (_flags & ~f) | (val ? f : 0); }
        }
        public void set(UInt16 f, bool val)
        {
            string s = String.Join("", tostring(f).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();
            string s1 = tostring(f);
            sb.Clear();
            for (int i = 0; i < s.Count(); i++)
            {
                sb.Append(Or(s[i], val ? s1[i] : '0'));
            }
            _flags = sb.ToString();

            //{ _flags = (_flags & ~f) | (val ? f : 0); }
        }
        public void set(UInt64 f, bool val)
        {
            string s = String.Join("", tostring(f).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();
            string s1 = tostring(f);
            sb.Clear();
            for (int i = 0; i < s.Count(); i++)
            {
                sb.Append(Or(s[i], val ? s1[i] : '0'));
            }
            _flags = sb.ToString();

            //{ _flags = (_flags & ~f) | (val ? f : 0); }
        }

        public void set(string f, bool val)
        {
            string s = String.Join("", f.Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();
            string s1 = f;
            sb.Clear();
            for (int i = 0; i < s.Count(); i++)
            {
                sb.Append(Or(s[i], val ? s1[i] : '0'));
            }
            _flags = sb.ToString();

            //{ _flags = (_flags & ~f) | (val ? f : 0); }
        }


        public void update(UInt32 flags, UInt32 mask)
        {
            string s = String.Join("", tostring(mask).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();

            string s1 = tostring(mask);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s1[i]));
            }
            s1 = sb.ToString();

            //      string s = tostring(flags);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(Or(s[i], s1[i]));
            }
            _flags = sb.ToString();

          //  _flags = (_flags & ~mask) | (flags & mask);
        }
        public void update(UInt16 flags, UInt16 mask)
        {
            string s = String.Join("", tostring(mask).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();

            string s1 = tostring(mask);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s1[i]));
            }
            s1 = sb.ToString();

            //      string s = tostring(flags);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(Or(s[i], s1[i]));
            }
            _flags = sb.ToString();

            //  _flags = (_flags & ~mask) | (flags & mask);
        }
        public void update(UInt64 flags, UInt64 mask)
        {
            string s = String.Join("", tostring(mask).Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();

            string s1 = tostring(mask);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s1[i]));
            }
            s1 = sb.ToString();

            //      string s = tostring(flags);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(Or(s[i], s1[i]));
            }
            _flags = sb.ToString();

            //  _flags = (_flags & ~mask) | (flags & mask);
        }
        public void update(string flags, string mask)
        {
            string s = String.Join("", mask.Select(x => Not(x)).ToArray());

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s[i]));
            }
            s = sb.ToString();

            string s1 = mask;
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(And(_flags[i], s1[i]));
            }
            s1 = sb.ToString();

            //      string s = tostring(flags);
            sb.Clear();
            for (int i = 0; i < _flags.Count(); i++)
            {
                sb.Append(Or(s[i], s1[i]));
            }
            _flags = sb.ToString();

            //  _flags = (_flags & ~mask) | (flags & mask);
        }

        public static implicit operator UInt32(FlagBit a)
        {
            return Convert.ToUInt32(a._flags, 2);
        }
        public static implicit operator UInt16(FlagBit a)
        {
            return Convert.ToUInt16(a._flags, 2);
        }
        public static implicit operator UInt64(FlagBit a)
        {
            return Convert.ToUInt64(a._flags, 2);
        }
        public static implicit operator string(FlagBit a)
        {
            return a._flags;
        }

        public char this[int index]
        {
            get
            {
                return _flags[index];

            }
        }
    }
}
