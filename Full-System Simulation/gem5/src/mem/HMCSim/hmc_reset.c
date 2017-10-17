/* 
 * _HMC_RESET_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * HMC WARM RESET FUNCTION
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- HMCSIM_RESET_DEVICE */
/* 
 * HMCSIM_RESET_DEVICE
 * 
 */
extern int	hmcsim_reset_device( struct hmcsim_t *hmc, uint32_t dev )
{
	if( hmc == NULL ) {
		return -1;
	}

	return 0;
}

/* EOF */
