/* 
 * _HMC_UTIL_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * UTILITY FUNCTIONS
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"


/* ----------------------------------------------------- HMCSIM_GET_CLOCK */
extern uint64_t hmcsim_get_clock( struct hmcsim_t *hmc ){
  if( hmc == NULL ){ 
    return 0x00ll;
  }

  return hmc->clk;
}

/* ----------------------------------------------------- HMCSIM_UTIL_IS_ROOT */
/* 
 * HMCSIM_UTIL_IS_ROOT 
 *
 */
extern int hmcsim_util_is_root( struct hmcsim_t *hmc, 
				uint32_t dev )
{
	/* vars */
	uint32_t	is_root = 0; 
	uint32_t 	i	= 0;
	/* ---- */

	/* 
	 * walk the links and see if i am a root device 
	 * root devices have a src_cub == num_devs+1
	 * 
	 */
	for( i=0; i<hmc->num_links;i++ ){ 

		if( hmc->devs[dev].links[i].src_cub == (hmc->num_devs+1) ){
			is_root = 1;
		}

	}

	return is_root;
}

/* ----------------------------------------------------- HMCSIM_UTIL_DECODE_SLID */
/* 
 * HMCSIM_UTIL_DECODE_SLID 
 *
 */
extern int hmcsim_util_decode_slid(	struct hmcsim_t *hmc, 
					struct hmc_queue_t *queue, 
					uint32_t slot, 
					uint32_t *slid )
{
	/* vars */
	uint64_t header	= 0x00ll;
	uint32_t tmp	= 0x00;
	/* ---- */

	/* 
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	}

	if( slid == NULL ){ 
		return -1;
	}

	if( queue == NULL ){ 
		return -1;
	}

	/* 
	 * get the packet header
	 * 
	 */
	header 	= queue[slot].packet[0];

	/* 
	 * get the slid value [41:39]
	 * 
	 */
	tmp 	= (uint32_t)((header>>39) & 0x7);

	/* 
	 * write it out 
	 * 
	 */
	*slid 	= tmp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_DECODE_QUAD */
/* 
 * HMCSIM_UTIL_DECODE_QUAD
 * 
 */
extern int hmcsim_util_decode_quad( 	struct hmcsim_t *hmc, 
					uint32_t dev, 
					uint32_t bsize, 
					uint64_t addr,
					uint32_t *quad )
{
	/* vars */
	uint32_t num_links	= 0x00;
	uint32_t capacity	= 0x00;
	uint32_t tmp		= 0x00;
	/* ---- */

	/*
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	}

	num_links	= hmc->num_links;
	capacity	= hmc->capacity;	

	/* 
	 * link layout 
	 * 
	 */
	if( num_links == 4 ){ 
		/* 
	 	 * 4-link device 
		 *
		 */
		if( capacity == 2 ){

			switch( bsize )
			{
				case 32:
					/* [8:7] */
					tmp = (uint32_t)((addr>>7) & 0x3);
					break;
				case 64:
					/* [9:8] */
					tmp = (uint32_t)((addr>>8) & 0x3);
					break;
				case 128:
					/* [10:9] */
					tmp = (uint32_t)((addr>>9) & 0x3);
					break;
				default:
					break;
			}

		} else if( capacity == 4 ){ 

			switch( bsize )
			{
				case 32:
					/* [12:11] */
					tmp = (uint32_t)((addr>>11) & 0x3);
					break;
				case 64:
					/* [13:12] */
					tmp = (uint32_t)((addr>>12) & 0x3);
					break;
				case 128:
					/* [14:13] */
					tmp = (uint32_t)((addr>>13) & 0x3);
					break;
				default:
					break;
			}

		}
	} else if( num_links == 8 ){ 
		/* 
	 	 * 8-link device 
		 *
		 */
		if( capacity == 4 ){
	
			switch( bsize )
			{
				case 32:
					/* [9:7] */
					tmp = (uint32_t)((addr>>7) & 0x7);
					break;
				case 64:
					/* [10:8] */
					tmp = (uint32_t)((addr>>8) & 0x7);
					break;
				case 128:
					/* [11:9] */
					tmp = (uint32_t)((addr>>9) & 0x7);
					break;
				default:
					break;
			}

			
		} else if( capacity == 8 ){ 

			switch( bsize )
			{
				case 32:
					/* [9:7] */
					tmp = (uint32_t)((addr>>7) & 0x7);
					break;
				case 64:
					/* [10:8] */
					tmp = (uint32_t)((addr>>8) & 0x7);
					break;
				case 128:
					/* [11:9] */
					tmp = (uint32_t)((addr>>9) & 0x7);
					break;
				default:
					break;
			}

		}
	} else {	
		return -1;
	}

	/* 
	 * write out the value 
	 * 
 	 */
	*quad = tmp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_DECODE_VAULT */
/* 
 * HMCSIM_UTIL_DECODE_VAULT
 * 
 */
extern int hmcsim_util_decode_vault( 	struct hmcsim_t *hmc, 
					uint32_t dev, 
					uint32_t bsize, 
					uint64_t addr,
					uint32_t *vault )
{
	/* vars */
	uint32_t num_links	= 0x00;
	uint32_t capacity	= 0x00;
	uint32_t tmp		= 0x00;
	/* ---- */

	/*
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	}

	num_links	= hmc->num_links;
	capacity	= hmc->capacity;	

	/* 
	 * link layout 
	 * 
	 */
	if( num_links == 4 ){ 
		/* 
	 	 * 4-link device 
		 *
		 */
		if( capacity == 2 ){

			switch( bsize )
			{
				case 32:
					/* [6:5] */
					tmp = (uint32_t)((addr>>5) & 0x3);
					break;
				case 64:
					/* [7:6] */
					tmp = (uint32_t)((addr>>6) & 0x3);
					break;
				case 128:
					/* [8:7] */
					tmp = (uint32_t)((addr>>7) & 0x3);
					break;
				default:
					break;
			}

		} else if( capacity == 4 ){ 

			switch( bsize )
			{
				case 32:
					/* [6:5] */
					tmp = (uint32_t)((addr>>5) & 0x3);
					break;
				case 64:
					/* [7:6] */
					tmp = (uint32_t)((addr>>6) & 0x3);
					break;
				case 128:
					/* [8:7] */
					tmp = (uint32_t)((addr>>7) & 0x3); // hkim
					break;
				default:
					break;
			}

		}
	} else if( num_links == 8 ){ 
		/* 
	 	 * 8-link device 
		 *
		 */
		if( capacity == 4 ){
	
			switch( bsize )
			{
				case 32:
					/* [6:5] */
					tmp = (uint32_t)((addr>>5) & 0x3);
					break;
				case 64:
					/* [7:6] */
					tmp = (uint32_t)((addr>>6) & 0x3);
					break;
				case 128:
					/* [8:7] */
					tmp = (uint32_t)((addr>>7) & 0x3);
					break;
				default:
					break;
			}

			
		} else if( capacity == 8 ){ 

			switch( bsize )
			{
				case 32:
					/* [6:5] */
					tmp = (uint32_t)((addr>>5) & 0x3);
					break;
				case 64:
					/* [7:6] */
					tmp = (uint32_t)((addr>>6) & 0x3);
					break;
				case 128:
					/* [8:7] */
					tmp = (uint32_t)((addr>>7) & 0x3);
					break;
				default:
					break;
			}

		}
	} else {	
		return -1;
	}

	/* 
	 * write out the value 
	 * 
 	 */
	*vault = tmp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_DECODE_QV */
/* 
 * HMCSIM_UTIL_DECODE_QV
 * 
 */
extern int hmcsim_util_decode_qv( 	struct hmcsim_t *hmc, 
					uint32_t dev, 
					uint32_t bsize, 
					uint64_t addr,
					uint32_t *quad, 
					uint32_t *vault )
{

	if( hmc == NULL ){ 
		return -1;
	}

	if( quad == NULL ){ 
		return -1;
	}

	if( vault == NULL ){ 
		return -1;
	}

	/* 
	 * decode the quad and vault
	 * 
 	 */
	hmcsim_util_decode_quad( hmc, dev, bsize, addr, quad );
	hmcsim_util_decode_vault( hmc, dev, bsize, addr, vault );

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_DECODE_BANK */
/* 
 * HMCSIM_UTIL_DECODE_BANK 
 * 
 */
extern int hmcsim_util_decode_bank( 	struct hmcsim_t *hmc, 
					uint32_t dev, 
					uint32_t bsize, 
					uint64_t addr,
					uint32_t *bank )
{
	/* vars */
	uint32_t num_links	= 0x00;
	uint32_t capacity	= 0x00;
	uint32_t tmp		= 0x00;
	/* ---- */

	/*
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	}

	num_links	= hmc->num_links;
	capacity	= hmc->capacity;	

	/* 
	 * link layout 
	 * 
	 */
	if( num_links == 4 ){ 
		/* 
	 	 * 4-link device 
		 *
		 */
		if( capacity == 2 ){

			switch( bsize )
			{
				case 32:
					/* [11:9] */
					tmp = (uint32_t)((addr>>9) & 0x7);
					break;
				case 64:
					/* [12:10] */
					tmp = (uint32_t)((addr>>10) & 0x7);
					break;
				case 128:
					/* [13:11] */
					tmp = (uint32_t)((addr>>11) & 0x7);
					break;
				default:
					break;
			}

		} else if( capacity == 4 ){ 

			switch( bsize )
			{
				case 32:
					/* [12:9] */
					tmp = (uint32_t)((addr>>9) & 0xF);
					break;
				case 64:
					/* [13:10] */
					tmp = (uint32_t)((addr>>10) & 0xF);
					break;
				case 128:
					/* [14:11] */
					tmp = (uint32_t)((addr>>11) & 0xF);
					break;
				default:
					break;
			}

		}
	} else if( num_links == 8 ){ 
		/* 
	 	 * 8-link device 
		 *
		 */
		if( capacity == 4 ){
	
			switch( bsize )
			{
				case 32:
					/* [12:10] */
					tmp = (uint32_t)((addr>>10) & 0x7);
					break;
				case 64:
					/* [13:11] */
					tmp = (uint32_t)((addr>>11) & 0x7);
					break;
				case 128:
					/* [14:12] */
					tmp = (uint32_t)((addr>>12) & 0x7);
					break;
				default:
					break;
			}

			
		} else if( capacity == 8 ){ 

			switch( bsize )
			{
				case 32:
					/* [13:10] */
					tmp = (uint32_t)((addr>>10) & 0xF);
					break;
				case 64:
					/* [14:11] */
					tmp = (uint32_t)((addr>>12) & 0xF);
					break;
				case 128:
					/* [15:12] */
					tmp = (uint32_t)((addr>>12) & 0xF);
					break;
				default:
					break;
			}

		}
	} else {	
		return -1;
	}

	/* 
	 * write out the value 
	 * 
 	 */
	*bank = tmp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_ZERO_PACKET */
/* 
 * HMCSIM_UTIL_ZERO_PACKET
 * 
 */
extern int hmcsim_util_zero_packet( struct hmc_queue_t *queue  )
{
	/* vars */
	uint64_t i	= 0;
	/* ---- */

	/* 
	 * sanity check 
	 * 
 	 */
	if( queue == NULL ){ 
		return -1;
	}

	for( i=0; i<HMC_MAX_UQ_PACKET; i++){
		queue->packet[i]	= 0x00ll;
	}

	queue->valid = HMC_RQST_INVALID;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_SET_MAX_BLOCKSIZE */
/* 
 * HMCSIM_UTIL_SET_MAX_BLOCKSIZE
 * See Table38 in the HMC Spec : pg 58
 * 
 */
extern int hmcsim_util_set_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t bsize )
{
	/* vars */
	uint64_t tmp = 0x00ll;
	/* ---- */

	/* 
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	} 

	if( dev > (hmc->num_devs-1) ){ 

		/* 
	 	 * device out of range 
		 * 
		 */
	
		return -1;
	}

	/* 
	 * decide which values to set 
	 * 
	 */
	switch( bsize )
	{
		case 32: 
			tmp = 0x0000000000000000;
			break;
		case 64:
			tmp = 0x0000000000000001;
			break;
		case 128:
		default : 
			
			tmp = 0x0000000000000002;
			break;
	}

	/* 
	 * write the register 
	 * 
 	 */
	hmc->devs[dev].regs[HMC_REG_AC_IDX].reg |= tmp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_SET_ALL_MAX_BLOCKSIZE */
/* 
 * HMCSIM_UTIL_SET_ALL_MAX_BLOCKSIZE
 * See Table38 in the HMC Spec : pg 58
 * 
 */
extern int hmcsim_util_set_all_max_blocksize( struct hmcsim_t *hmc, uint32_t bsize )
{
	/* vars */
	uint32_t i = 0;
	/* ---- */
	
	if( hmc == NULL ){ 
		return -1;
	}

	/* 
	 * check the bounds of the block size
	 *
 	 */
	if( (bsize != 32) && 
		(bsize != 64) &&
		(bsize != 128) ){
		return -1;
	}

	for( i=0; i<hmc->num_devs; i++ ){ 
		hmcsim_util_set_max_blocksize( hmc, i, bsize );
	}
	
	return 0;
}

/* ----------------------------------------------------- HMCSIM_UTIL_GET_MAX_BLOCKSIZE */
/* 
 * HMCSIM_UTIL_GET_MAX_BLOCKSIZE
 * See Table38 in the HMC Spec : pg 58
 * 
 */
extern int hmcsim_util_get_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t *bsize )
{
	/* vars */
	uint64_t reg	= 0x00ll;
	uint64_t code	= 0x00ll;
	/* ---- */

	/* 
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	} 

	if( dev > (hmc->num_devs-1) ){ 

		/* 
	 	 * device out of range 
		 * 
		 */
	
		return -1;
	}

	if( bsize == NULL ){ 
		return -1;
	}

	/*
	 * retrieve the register value 
 	 * 
	 */
	reg = hmc->devs[dev].regs[HMC_REG_AC_IDX].reg;
	

	/* 
	 * get the lower four bits 
	 * 
	 */
	code = (reg & 0xF);

	/* 
	 * decode it for the standard 
	 * device initialization table
	 * 
	 */	
	switch( code )
	{
		case 0x0:
			/* 32 bytes */
			*bsize = 32;
			break;
		case 0x1:
			/* 64 bytes */
			*bsize = 64;
			break;
		case 0x2:
			/* 128 bytes */
			*bsize = 128;
			break;
		case 0x8:
			/* 32 bytes */
			*bsize = 32;
			break;
		case 0x9:
			/* 64 bytes */
			*bsize = 64;
			break;
		case 0xA:
			/* 32 bytes */
			*bsize = 32;
			break;
		case 0x3:
		case 0x4:
		case 0x5:
		case 0x6:
		case 0x7:
		case 0xB:
		case 0xC:
		case 0xD:
		case 0xE:
		case 0xF:
			/* 
	 		 * vendor specific
			 *
			 */
			break;
		default:
			break;
	}

	return 0;
}

/* EOF */
