/* 
 * _HMC_RECV_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * MEMORY RECV FUNCTIONS
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int      hmcsim_util_zero_packet( struct hmc_queue_t *queue );

/* ----------------------------------------------------- HMCSIM_RECV */
/* 
 * HMCSIM_RECV
 * 
 */
extern int	hmcsim_recv( struct hmcsim_t *hmc, uint32_t dev, uint32_t link, uint64_t *packet )
{
	/* vars */
	uint32_t target	= hmc->xbar_depth+1;
	uint32_t i	= 0;
	uint32_t cur	= 0;
	/* ---- */

	if( hmc == NULL ){ 
		return -1;
	}

	if( dev > hmc->num_devs ){
		return -1;
	}		

	if( link > hmc->num_links ) {
		return -1;
	}

#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "CHECKING LINK FOR CONNECTIVITY" );
	HMCSIM_PRINT_INT_TRACE( "DEV", (int)(dev) );
	HMCSIM_PRINT_INT_TRACE( "LINK", (int)(link) );
#endif	

	if( hmc->devs[dev].links[link].type != HMC_LINK_HOST_DEV ){
		/* 
	 	 * oops, I'm not connected to this link 
		 *
		 */
		return -1;	
	}

	/* 
	 * ok, sanity check complete; 
	 * go walk the response queues associated
	 * with the target device+link combo
	 * 
	 * If nothing is found, return a stall signal
	 * 
	 */	
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "CHECKING LINK FOR VALID RESPONSE" );
#endif

	cur = hmc->xbar_depth-1;
	
	for( i=0; i<hmc->xbar_depth; i++ ){ 

#ifdef HMC_DEBUG
		HMCSIM_PRINT_INT_TRACE( "CHECKING XBAR RESPONSE QUEUE SLOT", (int)(cur) );

		HMCSIM_PRINT_ADDR_TRACE( "xbar_rsp", 
					(uint64_t)(hmc->devs[dev].xbar[link].xbar_rsp) );
		HMCSIM_PRINT_ADDR_TRACE( "xbar_rsp[cur]", 
					(uint64_t)&(hmc->devs[dev].xbar[link].xbar_rsp[cur]) );
#endif

		if( hmc->devs[dev].xbar[link].xbar_rsp[cur].valid == HMC_RQST_VALID ){
#ifdef HMC_DEBUG
			HMCSIM_PRINT_INT_TRACE( "FOUND A VALID RESPONSE PACKET AT SLOT", cur );
#endif
			target = cur;
		}

		cur--;
	}

	if( target ==  hmc->xbar_depth+1 ){
		/* 
		 * no responses found
		 * 
		 */
		return HMC_STALL;
	}

#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "VALID RESPONSE FOUND" );
#endif

	/* -- else, pull the response and clear the queue entry */
	for( i=0; i<HMC_MAX_UQ_PACKET; i++ ){
		packet[i]	= hmc->devs[dev].xbar[link].xbar_rsp[target].packet[i];
	}

	hmcsim_util_zero_packet( &(hmc->devs[dev].xbar[link].xbar_rsp[target]) );

	return 0;
}

/* EOF */
