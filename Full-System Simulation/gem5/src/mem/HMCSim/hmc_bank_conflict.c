/* 
 * _HMC_BANK_CONFLICT_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * HMC BANK CONFLICT MODEL
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int	hmcsim_trace( struct hmcsim_t *hmc, char *str );
extern int	hmcsim_util_decode_bank( struct hmcsim_t *hmc, 
					uint32_t dev, 
					uint32_t bsize, 
					uint64_t addr, 
					uint32_t *bank );
extern int	hmcsim_trace_bank_conflict( 	struct hmcsim_t *hmc, 
						uint32_t dev, 
						uint32_t quad, 
						uint32_t vault, 
						uint32_t bank, 
						uint64_t addr1 );




/* ----------------------------------------------------- HMCSIM_PROCESS_BANK_CONFLICTS */
/* 
 * HMCSIM_PROCESS_BANK_CONFLICTS
 * 
 */
extern int	hmcsim_process_bank_conflicts( struct hmcsim_t *hmc, 
						uint32_t dev, 
						uint32_t quad, 
						uint32_t vault, 
						uint64_t *addr, 
						uint64_t num_valid )
{
	/* vars */
	uint32_t i		= 0;
	uint32_t bsize		= 0;
	uint64_t bitarray	= 0x00ll;
	uint32_t bank[HMC_MAX_BANKS];
	/* ---- */

	/* 
	 * sanity check 
	 *
	 */
	if( hmc == NULL ){ 
		return -1;
	}

	if( addr == NULL ){ 
		return -1;
	}

	/* 
	 * get the block size 
	 * 
	 */
	hmcsim_util_get_max_blocksize( hmc, dev, &bsize );

	/* 
	 * walk the addresses and get the banks
	 * 
	 */
	for( i=0; i<num_valid; i++ ){ 

		hmcsim_util_decode_bank( hmc, dev, bsize, addr[i], &(bank[i]) );
	}	

	/* 
	 * map the banks to the bit array
	 * 
	 */		
	for( i=0; i<num_valid; i++ ){ 
		

		/*
	 	 * check to see if the bank is set 
		 * 
		 */
		if( (bitarray & (uint64_t)(1<<(uint64_t)(bank[i]))) > 0 ){
		
			/* 
			 * BANK CONFLICT!!
		 	 * 
			 * Mark the ancillary address
			 * and print the trace
			 * 
		 	 */

#ifdef HMC_DEBUG
			HMCSIM_PRINT_TRACE( "FOUND A BANK CONFLICT" );
#endif
	
			hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[i].valid = HMC_RQST_CONFLICT;
	
			if( (hmc->tracelevel & HMC_TRACE_BANK) > 0 ) {
		
				hmcsim_trace_bank_conflict( 	hmc, 
								dev, 
								quad, 
								vault, 
								bank[i], 
								addr[i] );	
	
			}
	
		}else{ 
			/* 
			 * NO CONFLICT
			 * OR' in the bit
			 * 
			 */
#ifdef HMC_DEBUG
			HMCSIM_PRINT_TRACE( "NO BANK CONFLICT FOUND" );
#endif

			bitarray |= (uint64_t)(1<<(uint64_t)(bank[i]));
		}

	}

#ifdef HMC_DEBUG
	HMCSIM_PRINT_INT_TRACE( "COMPLETED BANK CONFLICT ANALYSIS FOR VAULT", (int)(vault) );
#endif

	return 0;
}

/* EOF */
