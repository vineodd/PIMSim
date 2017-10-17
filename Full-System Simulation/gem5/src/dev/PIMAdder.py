

from m5.params import *
from m5.proxy import *
from m5.SimObject import SimObject
from m5.objects.PIMKernel import PIMKernel

class PIMAdder(PIMKernel):
    type = 'PIMAdder'
    cxx_header = "dev/pimadder.hh"
    name = Param.String("ADDer","PIM Unit name.")
    latency = Param.Int("1", "PIM Unit computation delay cycles.")
    input=Param.Int(4, "num of inputs")
    output=Param.Int(1, "num of outputs")
    

