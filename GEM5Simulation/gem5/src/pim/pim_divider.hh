#ifndef __PIM_ADDER__
#define __PIM_ADDER__

#include "pim/pim_kernel.hh"
#include "params/PIMDivider.hh"

class PIMDivider : public PIMKernel
{
public:
    
    typedef PIMDividerParams Params;
    PIMDivider(const Params *p);
    virtual dataType doCompute() override;

};


#endif
