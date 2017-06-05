using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.PIM
{
    public interface PIMInterface
    {
        void get_input();
        object get_output();
        void get_output(ref object output);
    }
}
