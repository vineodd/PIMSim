
#ifndef __PIM_ADDER_KERNEL__
#define __PIM_ADDER_KERNEL__

#include "sim/sim_object.hh"
#include "params/PIMAdder.hh"
#include "mem/port.hh"
#include "dev/pimkernel.hh"
using namespace std;
class PIMAdder : public PIMKernel
{

public:
    typedef PIMAdderParams Params;
    PIMAdder(const Params *p);

    virtual ~PIMAdder();

    void ADD();
};

#endif // __PIM_KERNEL__
