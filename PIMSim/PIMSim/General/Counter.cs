using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.General
{
    public class Counter
    {
        private int count;
        private int max;
        public Counter(int start_,int max_)
        {
            if (start_ < 0)
                throw new ArgumentException();
            if (max_ < start_)
                throw new ArgumentException();
            count = start_;
            max = max_;
        }
        public bool WaitOne()
        {
            if (max < 0)
                throw new ArgumentException();
            if (max == 0)
            {
                return false;
            }
            max--;
            if (max < 0)
                throw new ArgumentException();
            return true;
        }
        public void Reset(int start_)
        {
            if (start_ < 0)
                throw new ArgumentException();
            if (max < start_)
                throw new ArgumentException();
            count = start_;
        }
        public void Reset()
        {
            count = max;
        }
        public void Release()
        {
            if (count + 1 > max)
                return;
            max++;
        }
        public bool Zero => count == 0 && max != 0;
    }
}
