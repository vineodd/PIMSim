#include "dev/pimkernel.hh"
#include "dev/pimadder.hh"
#include "params/PIMAdder.hh"
#include <assert.h>


PIMAdder::PIMAdder(const Params *p):
    PIMKernel(p)
{
    
}


PIMAdder::~PIMAdder(){
    
}

PIMAdder *
PIMAdderParams::create()
{
    return new PIMAdder(this);
}


void
PIMAdder::ADD(){
    uint64_t result=0;
    assert(inputReady());
    
    for(int i=0;i<_input;i++){

        result+=regs[i];
    }
    
    regs[_input]=result;
    
    data_ready[_input]=true;
}
