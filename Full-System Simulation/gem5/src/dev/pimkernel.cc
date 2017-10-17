#include "dev/pimkernel.hh"
#include <assert.h>


PIMKernel::PIMKernel(const Params *p):
    SimObject(p),
    regs(nullptr),
    data_ready(nullptr),
    _name(p->name),
    _latency(p->latency),
    _input(p->input),
    _output(p->output),
    _fans(p->input+p->output),
    _active(false)
{
    regs=new Regs(p->input+p->output);
    
    data_ready=new ReadyStatus(p->input+p->output);
    for(int i=0;i<p->input+p->output;i++)
    {
        regs[i]=0;
        data_ready[i] = false;
    }
}


PIMKernel::~PIMKernel(){
    if(regs!=nullptr)
        delete regs;
    if(data_ready!=nullptr)
        delete data_ready;
}

PIMKernel *
PIMKernelParams::create()
{
    return new PIMKernel(this);
}

bool
PIMKernel::getReadyStatus(int reg_id)
{
    //basic check of registers
    assert(reg_id>=0&&reg_id<_fans);
    
    if(data_ready[reg_id])
        return true;
    
    return false;
}

bool
PIMKernel::inputReady()
{
    //basic check of registers
    assert(_input>0);
    for(int i=0;i<_input;i++){
        if(!getReadyStatus(i)){
            return false;
        }
    }
    return true;
}

bool
PIMKernel::outputReady(){
    //basic check of registers
    assert(_output>0);
    for(int i=0;i<_output;i++){
        if(!getReadyStatus(i+_input)){
            return false;
        }
    }
    return true;
}
