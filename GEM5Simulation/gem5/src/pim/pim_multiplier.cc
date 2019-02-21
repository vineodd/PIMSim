#include "pim/pim_multiplier.hh"

PIMMultiplier::PIMMultiplier(const PIMMultiplier::Params *p):
PIMKernel(p)
{

}

PIMKernel::dataType
PIMMultiplier::doCompute(){
	computing_counts++;
	return (dataType)(data[0]*data[1]);
}

PIMMultiplier *
PIMMultiplierParams::create()
{
    return new PIMMultiplier(this);
}

