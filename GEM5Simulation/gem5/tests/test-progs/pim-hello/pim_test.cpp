#include <iostream>
#include <stdio.h>

//Necessary
#include <gem5/m5ops.h>

int main(){
	std::cout<<"(pim_test.cpp) : Use PIM to calculate the sum from 1 to 100"<<std::endl;
	uint64_t sum=0;
	for(uint64_t i=1;i<=100;i++){
		//sum+=i;
		printf("(pim_test.cpp) : PIM [0x%lu] [0x%lu] -> [0x%lu]\n",(uint64_t)&sum,(uint64_t)&i,(uint64_t)&sum);
		PIM((uint64_t)&sum,(uint64_t)&i,(uint64_t)&sum);
	}
	std::cout<<"(pim_test.cpp) : Result: "<<std::dec<<sum<<std::endl;
}
		
