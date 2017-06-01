using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePIM.General
{
    public class Register 
    {
        public string name;
        public object value;
        public Register (string name_)
        {
            name = name_;
        }
        public Register(string name_,object value_)
        {
            name = name_;
            value = value_;
        }
    }
}
