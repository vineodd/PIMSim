#include "pim/pim_divider.hh"

PIMDivider::PIMDivider(const PIMDivider::Params *p):
PIMKernel(p)
{

}

PIMKernel::dataType
PIMDivider::doCompute(){
	computing_counts++;
	return (dataType)(data[0]/data[1]);
}

PIMDivider *
PIMDivider::create()
{
    return new PIMDivider(this);
}

