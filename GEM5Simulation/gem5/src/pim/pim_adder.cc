#include "pim/pim_adder.hh"

PIMAdder::PIMAdder(const PIMAdder::Params *p):
PIMKernel(p)
{

}

PIMKernel::dataType
PIMAdder::doCompute(){
	computing_counts++;
	return (dataType)(data[0]+data[1]);
}

PIMAdder *
PIMAdderParams::create()
{
    return new PIMAdder(this);
}

