
#ifndef __PIM_KERNEL__
#define __PIM_KERNEL__

#include "sim/sim_object.hh"
#include "params/PIMKernel.hh"
#include "mem/port.hh"
#include <vector>
using namespace std;
class PIMKernel : public SimObject
{

public:
    typedef PIMKernelParams Params;
    PIMKernel(const Params *p);
    
    //PIM register defination
    typedef uint64_t Regs;
    typedef bool ReadyStatus;
    Regs* regs;
    ReadyStatus* data_ready;
    virtual ~PIMKernel();

    string _name;
    int _latency;
    int _input;
    int _output;
    int _fans;
    bool _active;
    
public:
    bool getReadyStatus(int reg_id);
    virtual bool inputReady();
    virtual bool outputReady();


};

#endif // __PIM_KERNEL__
