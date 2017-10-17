//
//  pim_test.c
//  
//
//  Created by 徐晨 on 2017/9/8.
//
//

#include <gem5/m5ops.h>

int main()
{
    uint64_t a=0;
	uint64_t b=0;
	uint64_t c=9;
	uint64_t d=12;
	uint64_t out=0;
	uint64_t id=0;

    PIMKernel((uint64_t)&a,(uint64_t)&b,(uint64_t)&c,(uint64_t)&d,(uint64_t)&out,id);
    

    return 0;
}
