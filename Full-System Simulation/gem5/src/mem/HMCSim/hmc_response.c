/*
 * _HMC_RESPONSE_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * MEMORY RESPONSE HANDLERS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"


/* ----------------------------------------------------- HMCSIM_DECODE_RSP_CMD */
/*
 * HMCSIM_DECODE_RSP_CMD
 *
 */
extern int	hmcsim_decode_rsp_cmd(	hmc_response_t rsp_cmd, uint8_t *cmd )
{
	switch( rsp_cmd )
	{
		case RD_RS:
			*cmd = 0x38;
			break;
		case WR_RS:
			*cmd = 0x39;
			break;
		case MD_RD_RS:
			*cmd = 0x3A;
			break;
		case MD_WR_RS:
			*cmd = 0x3B;
			break;
		case RSP_ERROR:
			*cmd = 0x3E;
			break;
		case RSP_NONE:
			*cmd = 0x00;
			break;
		default:
			*cmd = 0x00;
			break;
	}

	return HMC_OK;
}

/* ----------------------------------------------------- HMCSIM_DECODE_MEMRESPONSE */
/*
 * HMCSIM_DECODE_MEMRESPONSE
 *
 */
extern int	hmcsim_decode_memresponse( 	struct hmcsim_t *hmc,
						uint64_t *packet,
						uint64_t *response_head,
						uint64_t *response_tail,
						hmc_response_t *type,
						uint8_t *length,
						uint16_t *tag,
						uint8_t *rtn_tag,
						uint8_t *src_link,
						uint8_t *rrp,
						uint8_t *frp,
						uint8_t *seq,
						uint8_t *dinv,
						uint8_t *errstat,
						uint8_t *rtc,
						uint32_t *crc )
{
	/* vars */
	uint64_t tmp64	= 0x00ll;
	uint32_t tmp32	= 0x00000000;
	uint16_t tmp16	= 0x00;
	uint8_t  tmp8	= 0x00;
	/* ---- */

	if( hmc == NULL ){
		return -1;
	}

	if( packet == NULL ){
		return -1;
	}

	/*
	 * pull out and decode the packet header
	 *
	 */

	tmp64 = packet[0];
	*response_head	= tmp64;

	/*
	 * shift out cmd field
	 *
	 */
	tmp8	= (uint8_t)( tmp64 & 0x7F );

	switch( tmp8 )
	{
		case 0x00:
			*type	= RSP_NONE;
			break;
		case 0x38:
			/* 111000 */
			*type	= RD_RS;
			break;
		case 0x39:
			/* 111001 */
			*type	= WR_RS;
			break;
		case 0x3A:
			/* 111010 */
			*type	= MD_RD_RS;
			break;
		case 0x3B:
			/* 111011 */
			*type	= MD_WR_RS;
			break;
		case 0x3E:
			/* 111110 */
			*type	= RSP_ERROR;
			break;
		default:
                        /* ASSUME THE RESPONSE IS A CMC */
			*type	= RSP_CMC;
			printf( "response type failure\n" );
			break;
	}

	tmp8 = 0x00;

	/*
	 * packet length field
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 7) & 0x1F);
	*length	= tmp8;
	tmp8	= 0x00;

	/*
	 * tag field
	 *
	 */
        tmp16   = (uint16_t)( (tmp64 >> 12) & 0x7FF);
	*tag	= tmp16;
	tmp16	= 0x0000;

	/*
	 * return tag field
	 * !DEPRECATED!
 	 */
        *rtn_tag = *tag;
	//tmp16	= (uint16_t)( (tmp64 & 0x1FF000000) >> 24);
	//*rtn_tag = tmp16;

	/*
	 * source link field
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 39) & 0x1F );
	*src_link = tmp8;

	/*
 	 * done decoding the header
	 *
	 */

	/*
	 * decode the tail
	 *
	 */
	tmp64	= packet[ (*length*2)-1 ];
	*response_tail	= tmp64;

	/*
	 * rrp
	 *
	 */
	tmp8	= (uint8_t)( tmp64 & 0xFF );
	*rrp 	= tmp8;
	tmp8	= 0x00;

	/*
	 * frp
	 *
 	 */
        tmp8    = (uint8_t)( (tmp64 >> 9) & 0x1FF);
	*frp	= tmp8;
	tmp8 	= 0x00;

	/*
	 * sequence number
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 18) & 0x7);
	*seq	= tmp8;
	tmp8 	= 0x00;

	/*
	 * dinv
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 21) & 0x1);
	*dinv	= tmp8;
	tmp8	= 0x00;

	/*
	 * errstat
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 22) & 0x7F);
	*dinv	= tmp8;
	tmp8	= 0x00;

	/*
	 * rtc
	 *
	 */
        tmp8    = (uint8_t)( (tmp64 >> 29) & 0x7 );
	*rtc	= tmp8;
	tmp8	= 0x00;

	/*
	 * crc
	 *
	 */
	tmp32	= (uint32_t)( (tmp64 & 0xFFFFFFFF00000000) >> 32 );
	*crc	= tmp32;
	tmp32	= 0x00000000;


	return HMC_OK;
}

/* EOF */
