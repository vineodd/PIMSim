/*
 * _HMC_CLOCK_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * CLOCK FUNCTIONS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int hmcsim_trace( struct hmcsim_t *hmc, char *str );
extern int hmcsim_trace_stall( 	struct hmcsim_t *hmc,
				uint32_t dev,
				uint32_t quad,
				uint32_t vault,
				uint32_t src,
				uint32_t dest,
				uint32_t link,
				uint32_t slot,
				uint32_t type );
extern int hmcsim_trace_latency( struct hmcsim_t *hmc,
				uint32_t dev,
				uint32_t link,
				uint32_t slot,
				uint32_t quad,
				uint32_t vault );
extern int hmcsim_process_bank_conflicts( struct hmcsim_t *hmc,
						uint32_t dev,
						uint32_t quad,
						uint32_t vault,
						uint64_t *addr,
						uint64_t num_valid );
extern int hmcsim_process_rqst( struct hmcsim_t *hmc,
				uint32_t dev,
				uint32_t quad,
				uint32_t vault,
				uint32_t slot );
extern int hmcsim_util_zero_packet( struct hmc_queue_t *queue );
extern int hmcsim_util_decode_slid( 	struct hmcsim_t *hmc,
					struct hmc_queue_t *queue,
					uint32_t slot,
					uint32_t *slid );
extern int hmcsim_util_decode_vault( 	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t bsize,
					uint64_t addr,
					uint32_t *vault );
extern int hmcsim_util_decode_quad( 	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t bsize,
					uint64_t addr,
					uint32_t *quad );
extern int hmcsim_util_decode_qv(	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t bsize,
					uint64_t addr,
					uint32_t *quad,
					uint32_t *vault );
extern int hmcsim_util_is_root( 	struct hmcsim_t *hmc,
					uint32_t dev );

#ifdef HMC_DEBUG
/* ----------------------------------------------------- HMCSIM_CLOCK_PRINT_XBAR_STATS */
static int hmcsim_clock_print_xbar_stats( struct hmcsim_t *hmc,
					struct hmc_queue_t *queue )
{
	/* vars */
	int nvalid	= 0;
	int ninvalid	= 0;
	int nstalled	= 0;
	int nconflict	= 0;
	int i		= 0;
	/* ---- */

	for( i=0; i<hmc->xbar_depth; i++ ){
		if( queue[i].valid == HMC_RQST_VALID ){
			nvalid++;
		}else if( queue[i].valid == HMC_RQST_INVALID ){
			ninvalid++;
		}else if( queue[i].valid == HMC_RQST_CONFLICT ){
			nconflict++;
		}else if( queue[i].valid == HMC_RQST_STALLED ){
			nstalled++;
		}
	}

	printf( "XBAR:nvalid:ninvalid:nconflict:nstalled = %d:%d:%d:%d\n",
                nvalid, ninvalid, nconflict, nstalled );

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_PRINT_VAULT_STATS */
static int hmcsim_clock_print_vault_stats( struct hmcsim_t *hmc,
					struct hmc_queue_t *queue )
{
  /* vars */
  int nvalid	= 0;
  int ninvalid	= 0;
  int nstalled	= 0;
  int nconflict	= 0;
  int i		= 0;
  /* ---- */

  for( i=0; i<hmc->queue_depth; i++ ){
    if( queue[i].valid == HMC_RQST_VALID ){
      nvalid++;
    }else if( queue[i].valid == HMC_RQST_INVALID ){
      ninvalid++;
    }else if( queue[i].valid == HMC_RQST_CONFLICT ){
      nconflict++;
    }else if( queue[i].valid == HMC_RQST_STALLED ){
      nstalled++;
    }
  }

  printf( "VAULT:nvalid:ninvalid:nconflict:nstalled = %d:%d:%d:%d\n",
          nvalid, ninvalid, nconflict, nstalled );

  return 0;
}
#endif

/* ----------------------------------------------------- HMCSIM_CLOCK_PROCESS_RSP_QUEUE */
/*
 * HMCSIM_CLOCK_PROCESS_RSP_QUEUE
 *
 */
static int hmcsim_clock_process_rsp_queue( 	struct hmcsim_t *hmc, 
						uint32_t dev, 
						uint32_t link )
{
	/* vars */
	uint32_t i			= 0;
	uint32_t j			= 0;
	uint32_t cur			= 0;
	uint32_t dest			= 0;
	uint32_t t_slot			= hmc->xbar_depth+1;
	struct hmc_queue_t *queue	= NULL;
	/* ---- */

	if( hmcsim_util_is_root( hmc, dev ) == 1 ){

		/*
		 * i am a root device, nothing to do here
		 *
		 */

		return 0;
	}

	/*
	 * walk the response queue and process all the responses
	 *
 	 */
	for( i=0; i<hmc->xbar_depth; i++ ){

		if( hmc->devs[dev].xbar[link].xbar_rsp[i].valid != HMC_RQST_INVALID ){

			/*
	 		 * process me
			 *
			 */

			/*
			 * Stage 1: get the corresponding cub device
			 * 	    and its response queue
			 *
			 */
			dest	= hmc->devs[dev].links[link].dest_cub;
			queue	= hmc->devs[dev].xbar[dest].xbar_rsp;


			/*
			 * Stage 2: determine if the destination cub
			 * 	    device has empty response queue
			 * 	    slots
			 *
			 */
			cur = hmc->xbar_depth-1;
			for( j=0; j<hmc->xbar_depth; j++ ){
				if( queue[cur].valid == HMC_RQST_INVALID ){

					/*
					 * found an empty slot
					 */
					t_slot = cur;
				}
				cur--;
			}

			/*
			 * Stage 3: if slots exist, perform the transfer
			 *          else, stall the slot
			 *
			 */
			if( t_slot != (hmc->xbar_depth+1) ){

				/*
				 * found a good slot
				 *
				 */

				for( j=0; j<HMC_MAX_UQ_PACKET; j++){
					queue[t_slot].packet[j]	=
						hmc->devs[dev].xbar[link].xbar_rsp[i].packet[j];
				}

				queue[t_slot].valid = HMC_RQST_VALID;

				/*
				 * zero the packet
				 *
				 */
				hmcsim_util_zero_packet( &(hmc->devs[dev].xbar[link].xbar_rsp[i]));

			}else{

				/*
				 * STALL!
				 *
			 	 */
				hmc->devs[dev].xbar[link].xbar_rsp[i].valid	= HMC_RQST_STALLED;

				/*
				 * Print a stall trace
				 *
				 */
				if ((hmc->tracelevel & HMC_TRACE_STALL) >0 ) {
					hmcsim_trace_stall(	hmc,
								dev,
								0,
								0,
								dev,
								dest,
								link,
								i,
								4 );
				}
			}

		}
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_PROCESS_RQST_QUEUE */
/*
 * HMCSIM_CLOCK_PROCESS_RQST_QUEUE
 *
 */
static int hmcsim_clock_process_rqst_queue( 	struct hmcsim_t *hmc,
						uint32_t dev,
						uint32_t link )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t cur	= 0;
	uint32_t found	= 0;
	uint32_t success= 0;
	uint32_t len	= 0;
	uint32_t t_link	= 0;
	uint32_t t_slot	= 0;
	uint32_t t_quad = 0;
	uint32_t bsize	= 0;
	uint32_t t_vault= hmc->queue_depth+1;
	uint8_t	cub	= 0;
	uint8_t plink	= 0;
	uint64_t header	= 0x00ll;
	uint64_t tail	= 0x00ll;
	uint64_t addr	= 0x00ll;
	/* ---- */

	/*
	 * get the block size
	 *
	 */
	hmcsim_util_get_max_blocksize( hmc, dev, &bsize );

	/*
	 * walk the queue and process all the valid
	 * slots
	 *
	 */

	for( i=0; i<hmc->xbar_depth; i++ ){

		if( hmc->devs[dev].xbar[link].xbar_rqst[i].valid != HMC_RQST_INVALID ){ 

			/*
			 * process me
			 *
			 */

#ifdef HMC_DEBUG
			HMCSIM_PRINT_INT_TRACE( "PROCESSING REQUEST QUEUE FOR SLOT", (int)(i) );
#endif

			/*
			 * Step 1: Get the header
			 *
			 */
			header	= hmc->devs[dev].xbar[link].xbar_rqst[i].packet[0];

			addr	= ((header >> 24) & 0x3FFFFFFFF);

			/*
			 * Step 2: Get the CUB.
			 *
			 */
			cub = (uint8_t)((header>>61) & 0x3F );

			/* Step 3: If it is equal to `dev`
			 *         then we have a local request.
			 * 	   Otherwise, its a request to forward to
			 * 	   an adjacent device.
			 */

			/*
			 * Stage 4: Get the packet length
			 *
			 */
			len = (uint32_t)( (header & 0x780) >> 7);
			len *= 2;

			/*
			 * Stage 5: Get the packet tail
			 *
			 */
			tail = hmc->devs[dev].xbar[link].xbar_rqst[i].packet[len-1];


			/*
			 * Stage 6: Get the link
			 *
			 */
			plink = (uint8_t)( (tail & 0x7000000) >> 24 );

			if( cub == (uint8_t)(dev) ){
#ifdef HMC_DEBUG
				HMCSIM_PRINT_INT_TRACE( "LOCAL DEVICE REQUEST AT SLOT", (int)(i) );
#endif
				/*
				 * local request
				 *
				 */

				/*
				 * 7a: Retrieve the vault id
				 *
				 */
				hmcsim_util_decode_vault( hmc,
							dev,
							bsize,
							addr,
							&t_vault );

				/*
				 * 8a: Retrieve the quad id
				 *
				 */
				hmcsim_util_decode_quad( hmc,
							dev,
							bsize,
							addr,
							&t_quad );


				/*
				 * if quad is not directly attached
				 * to my link, print a trace message
				 * indicating higher latency
				 */
				if( link != t_quad ){
					/*
					 * higher latency
					 *
					 */

					if( (hmc->tracelevel & HMC_TRACE_LATENCY) > 0 ){ 
						hmcsim_trace_latency( hmc,
									dev,
									link,
									i,
									t_quad,
									t_vault );
					}
				}

				/*
				 * 9a: Search the vault queue for valid slot
				 *     Search bottom-up
				 *
				 */
				t_slot = hmc->queue_depth+1;
				cur    = hmc->queue_depth-1;

				for( j=0; j<hmc->queue_depth; j++ ){

					if( hmc->devs[dev].quads[t_quad].vaults[t_vault].rqst_queue[cur].valid == HMC_RQST_INVALID ){
						t_slot = cur;
					}

					cur--;
				}

				if( t_slot == hmc->queue_depth+1 ){


#ifdef HMC_DEBUG
					HMCSIM_PRINT_INT_TRACE( "STALLED REQUEST AT SLOT", (int)(i) );
#endif

					/* STALL */
					hmc->devs[dev].xbar[link].xbar_rqst[i].valid = HMC_RQST_STALLED;

					/*
					 * print a stall trace
					 *
					 */
					if ((hmc->tracelevel & HMC_TRACE_STALL) >0 ) {
						hmcsim_trace_stall(	hmc,
									dev,
									t_quad,
									t_vault,
									0,
									0,
									0,
									i,
									0 );
					}

					success = 0;
				}else {

#ifdef HMC_DEBUG
					HMCSIM_PRINT_INT_TRACE( "TRANSFERRING PACKET FROM SLOT", (int)(i) );
					HMCSIM_PRINT_INT_TRACE( "TRANSFERRING PACKET TO SLOT", (int)(t_slot) );
#endif
					/*
					 * push it into the designated queue slot
					 *
					 */
					hmc->devs[dev].quads[t_quad].vaults[t_vault].rqst_queue[t_slot].valid = HMC_RQST_VALID;
					for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){ 
						hmc->devs[dev].quads[t_quad].vaults[t_vault].rqst_queue[t_slot].packet[j] = 
							hmc->devs[dev].xbar[link].xbar_rqst[i].packet[j];
					}

					success = 1;

				}

			}else{

#ifdef HMC_DEBUG
				HMCSIM_PRINT_INT_TRACE( "REMOTE DEVICE REQUEST AT SLOT", (int)(i) );
#endif
				/*
				 * forward request to remote device
				 *
				 */

				/*
				 * Stage 7b: Decide whether cub is accessible
				 *
				 */
				found = 0;

				while( (found != 1) && (j<hmc->num_links) ){

					if( hmc->devs[dev].links[j].dest_cub == cub ){ 
						found = 1;
						t_link = j;
					}

					j++;
				}

				if( found == 0 ){
					/*
					 * oh snap! can't route to that CUB
					 * Mark it as a zombie request
					 * Future: return an error packet
					 *
					 */
					hmc->devs[dev].xbar[link].xbar_rqst[i].valid = HMC_RQST_ZOMBIE;
				}else{
					/*
					 * 8b: routing is good, look for an empty slot
					 * in the target xbar link queue
					 *
					 */
					t_slot = hmc->xbar_depth+1;
					cur    = hmc->xbar_depth-1;

					for( j=0; j<hmc->xbar_depth; j++ ){
	
						/*
						 * walk the queue from the bottom
						 * up
						 */
						if( hmc->devs[cub].xbar[t_link].xbar_rqst[cur].valid == 
							HMC_RQST_INVALID ) {
							t_slot = cur;
						}
						cur--;
					}

					/*
				 	 * 9b: If available, insert into remote xbar slot 
					 *
					 */
					if( t_slot == hmc->xbar_depth+1 ){
						/*
						 * STALL!
						 *
						 */
						hmc->devs[dev].xbar[link].xbar_rqst[i].valid = HMC_RQST_STALLED;
						/*
					 	 * print a stall trace
					 	 *
					 	 */
						if ((hmc->tracelevel & HMC_TRACE_STALL) >0 ) {
							hmcsim_trace_stall(	hmc, 
										dev, 
										0,
										0, 
										dev, 
										cub,
										link,
										i, 
										3 ); 
						}
		
						success = 0;
					}else {
						/*
						 * put the new link in the link field
						 *
						 */
						 hmc->devs[dev].xbar[link].xbar_rqst[i].packet[len-1] |= 
										((uint64_t)(plink)<<24);

						/*
						 * transfer the packet to the target slot
						 *
						 */
						hmc->devs[cub].xbar[t_link].xbar_rqst[t_slot].valid = 
							HMC_RQST_VALID;
						for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){ 
							hmc->devs[cub].xbar[t_link].xbar_rqst[t_slot].packet[j] = 
							hmc->devs[dev].xbar[link].xbar_rqst[i].packet[j];
						}

						/*
						 * signal success
						 *
						 */
						success = 1;
					}
				}
			}

			if( success == 1 ){

				/*
				 * clear the packet
				 *
				 */
#ifdef HMC_DEBUG
				HMCSIM_PRINT_TRACE( "ZEROING PACKET" );
#endif
				hmcsim_util_zero_packet( &(hmc->devs[dev].xbar[link].xbar_rqst[i]) );
			}

		}

		success = 0;
	}

#ifdef HMC_DEBUG
	hmcsim_clock_print_xbar_stats( hmc, hmc->devs[dev].xbar[link].xbar_rqst );
	HMCSIM_PRINT_TRACE( "FINISHED PROCESSING REQUEST QUEUE" );
#endif

	return 0;
}


/* ----------------------------------------------------- HMCSIM_CLOCK_CHILD_XBAR */
/*
 * HMCSIM_CLOCK_CHILD_XBAR
 *
 */
static int hmcsim_clock_child_xbar( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t host_l	= 0;
	/* ---- */

	/*
	 * walk each device and interpret the links
	 * if you find a HOST link, no dice.
	 * otherwise, process the xbar queues
	 *
	 */
	for( i=0; i<hmc->num_devs; i++ ){

		/*
		 * determine if i am connected to the host device
	 	 *
	 	 */

		if( hmcsim_util_is_root( hmc, i ) == 1 ){
			host_l++;
		}

		/*
		 * if no host links found, process the queues
	 	 *
		 */
		if( host_l == 0 ) {

			/*
			 * walk the xbar queues and process the
			 * incoming packets
			 *
			 */
			for( j=0; j<hmc->num_links; j++ ){

				hmcsim_clock_process_rqst_queue( hmc, i, j );
				hmcsim_clock_process_rsp_queue( hmc, i, j );
			}
		}

		/*
		 * reset the host links
		 *
		 */
		host_l	= 0;
	}

	return 0;
}


/* ----------------------------------------------------- HMCSIM_CLOCK_ROOT_XBAR */
/*
 * HMCSIM_CLOCK_ROOT_XBAR
 *
 */
static int hmcsim_clock_root_xbar( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t host_l	= 0;
	/* ---- */

	/*
	 * walk each device and interpret the links
	 * if you find a HOST link, process it.
	 * otherwise, ignore it
	 *
	 */
	for( i=0; i<hmc->num_devs; i++ ){

		/*
		 * determine if i am connected to the host device
	 	 *
	 	 */

		if( hmcsim_util_is_root( hmc, i ) == 1 ){
			host_l++;
		}

		/*
		 * if no host links found, process the queues
	 	 *
		 */
		if( host_l != 0 ) {


#ifdef HMC_DEBUG
			HMCSIM_PRINT_INT_TRACE( "HMCSIM_CLOCK_ROOT_XBAR:PROCESSING DEVICE", (int)(i) );
#endif

			/*
			 * walk the xbar queues and process the
			 * incoming packets
			 *
			 */
			for( j=0; j<hmc->num_links; j++ ){

				hmcsim_clock_process_rqst_queue( hmc, i, j );

				/*
				 * no point in walking the response queues,
				 * the host must drain these manually
				 * using recv's
				 *
				 */
			}
		}

		/*
		 * reset the host links
		 *
		 */
		host_l	= 0;
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_BANK_CONFLICTS */
/*
 * HMCSIM_CLOCK_BANK_CONFLICTS
 *
 */
static int hmcsim_clock_bank_conflicts( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t k	= 0;
	uint32_t x	= 0;
	uint64_t tmp	= 0x00ll;
	uint64_t addr[HMC_MAX_BANKS];
	uint64_t na	= 0;
	/* ---- */

	/*
	 * clear the address array
	 *
	 */
	for( i=0; i<HMC_MAX_BANKS; i++ ){
		addr[i]	= 0x00ll;
	}


	/*
	 * reset each of the valid queue entries
	 * such that we don't poison things
	 *
	 */
	for( i=0; i<hmc->num_devs; i++){
		for( j=0; j<hmc->num_quads; j++ ){
			for( k=0; k<4; k++ ){
				for( x=0; x<hmc->queue_depth; x++ ){

					/*
					 * request queue
					 *
					 */
					if( hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid == HMC_RQST_CONFLICT ){ 
						hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid = HMC_RQST_VALID;
					}else if( hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid == HMC_RQST_STALLED ){ 
						hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid = HMC_RQST_VALID;
					}

					/*
					 * response queue
					 *
					 */
					if( hmc->devs[i].quads[j].vaults[k].rsp_queue[x].valid == HMC_RQST_CONFLICT ){ 
						hmc->devs[i].quads[j].vaults[k].rsp_queue[x].valid = HMC_RQST_VALID;
					}else if( hmc->devs[i].quads[j].vaults[k].rsp_queue[x].valid == HMC_RQST_STALLED ){ 
						hmc->devs[i].quads[j].vaults[k].rsp_queue[x].valid = HMC_RQST_VALID;
					}
				}
			}
		}
	}

	/*
	 * Walk each device+vault combination
	 * and examine the request queues for
	 * bank conflicts
	 *
 	 */
	for( i=0; i<hmc->num_devs; i++){
		for( j=0; j<hmc->num_quads; j++ ){
			for( k=0; k<4; k++ ){

				na = 0;

				/*
				 * process the first
				 * min( HMC_NUM_BANKS, queue_depth )
				 * queue slots and examine
				 * the addresses
				 *
				 */

				if( hmc->queue_depth > HMC_MAX_BANKS ){

					/*
					 * queue_depth is > MAX_BANKS
					 *
					 */
					for( x=0; x<HMC_MAX_BANKS; x++ ){

						/*
						 * collect the first 'x'
						 * addresses
						 *
						 */
						if( hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid == HMC_RQST_VALID ){ 
							tmp = 0x00ll;
							tmp =
						   	(hmc->devs[i].quads[j].vaults[k].rqst_queue[x].packet[0]>>24)
						   	&0x3FFFFFFFF;
							addr[na] = tmp;

							na++;
						}
					}

				} else {

					/*
					 * queue_depth is < NUM_BANKS
					 *
					 */
					for( x=0; x<hmc->queue_depth; x++ ){

						/*
						 * collect the first 'x'
						 * addresses
						 *
						 */
						tmp = 0x00ll;
						tmp =
						   (hmc->devs[i].quads[j].vaults[k].rqst_queue[x].packet[0]>>24)
						   &0x3FFFFFFFF;
						addr[na] = tmp;

						na++;
					}

				}

				/*
				 * process the bank conflicts
				 *
			 	 */
				//hmcsim_process_bank_conflicts( hmc, i, j, k, &(addr[0]), na ); 
			}
		}
	}

#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "COMPLETED PROCESSING BANK CONFLICTS" );
#endif

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_ANALYSIS_PHASE */
/*
 * HMCSIM_CLOCK_ANALYSIS_PHASE
 *
 */
static int hmcsim_clock_analysis_phase( struct hmcsim_t *hmc )
{
	/*
	 * This is where we put all the inner-clock
	 * analysis phases.  The current phases
	 * are as follows:
 	 *
	 * 1) Bank conflict analysis
	 * 2) TODO: cache detection analysis
	 *
	 */

	/*
	 * 1) Bank Conflict Analysis
	 *
	 */
	if( hmcsim_clock_bank_conflicts( hmc ) != 0 ){
		return -1;
	}

	/*
	 * 2) Cache Detection Analysis
	 *
	 */

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_RW_OPS */
/*
 * HMCSIM_CLOCK_RW_OPS
 *
 */
static int hmcsim_clock_rw_ops( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t k	= 0;
	uint32_t x	= 0;
	uint32_t test	= 0x00000000;
	/* ---- */

	for( i=0; i<hmc->num_devs; i++){
		for( j=0; j<hmc->num_quads; j++ ){
			//for( k=0; j<hmc->num_vaults; k++ ){
			for( k=0; k<4; k++ ){

				/*
				 * process the first
				 * min( HMC_NUM_BANKS, queue_depth )
				 * queue slots and process the requests
				 *
				 */

				if( hmc->queue_depth > HMC_MAX_BANKS ){

					/*
					 * queue_depth is > MAX_BANKS
					 *
					 */
					for( x=0; x<hmc->num_banks; x++ ){

						/*
						 * determine whether the
						 * queue slot is valid and not
						 * a bank conflict
						 *
						 */
						test = 0x00000000;
						test = hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid;

						if( (test > 0 ) && (test != 2) ){

							/*
							 * valid and no conflict
							 * process the request
							 *
							 */
							hmcsim_process_rqst( hmc, i, j, k, x );
						}
					}

				} else {

					/*
					 * queue_depth is < NUM_BANKS
					 *
					 */
					for( x=0; x<hmc->queue_depth; x++ ){

						/*
						 * determine whether the
						 * queue slot is valid and not
						 * a bank conflict
						 *
						 */
						test = 0x00000000;
						test = hmc->devs[i].quads[j].vaults[k].rqst_queue[x].valid;

						if( (test > 0 ) && (test != 2) ){

							/*
							 * valid and no conflict
							 * process the request
							 *
							 */
							hmcsim_process_rqst( hmc, i, j, k, x );
						}
					}

				}
			}
		}
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_REG_RESPONSES */
/*
 * HMCSIM_CLOCK_REG_RESPONSES
 *
 */
static int hmcsim_clock_reg_responses( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i		= 0;
	uint32_t j		= 0;
	uint32_t cur		= 0;
	uint32_t k		= 0;
	uint32_t x		= 0;
	uint32_t y		= 0;
	uint32_t r_link		= 0;
	uint32_t r_slot		= hmc->xbar_depth+1;
	struct hmc_queue_t *lq	= NULL;
	/* ---- */


	/*
	 * Walk all the vault response queues
	 * For each queue, attempt to push it into a crossbar
	 * slot.  If not, signal a stall
	 *
 	 */
	for( i=0; i<hmc->num_devs; i++){
		for( j=0; j<hmc->num_quads; j++ ){
			//for( k=0; k<hmc->num_vaults; k++ ){
			for( k=0; k<4; k++ ){

				lq = hmc->devs[i].quads[j].vaults[k].rsp_queue;

				for( x=0; x<hmc->queue_depth; x++ ){

					/*
					 * Determine if I am a live response
					 * If so, check for an appropriate
					 * response queue slot
					 *
					 */
					if( lq[x].valid != HMC_RQST_INVALID ){
						
						/*
						 * determine which link response
						 * queue we're supposed to route
						 * use the SLID value
						 *
						 */
						hmcsim_util_decode_slid( hmc,
									lq,
									x,
									&r_link );

						/*
						 * determine if the response
						 * xbar queue has an empty slot
						 *
						 */
						cur = hmc->xbar_depth-1;
						for( y=0; y<hmc->xbar_depth; y++ ){	
							if( hmc->devs[i].xbar[r_link].xbar_rsp[cur].valid == 
									HMC_RQST_INVALID ){
								/* empty queue slot */
								r_slot = cur;
							}
							cur--;
						}

						/*
						 * if we found a good slot, insert it
						 * and zero the vault response slot
						 *
						 */
						if( r_slot != (hmc->xbar_depth+1) ){

							/*
							 * slot found!
							 * transfer the data
							 *
							 */
							hmc->devs[i].xbar[r_link].xbar_rsp[r_slot].valid = 
									HMC_RQST_VALID;
							for( y=0; y<HMC_MAX_UQ_PACKET; y++){ 
								hmc->devs[i].xbar[r_link].xbar_rsp[r_slot].packet[y] = 
										lq[x].packet[y];
								lq[x].packet[y]	= 0x00ll;
							}

							/*
							 * clear the source slot
							 *
							 */
							lq[x].valid = HMC_RQST_INVALID;

						}else{

							/*
							 * STALL!
							 *
							 */

							lq[x].valid = HMC_RQST_STALLED;

							if( (hmc->tracelevel & HMC_TRACE_STALL)>0 ){

								/*
								 * print a trace signal 
								 *
								 */
								hmcsim_trace_stall( hmc, 
										i, 
										j, 
										k,
										0,
										0,
										0, 
										x, 
										2 );
							}
						}
					}
					/* else, request not valid */
				}

#ifdef HMC_DEBUG
				hmcsim_clock_print_vault_stats( hmc, hmc->devs[i].quads[j].vaults[k].rqst_queue );
				hmcsim_clock_print_vault_stats( hmc, hmc->devs[i].quads[j].vaults[k].rsp_queue );
#endif
			}
		}
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_REORG_XBAR_RQST */
/*
 * HMCSIM_CLOCK_REORG_XBAR_RQST
 *
 */
static int hmcsim_clock_reorg_xbar_rqst( struct hmcsim_t *hmc, uint32_t dev, uint32_t link )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t slot   = 0;
	/* ---- */

	/*
	 * walk the request queue starting at slot 1
	 *
	 */
	for( i=1; i<hmc->xbar_depth; i++ )
	{
		/*
		 * if the slot is valid, look upstream in the queue
		 *
		 */
		if( hmc->devs[dev].xbar[link].xbar_rqst[i].valid == HMC_RQST_VALID ){

			/*
			 * find the lowest appropriate slot
			 *
			 */
			slot = i;
			for( j=(i-1); j>0; j-- ){
				if( hmc->devs[dev].xbar[link].xbar_rqst[j].valid == HMC_RQST_INVALID ){
					slot = j;
				}
			}

			/*
		 	 * check to see if a new slot was found
			 * if so, perform the swap
			 *
			 */
			if( slot != i ){

				/*
				 * perform the swap
				 *
				 */
				for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){

					hmc->devs[dev].xbar[link].xbar_rqst[slot].packet[j] = 
						hmc->devs[dev].xbar[link].xbar_rqst[i].packet[j];	

					hmc->devs[dev].xbar[link].xbar_rqst[i].packet[j] =0x00ll;	
					
				}

				hmc->devs[dev].xbar[link].xbar_rqst[slot].valid = 1;	
				hmc->devs[dev].xbar[link].xbar_rqst[i].valid    = 0;	
			}
		} /* else, slot not valid.. move along */
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_REORG_XBAR_RSP */
/*
 * HMCSIM_CLOCK_REORG_XBAR_RSP
 *
 */
static int hmcsim_clock_reorg_xbar_rsp( struct hmcsim_t *hmc, uint32_t dev, uint32_t link )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t slot   = 0;
	/* ---- */

	/*
	 * walk the response queue starting at slot 1
	 *
	 */
	for( i=1; i<hmc->xbar_depth; i++ )
	{
		/*
		 * if the slot is valid, look upstream in the queue
		 *
		 */
		if( hmc->devs[dev].xbar[link].xbar_rsp[i].valid == HMC_RQST_VALID ){

			/*
			 * find the lowest appropriate slot
			 *
			 */
			slot = i;
			for( j=(i-1); j>0; j-- ){
				if( hmc->devs[dev].xbar[link].xbar_rsp[j].valid == HMC_RQST_INVALID ){
					slot = j;
				}

			}

			/*
		 	 * check to see if a new slot was found
			 * if so, perform the swap
			 *
			 */
			if( slot != i ){

				/*
				 * perform the swap
				 *
				 */
				for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){

					hmc->devs[dev].xbar[link].xbar_rsp[slot].packet[j] = 
						hmc->devs[dev].xbar[link].xbar_rsp[i].packet[j];	

					hmc->devs[dev].xbar[link].xbar_rsp[i].packet[j] =0x00ll;	
					
				}

				hmc->devs[dev].xbar[link].xbar_rsp[slot].valid = 1;	
				hmc->devs[dev].xbar[link].xbar_rsp[i].valid    = 0;	
			}
		} /* else, slot not valid.. move along */
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_REORG_VAULT_RQST */
/*
 * HMCSIM_CLOCK_REORG_VAULT_RQST
 *
 */
static int hmcsim_clock_reorg_vault_rqst( 	struct hmcsim_t *hmc,
						uint32_t dev,
						uint32_t quad,
						uint32_t vault )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t slot   = 0;
	/* ---- */

	/*
	 * walk the request queue starting at slot 1
	 *
	 */
	for( i=1; i<hmc->queue_depth; i++ )
	{
		/*
		 * if the slot is valid, look upstream in the queue
		 *
		 */
		if( hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[i].valid == HMC_RQST_VALID ){

			/*
			 * find the lowest appropriate slot
			 *
			 */
			slot = i;
			for( j=(i-1); j>0; j-- ){
				if( hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[j].valid == HMC_RQST_INVALID ){
					slot = j;
				}
			}

			/*
		 	 * check to see if a new slot was found
			 * if so, perform the swap
			 *
			 */
			if( slot != i ){

				/*
				 * perform the swap
				 *
				 */
				for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){

					hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[slot].packet[j] = 
						hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[i].packet[j];	

					hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[i].packet[j] =0x00ll;	
					
				}

				hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[slot].valid = 1;	
				hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[i].valid    = 0;	
			}
		} /* else, slot not valid.. move along */
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_REORG_VAULT_RSP */
/*
 * HMCSIM_CLOCK_REORG_VAULT_RSP
 *
 */
static int hmcsim_clock_reorg_vault_rsp( 	struct hmcsim_t *hmc,
						uint32_t dev,
						uint32_t quad,
						uint32_t vault )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t slot   = 0;
	/* ---- */

	/*
	 * walk the response queue starting at slot 1
	 *
	 */
	for( i=1; i<hmc->queue_depth; i++ )
	{
		/*
		 * if the slot is valid, look upstream in the queue
		 *
		 */
		if( hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[i].valid == HMC_RQST_VALID ){

			/*
			 * find the lowest appropriate slot
			 *
			 */
			slot = i;
			for( j=(i-1); j>0; j-- ){
				if( hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[j].valid == HMC_RQST_INVALID ){
					slot = j;
				}
			}

			/*
		 	 * check to see if a new slot was found
			 * if so, perform the swap
			 *
			 */
			if( slot != i ){

				/*
				 * perform the swap
				 *
				 */
				for( j=0; j<HMC_MAX_UQ_PACKET; j++ ){

					hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[slot].packet[j] = 
						hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[i].packet[j];	

					hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[i].packet[j] =0x00ll;	
					
				}

				hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[slot].valid = 1;	
				hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[i].valid    = 0;	
			}
		} /* else, slot not valid.. move along */
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK_QUEUE_REORG */
/*
 * HMCSIM_CLOCK_QUEUE_REORG
 *
 */
static int hmcsim_clock_queue_reorg( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i	= 0;
	uint32_t j	= 0;
	uint32_t k	= 0;
	/* ---- */

	/*
	 * crossbar queues
	 *
	 */
	for( i=0; i<hmc->num_devs; i++ )
	{
		for( j=0; j<hmc->num_links; j++ )
		{
			/*
			 * reorder:
			 * hmc->devs[i].xbar[j].xbar_rqst;
			 * hmc->devs[i].xbar[j].xbar_rsp;
			 *
			 */
			hmcsim_clock_reorg_xbar_rqst( hmc, i, j );
			hmcsim_clock_reorg_xbar_rsp( hmc, i, j );
		}
	}

	/*
	 * vault queues
	 *
	 */
	for( i=0; i<hmc->num_devs; i++ )
	{
		for( j=0; j<hmc->num_quads; j++ )
		{
			//for( k=0; k<hmc->num_vaults; k++ )
			for( k=0; k<4; k++ )
			{
				/*
				 * reorder:
				 * hmc->devs[i].quads[j].vaults[k].rqst_queue;
				 * hmc->devs[i].quads[j].vaults[k].rsp_queue;
				 *
			 	 */
				hmcsim_clock_reorg_vault_rqst( hmc, i, j, k );
				hmcsim_clock_reorg_vault_rsp( hmc, i, j, k );
			}
		}
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_CLOCK */
/*
 * HMCSIM_CLOCK
 *
 */
extern int	hmcsim_clock( struct hmcsim_t *hmc )
{
	/*
	 * sanity check
	 *
 	 */
	if( hmc == NULL ){
		return -1;
	}

	/*
	 * Overview of the clock handler structure
	 *
	 * Stage 1: Walk all the child devices and drain
	 * 	    their xbars
	 *
	 * Stage 2: Start with the root device(s) and drain
	 * 	    the xbar of any messages
	 *
	 * Stage 3: Walk each device's vault queues and
	 *          look for bank conflicts
	 *
	 * Stage 4: Walk each device's vault queues and
	 * 	    perform the respective operations
	 *
	 * Stage 5: Register any necessary responses with
	 * 	    the xbar
	 *
	 * Stage 6: Update the internal clock value
	 *
	 */

	/*
	 * Stage 1: Drain the child devices
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE1: DRAIN CHILD DEVICES" );
#endif
	if( hmcsim_clock_child_xbar( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 2: Drain the root devices
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE2: DRAIN ROOT DEVICES" );
#endif
	if( hmcsim_clock_root_xbar( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 3: Walk the vault queues and perform
	 *          any integrated analysis
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE3: ANALYSIS PHASE" );
#endif
	if( hmcsim_clock_analysis_phase( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 4: Walk the vault queues and perform
	 *          read and write operations
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE4: R/W OPERATIONS" );
#endif
	if( hmcsim_clock_rw_ops( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 5: Register any responses
	 *          with the crossbar.
	 *          This is registering responses
	 * 	    from the respective vault response
	 * 	    queues with a crossbar response queue
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE5: REGISTER RESPONSES" );
#endif
	if( hmcsim_clock_reg_responses( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 5a: Reorder all the request queues
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE5a: REORDER THE QUEUES" );
#endif
	if( hmcsim_clock_queue_reorg( hmc ) != 0 ){
		return -1;
	}

	/*
	 * Stage 6: update the clock value
	 *
	 */
#ifdef HMC_DEBUG
	HMCSIM_PRINT_TRACE( "STAGE6: UPDATE THE CLOCK" );
#endif
	hmc->clk++;

	return 0;
}

/* EOF */
