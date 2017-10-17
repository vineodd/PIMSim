/* 
 * _HMC_LINKS_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * HMC LINK CONFIGURATION FUNCTIONS
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"



/* ----------------------------------------------------- HMCSIM_LINK_CONFIG */
/* 
 * HMCSIM_LINK_CONFIG
 * 
 */
extern int	hmcsim_link_config( 	struct hmcsim_t *hmc, 
					uint32_t src_dev,
					uint32_t dest_dev,
					uint32_t src_link,
					uint32_t dest_link,
					hmc_link_def_t type )
{
	/* sanity check */
	if( hmc == NULL ){ 
		return -1;
	}

	if( src_dev > (hmc->num_devs+1) ){
		return -1;
	}	

	if( dest_dev >= hmc->num_devs ){
		return -1;
	}	
	if( src_link >= hmc->num_links) {
		return -1;
	}

	if( dest_link >= hmc->num_links) {
		return -1;
	}

	/* 
	 * ok, we're sane.. setup the links
	 * 
	 */

	if( type == HMC_LINK_HOST_DEV ){ 

		/* 
		 * host to device link 
	 	 *
		 */


		hmc->devs[ dest_dev ].links[ dest_link ].src_cub 	= (hmc->num_devs+1);
		hmc->devs[ dest_dev ].links[ dest_link ].dest_cub 	= dest_dev;
		hmc->devs[ dest_dev ].links[ dest_link ].type 		= HMC_LINK_HOST_DEV;

	}else{
		/* 
		 * device to device link 
		 * dest && src must be different; no loops
		 *
		 */
		
		if( dest_dev == src_dev ){ 
			return -1;
		}

		/* 
		 * config the src 
		 *
		 */
		hmc->devs[ src_dev ].links[ src_link ].src_cub		= src_dev;
		hmc->devs[ src_dev ].links[ src_link ].dest_cub		= dest_dev;
		hmc->devs[ src_dev ].links[ src_link ].type		= HMC_LINK_DEV_DEV;

		/* 
		 * config the dest
		 */
		hmc->devs[ dest_dev ].links[ dest_link ].src_cub	= dest_dev;
		hmc->devs[ dest_dev ].links[ dest_link ].dest_cub 	= src_dev;
		hmc->devs[ dest_dev ].links[ dest_link ].type		= HMC_LINK_HOST_DEV;
	}

	return 0;
}

/* EOF */
