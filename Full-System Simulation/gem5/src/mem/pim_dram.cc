#include "mem/pim_dram.hh"
#include "sim/system.hh"
#include "debug/Drain.hh"
#include "base/callback.hh"
#include "base/trace.hh"
#include "debug/PIM.hh"
#include "mem/cache/cache.hh"
#include "DRAMSim2/Callback.h"

#include <iostream>
using namespace std;
PIM_DRAM::PIM_DRAM(PIM_DRAMParams *p) :
    DRAMSim2(p),
    params(p),
    port(name() + ".port", *this),
    PIM_AddressReference(p->PIM_AddressReference),
    PIM_Regs_Reference(p->PIM_Regs_Reference),
    PIM_Units_Count(p->PIM_Units_Count),
    PIM_Processor_Count(p->PIM_Processor_Count),
    kernels(p->kernels),
    //system(nullptr),
    cpu(nullptr),
    tlb(nullptr)

{
    
}

PIM_DRAM*
PIM_DRAMParams::create()
{
    return new PIM_DRAM(this);
}

void
PIM_DRAM::init()
{
    DRAMSim2::init();

    //AbstractMemory::init();
    //AbstractMemory::init();
    //DRAMSim2::port.memory=(DRAMSim2&)(*this);
    
    if (system()->cacheLineSize() != wrapper.burstSize())
        fatal("DRAMSim2 burst size %d does not match cache line size %d\n",
              wrapper.burstSize(), system()->cacheLineSize());
    DRAMSim::TransactionCompleteCB* read_cb =
    new DRAMSim::Callback<PIM_DRAM, void, unsigned, uint64_t, uint64_t>(
                                                                        this, &PIM_DRAM::readComplete);
    DRAMSim::TransactionCompleteCB* write_cb =
    new DRAMSim::Callback<PIM_DRAM, void, unsigned, uint64_t, uint64_t>(
                                                                        this, &PIM_DRAM::writeComplete);
    wrapper.setCallbacks(read_cb, write_cb);
    
    // Register a callback to compensate for the destructor not
    // being called. The callback prints the DRAMSim2 stats.
    Callback* cb = new MakeCallback<DRAMSim2Wrapper,
    &DRAMSim2Wrapper::printStats>(wrapper);
    registerExitCallback(cb);
    
    
    
    DPRINTF(PIM, "PIMMemory [init] : Init PIM Device. \n");
    tlb=nullptr;
    std::vector<ThreadContext *> &tcvec = system()->threadContexts;
    cpu = tcvec[0]->getCpuPtr(); // cpu_id = 0
    assert( cpu );

    SimObject* allobj = SimObject::find("system.l2");
    if ( allobj )
        dirty_caches.push_back(allobj);

    DPRINTF(PIM, "PIMMemory [init] : Search For L2 Cache. \n");
    
    for (int i=0; i<1; i++ )
    {
        string id="";
        string name="";

        // Search for dcache
        name = "system.cpu";
        name += "0";
        name += ".dcache";
        
        SimObject* allobj = SimObject::find(name.c_str());
        if ( allobj )
            dirty_caches.push_back(allobj);
        
    }
    DPRINTF(PIM, "PIMMemory [init] : Search For All L1 Cache. \n");
    
    
}

Tick
PIM_DRAM::recvAtomic(PacketPtr pkt)
{


    if(!pkt->isPIMOperation())
        return DRAMSim2::recvAtomic(pkt);
    else{
        if(pkt->cmd==MemCmd::PIMKernel){
            DPRINTF(PIM, "PIMMemory [recvAtomic] : Recv PIM Kernel Command\n");
            DPRINTF(PIM, "[PIM Kernel] : Input Address 1 : %llx\n",pkt->cmd.pim_data->input1);
            DPRINTF(PIM, "[PIM Kernel] : Input Address 2 : %llx\n",pkt->cmd.pim_data->input2);
            DPRINTF(PIM, "[PIM Kernel] : Input Address 3 : %llx\n",pkt->cmd.pim_data->input3);
            DPRINTF(PIM, "[PIM Kernel] : Input Address 4 : %llx\n",pkt->cmd.pim_data->input4);
            DPRINTF(PIM, "[PIM Kernel] : Output Address 1 : %llx\n",pkt->cmd.pim_data->output1);
            DPRINTF(PIM, "[PIM Kernel] : Invoke Kernel ID : 0\n");
            FlushDirtyCaches(pkt->cmd.pim_data);
            data.push_back(pkt->cmd.pim_data->input1);
            data.push_back(pkt->cmd.pim_data->input2);
            data.push_back(pkt->cmd.pim_data->input3);
            data.push_back(pkt->cmd.pim_data->input4);
            DPRINTF(PIM, "PIMMemory : Create PIM reads\n");
          //  data.push_back(input1);
        }
        return DRAMSim2::recvAtomic(pkt);
    }
}


void PIM_DRAM::readComplete(unsigned id, uint64_t addr, uint64_t cycle)
{
    std::cout<<"void PIM_DRAM::readComplete(unsigned id, uint64_t addr, uint64_t cycle)"<<std::endl;
    DRAMSim2::readComplete(id, addr, cycle);

        DPRINTF(PIM, "PIMMemory [PIM OP] : Inject Read Operation.\n");

}

void PIM_DRAM::writeComplete(unsigned id, uint64_t addr, uint64_t cycle)
{
    std::cout<<"void PIM_DRAM::writeComplete(unsigned id, uint64_t addr, uint64_t cycle)"<<std::endl;
    DRAMSim2::writeComplete(id, addr, cycle);
}
bool
PIM_DRAM::recvTimingReq(PacketPtr pkt)
{

    return DRAMSim2::recvTimingReq(pkt);
}

void
PIM_DRAM::recvFunctional(PacketPtr pkt)
{

    DRAMSim2::recvFunctional(pkt);
}

void
PIM_DRAM::recvRespRetry()
{
    DRAMSim2::recvRespRetry();
}


void
PIM_DRAM::FlushDirtyCaches(MemCmd::PIM_DATA* req)
{
    DPRINTF(PIM, "PIMMemory [FlushDirtyCaches] : Try to flush all data PIM used. \n");
    for ( int i=0; i< dirty_caches.size(); i++ )
    {
        Cache* c =(Cache*)dirty_caches[i];
        c->Flush(req->input1);
        c->Flush(req->input2);
        c->Flush(req->input3);
        c->Flush(req->input4);
        c->Flush(req->output1);

    }
}


