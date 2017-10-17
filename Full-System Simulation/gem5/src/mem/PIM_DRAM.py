from m5.params import *
from m5.SimObject import *
from DRAMSim2 import *
import PIMKernel

class PIM_DRAM(DRAMSim2):
    type = 'PIM_DRAM'
    cxx_header = "mem/pim_dram.hh"
    port = SlavePort("Slave port")
    PIM_AddressReference = Param.Int(0, "PIM Reference ADDRESS");
    PIM_Regs_Reference = Param.Int(0, "PIM Reference ADDRESS");
    PIM_Units_Count = Param.Int(1, "PIM Reference ADDRESS");
    PIM_Processor_Count = Param.Int(0, "PIM Reference ADDRESS");
    kernels = VectorParam.PIMKernel([],"")

