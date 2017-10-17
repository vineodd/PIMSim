/*
 * _HMC_INIT_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * INITIALIZATION ROUTINES
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"



/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int	hmcsim_allocate_memory( struct hmcsim_t *hmc );
extern int	hmcsim_free_memory( struct hmcsim_t *hmc );
extern int	hmcsim_config_devices( struct hmcsim_t *sim );
extern int	hmc_reset_device( struct hmcsim_t *hmc, uint32_t dev );


/* ----------------------------------------------------- HMCSIM_INIT */
/*
 * HMCSIM_INIT
 *
 */
extern int hmcsim_init(	struct hmcsim_t *hmc,
			uint32_t num_devs,
			uint32_t num_links,
			uint32_t num_vaults,
			uint32_t queue_depth,
			uint32_t num_banks,
			uint32_t num_drams,
			uint32_t capacity,
			uint32_t xbar_depth )
{
	/* vars */
	uint32_t i	= 0;
	/* ---- */

	/*
	 * ensure we have a good structure
	 *
	 */
	if( hmc == NULL ){
		return -1;
	}

	/*
	 * sanity check the args
	 *
	 */
	if( (num_devs > HMC_MAX_DEVS) || (num_devs < 1) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_links < HMC_MIN_LINKS) || (num_links > HMC_MAX_LINKS) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_vaults < HMC_MIN_VAULTS) || (num_vaults > HMC_MAX_VAULTS) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_banks < HMC_MIN_BANKS) || (num_banks > HMC_MAX_BANKS) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_drams < HMC_MIN_DRAMS) || (num_drams > HMC_MAX_DRAMS) ){ 
		return HMC_ERROR_PARAMS;
	}else if( (capacity < HMC_MIN_CAPACITY) || (capacity > HMC_MAX_CAPACITY) ){
		return HMC_ERROR_PARAMS;
	}else if( (queue_depth < HMC_MIN_QUEUE_DEPTH ) || (queue_depth > HMC_MAX_QUEUE_DEPTH ) ){
		return HMC_ERROR_PARAMS;
	}else if( (xbar_depth < HMC_MIN_QUEUE_DEPTH ) || (xbar_depth > HMC_MAX_QUEUE_DEPTH ) ){
		return HMC_ERROR_PARAMS;
	}
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "PASSED LEVEL1 INIT SANITY CHECK" );
#endif

	/* 
	 * look deeper to make sure the default addressing works
	 * and the vault counts
	 * 
	 */
	if( (num_banks != 8) && (num_banks != 16) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_links != 4) && (num_links != 8) ){
		return HMC_ERROR_PARAMS;
	}else if( (num_vaults/num_links) != 4 ){
		/* always maintain 4 vaults per quad, or link */
		return HMC_ERROR_PARAMS;
	}else if( (capacity%2) != 0 ){
		return HMC_ERROR_PARAMS;
	}
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "PASSED LEVEL2 INIT SANITY CHECK" );
#endif

	/* 
	 * go deeper still...
	 * 
	 */
	if( (capacity == 2) && ( (num_banks == 16) || (num_links==8) ) ){
		return HMC_ERROR_PARAMS;
	}else if( (capacity == 4) && ( (num_banks == 16) && (num_links==8) ) ){
		return HMC_ERROR_PARAMS;
	}else if( (capacity == 8) && ( (num_banks == 8 ) || (num_links==4) ) ){
		return HMC_ERROR_PARAMS;
	}
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "PASSED LEVEL3 INIT SANITY CHECK" );
#endif

	/*
	 * init all the internals
	 *
 	 */
	hmc->tfile	= NULL;
	hmc->tracelevel	= 0x00;

	hmc->num_devs	= num_devs;
	hmc->num_links	= num_links;
	hmc->num_vaults	= num_vaults;
	hmc->num_banks	= num_banks;
	hmc->num_drams	= num_drams;
	hmc->capacity	= capacity;
        hmc->num_cmc    = 0x00;
	hmc->queue_depth= queue_depth;
	hmc->xbar_depth	= xbar_depth;

	hmc->clk	= 0x00ll;

	if( num_links == 4 ){
		hmc->num_quads = 4;
	}else{
		hmc->num_quads = 8;
	}

	/*
	 * pointers
	 */
	hmc->__ptr_devs			= NULL;
	hmc->__ptr_quads		= NULL;
	hmc->__ptr_vaults		= NULL;
	hmc->__ptr_banks		= NULL;
	hmc->__ptr_drams		= NULL;
	hmc->__ptr_links		= NULL;
	hmc->__ptr_xbars		= NULL;
	hmc->__ptr_stor			= NULL;
	hmc->__ptr_xbar_rqst		= NULL;
	hmc->__ptr_xbar_rsp		= NULL;
	hmc->__ptr_vault_rqst		= NULL;
	hmc->__ptr_vault_rsp		= NULL;

	/*
	 *
	 * allocate memory
	 *
	 */
	if( hmcsim_allocate_memory( hmc ) != 0 ){
		/*
		 * probably ran out of memory
		 *
		 */
#ifdef HMC_DEBUG
		HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE INTERNAL MEMORY" );
#endif

		return -1;
	}

	/*
	 * configure all the devices
	 *
	 */
	if( hmcsim_config_devices( hmc ) != 0 ){
#ifdef HMC_DEBUG
		HMCSIM_PRINT_TRACE( "FAILED TO CONFIGURE THE INTERNAL DEVICE STRUCTURE" );
#endif

		hmcsim_free_memory( hmc );
		return -1;
	}
	/*
	 * warm reset all the devices
	 *
	 */
	for( i=0; i<hmc->num_devs; i++ ) {

		//hmc_reset_device( hmc, i );
	}

	return 0;
}

/* EOF */
