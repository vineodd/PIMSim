/*
 * _HMC_MEM_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * MEMORY ALLOCATION/FREE FUNCTIONS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

extern int hmcsim_free_cmc( struct hmcsim_t *hmc );
extern int hmcsim_readmem( struct hmcsim_t *hmc,
                            uint64_t addr,
                            uint64_t *data,
                            uint32_t len);
extern int hmcsim_writemem( struct hmcsim_t *hmc,
                            uint64_t addr,
                            uint64_t *data,
                            uint32_t len);


/* ----------------------------------------------------- HMCSIM_FREE_MEMORY */
/*
 * HMCSIM_FREE_MEMORY
 *
 */
extern int	hmcsim_free_memory( struct hmcsim_t *hmc )
{
	if( hmc == NULL ){
		return -1;
	}

        if( hmc->cmcs != NULL ){
                hmcsim_free_cmc( hmc );
                free( hmc->cmcs );
                hmc->cmcs = NULL;
        }

	if( hmc->__ptr_devs != NULL ){
		free( hmc->__ptr_devs );
		hmc->__ptr_devs = NULL;
	}

	if( hmc->__ptr_quads != NULL ){
		free( hmc->__ptr_quads );
		hmc->__ptr_quads = NULL;
	}

	if( hmc->__ptr_vaults != NULL ){
		free( hmc->__ptr_vaults );
		hmc->__ptr_vaults = NULL;
	}

	if( hmc->__ptr_banks != NULL ){
		free( hmc->__ptr_banks );
		hmc->__ptr_banks = NULL;
	}

	if( hmc->__ptr_drams != NULL ){
		free( hmc->__ptr_drams );
		hmc->__ptr_drams = NULL;
	}

	if( hmc->__ptr_links != NULL ){
		free( hmc->__ptr_links );
		hmc->__ptr_links = NULL;
	}

	if( hmc->__ptr_xbars != NULL ){
		free( hmc->__ptr_xbars );
		hmc->__ptr_xbars = NULL;
	}

	if( hmc->__ptr_stor != NULL ){
		free( hmc->__ptr_stor );
		hmc->__ptr_stor = NULL;
	}

	if( hmc->__ptr_xbar_rqst  != NULL ){
		free( hmc->__ptr_xbar_rqst );
		hmc->__ptr_xbar_rqst = NULL;
	}

	if( hmc->__ptr_xbar_rsp != NULL ){
		free( hmc->__ptr_xbar_rsp );
		hmc->__ptr_xbar_rsp = NULL;
	}

	if( hmc->__ptr_vault_rqst != NULL ){
		free( hmc->__ptr_vault_rqst );
		hmc->__ptr_vault_rqst = NULL;
	}

	if( hmc->__ptr_vault_rsp != NULL ){
		free( hmc->__ptr_vault_rsp );
		hmc->__ptr_vault_rsp = NULL;
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_ALLOCATE_MEMORY */
/*
 * HMCSIM_ALLOCATE_MEMORY
 *
 */
extern int	hmcsim_allocate_memory( struct hmcsim_t *hmc )
{
	if( hmc == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "HMC STRUCTURE IS NULL" );
#endif
		return -1;
	}

        hmc->cmcs = malloc( sizeof( struct hmc_cmc_t ) * HMC_MAX_CMC );
        if( hmc->cmcs == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE cmcs" );
#endif
          return -1;
        }


	hmc->__ptr_devs	= malloc( sizeof( struct hmc_dev_t ) * hmc->num_devs );
	if( hmc->__ptr_devs == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_devs" );
#endif
		return -1;
	}

	hmc->__ptr_quads = malloc( sizeof( struct hmc_quad_t ) * hmc->num_devs * hmc->num_quads );
	if( hmc->__ptr_quads == NULL ) { 
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_quads" );
#endif
		return -1;
	}

	hmc->__ptr_vaults = malloc( sizeof( struct hmc_vault_t ) * hmc->num_devs * hmc->num_vaults );
	if( hmc->__ptr_vaults == NULL ){ 
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_vaults" );
#endif
		return -1;
	}

	hmc->__ptr_banks = malloc( sizeof( struct hmc_bank_t ) 
					* hmc->num_devs * hmc->num_vaults * hmc->num_banks );
	if( hmc->__ptr_banks == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_banks" );
#endif
		return -1;
	}

#if 0
	hmc->__ptr_drams = malloc( sizeof( struct hmc_dram_t ) 
					* hmc->num_devs * hmc->num_vaults * hmc->num_banks 
					* hmc->num_drams );
	if( hmc->__ptr_drams == NULL ){ 
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_drams" );
#endif
		return -1;
	}
#endif
	hmc->__ptr_links = malloc( sizeof( struct hmc_link_t ) * hmc->num_devs * hmc->num_links );
	if( hmc->__ptr_links == NULL ){ 
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_links" );
#endif
		return -1;
	}

	hmc->__ptr_xbars = malloc( sizeof( struct hmc_xbar_t ) * hmc->num_devs * hmc->num_links );
	if( hmc->__ptr_xbars == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_xbars" );
#endif
		return -1;
	}

#ifdef HMC_ALLOC_MEM
	hmc->__ptr_stor = malloc( sizeof( uint64_t ) * hmc->num_devs * hmc->capacity * HMC_1GB );
	if( hmc->__ptr_stor == NULL ){ 
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_stor" );
#endif
                printf( "DUMPING OUT; CAN'T ALLOC MEMORY\n" );
		return -1;
	}

        hmc->__ptr_end  = (uint64_t *)( hmc->__ptr_stor
                                        + (sizeof( uint64_t ) *
                                           hmc->num_devs *
                                           hmc->capacity * HMC_1GB) );
#endif

	hmc->__ptr_xbar_rqst = malloc( sizeof( struct hmc_queue_t ) * hmc->num_devs 
								* hmc->xbar_depth 
								* hmc->num_links );
	if( hmc->__ptr_xbar_rqst == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_xbar_rqst" );
#endif
		return -1;
	}

	hmc->__ptr_xbar_rsp = malloc( sizeof( struct hmc_queue_t ) * hmc->num_devs 
								* hmc->xbar_depth 
								* hmc->num_links );
	if( hmc->__ptr_xbar_rsp == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_xbar_rsp" );
#endif
		return -1;
	}


	hmc->__ptr_vault_rqst = malloc( sizeof( struct hmc_queue_t ) * hmc->num_devs * hmc->num_vaults * hmc->queue_depth );
	if( hmc->__ptr_vault_rqst == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_vault_rqst" );
#endif
		return -1;
	}

	hmc->__ptr_vault_rsp = malloc( sizeof( struct hmc_queue_t ) * hmc->num_devs * hmc->num_vaults * hmc->queue_depth );
	if( hmc->__ptr_vault_rsp == NULL ){
#ifdef HMC_DEBUG
                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_vault_rsp" );
#endif
		return -1;
	}

        hmc->readmem  = &(hmcsim_readmem);
        hmc->writemem = &(hmcsim_writemem);

	return 0;
}

/* ----------------------------------------------------- HMCSIM_FREE */
/*
 * HMCSIM_FREE
 *
 */
extern int	hmcsim_free( struct hmcsim_t *hmc )
{
	if( hmc->tfile != NULL ){
		fflush( hmc->tfile );
	}

	return hmcsim_free_memory( hmc );
}

/* EOF */
