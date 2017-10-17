#ifndef PIM_MEMORY
#define PIM_MEMORY

#include "params/PIM_DRAM.hh"
#include "sim/sim_object.hh"
#include "cpu/base.hh"
#include "mem/abstract_mem.hh"
#include "mem/dramsim2.hh"
#include "mem/qport.hh"
#include "arch/generic/tlb.hh"
#include "dev/pimkernel.hh"
#include <queue>
#include <unordered_map>
using namespace std;



class PIM_DRAM : public DRAMSim2
{
public:
    
    PIM_DRAMParams* params;
    DRAMSim2::MemoryPort port;
    PIM_DRAM(PIM_DRAMParams *p);
    uint64_t PIM_AddressReference;
    uint64_t PIM_Regs_Reference;
    uint64_t PIM_Units_Count;
    uint64_t PIM_Processor_Count;
    vector<PIMKernel*> kernels;
    
    unsigned long read_mem_at_address(unsigned long int address);
    unsigned long write_mem_at_address(unsigned long int address, uint8_t value);
    
    void FlushDirtyCaches(MemCmd::PIM_DATA* req);
    
    void readComplete(unsigned id, uint64_t addr, uint64_t cycle) override;
    void writeComplete(unsigned id, uint64_t addr, uint64_t cycle)override;
    
private:
    //System* system;
    BaseCPU* cpu;
    BaseTLB* tlb;

    vector<SimObject*> dirty_caches;
    vector<uint64_t> data;

    
public:
    
    Tick recvAtomic(PacketPtr pkt) override;
    void recvFunctional(PacketPtr pkt) override;
    bool recvTimingReq(PacketPtr pkt) override;
    void recvRespRetry() override;
    void init() override;
};

#endif
