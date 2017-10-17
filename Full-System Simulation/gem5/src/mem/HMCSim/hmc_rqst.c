/*
 * _HMC_RQST_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * MEMORY REQUEST HANDLERS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"


/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int hmcsim_query_cmc( struct hmcsim_t *hmc,
                             hmc_rqst_t type,
                             uint32_t *flits,
                             uint8_t *cmd );



/* ----------------------------------------------------- HMCSIM_RQST_GETSEQ */
/*
 * HMCSIM_RQST_GETSEQ
 *
 */
static uint8_t hmcsim_rqst_getseq( struct hmcsim_t *hmc, hmc_rqst_t type )
{
	if( (type == PRET) || (type == IRTRY) ){
		return hmc->seq;
	}

	hmc->seq++;

	if( hmc->seq > 0x07 ){
		hmc->seq = 0x00;
	}

	return 0x01;
}

/* ----------------------------------------------------- HMCSIM_RQST_GETRRP */
/*
 * HMCSIM_RQST_GETRRP
 *
 */
static uint8_t hmcsim_rqst_getrrp( struct hmcsim_t *hmc )
{
	return 0x03;
}

/* ----------------------------------------------------- HMCSIM_RQST_GETFRP */
/*
 * HMCSIM_RQST_GETFRP
 *
 */
static uint8_t hmcsim_rqst_getfrp( struct hmcsim_t *hmc )
{
	return 0x02;
}

/* ----------------------------------------------------- HMCSIM_RQST_GETRTC */
/*
 * HMCSIM_RQST_GETRTC
 *
 */
static uint8_t hmcsim_rqst_getrtc( struct hmcsim_t *hmc )
{
	return 0x01;
}


/* ----------------------------------------------------- HMCSIM_CRC32 */
/*
 * HMCSIM_CRC32
 *
 */
static uint32_t hmcsim_crc32( uint64_t addr, uint64_t *payload, uint32_t len )
{
	/* vars */
	uint32_t crc	= 0x11111111;
	/* ---- */

        /* FIXME : REPORT THE TRUE CRC */
	return crc;
}

/* ----------------------------------------------------- HMCSIM_BUILD_MEMREQUEST */
/*
 * HMCSIM_BUILD_MEMREQUEST
 *
 */
extern int	hmcsim_build_memrequest( struct hmcsim_t *hmc,
					uint8_t  cub,
					uint64_t addr,
					uint16_t  tag,
					hmc_rqst_t type,
					uint8_t link,
					uint64_t *payload,
					uint64_t *rqst_head,
					uint64_t *rqst_tail )
{
	/* vars */
	uint8_t cmd	= 0x00;
	uint8_t rrp	= 0x00;
	uint8_t frp	= 0x00;
	uint8_t seq	= 0x00;
	uint8_t rtc	= 0x00;
	uint32_t crc	= 0x00000000;
	uint32_t flits	= 0x00000000;
	uint64_t tmp	= 0x00ll;
	/* ---- */

	if( hmc == NULL ){
		return -1;
	}


	/*
	 * we do no validation of inputs here
	 * users may want to deliberately create bogus
	 * requests
	 *
	 */

	/*
	 * get the correct command bit sequence
	 *
	 */
	switch( type )
	{
		case WR16:
			flits	= 2;
			cmd	= 0x08;		/* 001000 */
			break;
		case WR32:
			flits	= 3;
			cmd	= 0x09;		/* 001001 */
			break;
		case WR48:
			flits	= 4;
			cmd	= 0x0A;		/* 001010 */
			break;
		case WR64:
			flits	= 5;
			cmd	= 0x0B;		/* 001011 */
			break;
		case WR80:
			flits	= 6;
			cmd	= 0x0C;		/* 001100 */
			break;
		case WR96:
			flits	= 7;
			cmd	= 0x0D;		/* 001101 */
			break;
		case WR112:
			flits	= 8;
			cmd	= 0x0E;		/* 001110 */
			break;
		case WR128:
			flits	= 9;
			cmd	= 0x0F;		/* 001111 */
			break;
                case WR256:
                        flits   = 17;
                        cmd     = 79;
		case MD_WR:
			flits	= 2;
			cmd	= 0x10;		/* 010000 */
			break;
		case BWR:
			flits	= 2;
			cmd	= 0x11;		/* 010001 */
			break;
		case TWOADD8:
			flits	= 2;
			cmd	= 0x12;		/* 010010 */
			break;
		case ADD16:
			flits	= 2;
			cmd	= 0x13;		/* 010011 */
			break;
		case P_WR16:
			flits	= 2;
			cmd	= 0x18;		/* 011000 */
			break;
		case P_WR32:
			flits	= 3;
			cmd	= 0x19;		/* 011001 */
			break;
		case P_WR48:
			flits	= 4;
			cmd	= 0x1A;		/* 011010 */
			break;
		case P_WR64:
			flits	= 5;
			cmd	= 0x1B;		/* 011011 */
			break;
		case P_WR80:
			flits	= 6;
			cmd	= 0x1C;		/* 011100 */
			break;
		case P_WR96:
			flits	= 7;
			cmd	= 0x1D;		/* 011101 */
			break;
		case P_WR112:
			flits	= 8;
			cmd	= 0x1E;		/* 011110 */
			break;
		case P_WR128:
			flits	= 9;
			cmd	= 0x1F;		/* 011111 */
			break;
                case P_WR256:
                        flits   = 17;
                        cmd     = 95;
                        break;
		case P_BWR:
			flits	= 2;
			cmd	= 0x21;		/* 100001 */
			break;
		case P_2ADD8:
			flits	= 2;
			cmd	= 0x22;		/* 100010 */
			break;
		case P_ADD16:
			flits	= 2;
			cmd	= 0x23;		/* 100011 */
			break;
		case RD16:
			flits	= 1;
			cmd	= 0x30;		/* 110000 */
			break;
		case RD32:
			flits	= 1;
			cmd	= 0x31;		/* 110001 */
			break;
		case RD48:
			flits	= 1;
			cmd	= 0x32;		/* 110010 */
			break;
		case RD64:
			flits	= 1;
			cmd	= 0x33;		/* 110011 */
			break;
		case RD80:
			flits	= 1;
			cmd	= 0x34;		/* 110100 */
			break;
		case RD96:
			flits	= 1;
			cmd	= 0x35;		/* 110101 */
			break;
		case RD112:
			flits	= 1;
			cmd	= 0x36;		/* 110110 */
			break;
		case RD128:
			flits	= 1;
			cmd	= 0x37;		/* 110111 */
			break;
                case RD256:
                        flits   = 1;
                        cmd     = 119;
                        break;
		case MD_RD:
			flits	= 1;
			cmd	= 0x28;		/* 101000 */
			break;
		case FLOW_NULL:
			flits	= 0;
			cmd	= 0x00;		/* 000000 */
			break;
		case PRET:
			flits	= 1;
			cmd	= 0x01;		/* 000001 */
			break;
		case TRET:
			flits	= 1;
			cmd	= 0x02;		/* 000010 */
			break;
		case IRTRY:
			flits	= 1;
			cmd	= 0x03;		/* 000011 */
			break;
                case TWOADDS8R:
                        flits   = 2;
                        cmd     = 82;
                        break;
                case ADDS16R:
                        flits   = 2;
                        cmd     = 83;
                        break;
                case INC8:
                        flits   = 1;
                        cmd     = 80;
                        break;
                case P_INC8:
                        flits   = 1;
                        cmd     = 84;
                        break;
                case XOR16:
                        flits   = 2;
                        cmd     = 64;
                        break;
                case OR16:
                        flits   = 2;
                        cmd     = 65;
                        break;
                case NOR16:
                        flits   = 2;
                        cmd     = 66;
                        break;
                case AND16:
                        flits   = 2;
                        cmd     = 67;
                        break;
                case NAND16:
                        flits   = 2;
                        cmd     = 68;
                        break;
                case CASGT8:
                        flits   = 2;
                        cmd     = 96;
                        break;
                case CASGT16:
                        flits   = 2;
                        cmd     = 98;
                        break;
                case CASLT8:
                        flits   = 2;
                        cmd     = 97;
                        break;
                case CASLT16:
                        flits   = 2;
                        cmd     = 99;
                        break;
                case CASEQ8:
                        flits   = 2;
                        cmd     = 100;
                        break;
                case CASZERO16:
                        flits   = 2;
                        cmd     = 101;
                        break;
                case EQ8:
                        flits   = 2;
                        cmd     = 105;
                        break;
                case EQ16:
                        flits   = 2;
                        cmd     = 104;
                        break;
                case BWR8R:
                        flits   = 2;
                        cmd     = 81;
                        break;
                case SWAP16:
                        flits   = 2;
                        cmd     = 106;
                        break;
                /* CMC OPS */
                case CMC04:
                case CMC05:
                case CMC06:
                case CMC07:
                case CMC20:
                case CMC21:
                case CMC22:
                case CMC23:
                case CMC32:
                case CMC36:
                case CMC37:
                case CMC38:
                case CMC39:
                case CMC41:
                case CMC42:
                case CMC43:
                case CMC44:
                case CMC45:
                case CMC46:
                case CMC47:
                case CMC56:
                case CMC57:
                case CMC58:
                case CMC59:
                case CMC60:
                case CMC61:
                case CMC62:
                case CMC63:
                case CMC69:
                case CMC70:
                case CMC71:
                case CMC72:
                case CMC73:
                case CMC74:
                case CMC75:
                case CMC76:
                case CMC77:
                case CMC78:
                case CMC85:
                case CMC86:
                case CMC87:
                case CMC88:
                case CMC89:
                case CMC90:
                case CMC91:
                case CMC92:
                case CMC93:
                case CMC94:
                case CMC102:
                case CMC103:
                case CMC107:
                case CMC108:
                case CMC109:
                case CMC110:
                case CMC111:
                case CMC112:
                case CMC113:
                case CMC114:
                case CMC115:
                case CMC116:
                case CMC117:
                case CMC118:
                case CMC120:
                case CMC121:
                case CMC122:
                case CMC123:
                case CMC124:
                case CMC125:
                case CMC126:
                case CMC127:

#ifdef HMC_DEBUG
                        printf( "HMCSIM_BUILD_MEMREQUEST : CMC PACKET TYPE = %d\n", type );
#endif
                        /* check for an active cmc op */
                        if( hmcsim_query_cmc( hmc,
                                              type,
                                              &flits,
                                              &cmd ) != 0 ){
                          /* no active cmc op */
                          return -1;
                        }

                        /*
                         * cmc op is active
                         * flits and cmd are initialized
                         */
#ifdef HMC_DEBUG
                        printf( "HMCSIM_BUILD_MEMREQUEST : CMC PACKET COMMAND = %d\n", cmd );
#endif
                        break;
		default:
			return -1;
			break;
	}

	/*
	 * build the request packet header
	 *
	 */

	/* -- cmd field : bits 6:0 */
	tmp |= (cmd & 0x7F);

	/* -- lng field in flits : bits 11:7 */
	tmp |= ( (uint64_t)(flits & 0x1F) << 7 );

	/* -- dln field; duplicate of lng : bits 14:11 */
        /* this is disabled in the 2.0 spec */
	//tmp |= ( (uint64_t)(flits & 0xF) << 11 );

	/* -- tag field: bits 22:12 */
	tmp |= ( (uint64_t)(tag & 0x7FF) << 12 );

	/* -- address field : bits 57:24 */
	tmp |= ( (addr& 0x3FFFFFFFF) << 24 );

	/* -- cube id field : bits 63:61 */
	tmp |= ( (uint64_t)(cub&0x7) << 61 );

	/* write the request header out */
	*rqst_head	= tmp;

	tmp = 0x00ll;

	/*
	 * build the request packet tail
	 *
	 */

	/* -- return retry pointer : bits 8:0 */
	rrp = hmcsim_rqst_getrrp( hmc );
	tmp |= rrp;

	/* -- forward retry pointer : bits 17:9 */
	frp = hmcsim_rqst_getfrp( hmc );
	tmp |= ( (uint64_t)(frp & 0x1FF) << 9 );

	/* -- sequence number : bits 20:18 */
	seq = hmcsim_rqst_getseq( hmc, type );
	tmp |= ( (uint64_t)(seq & 0x7) << 18 );

        /* -- data valid bit : bit 21 */
        tmp |= ( (uint64_t)(0x1<<21) );

	/* -- source source link id : bits 28:26 */
	tmp |= ( (uint64_t)(link & 0x7) << 26 );

        /* -- error status bits : bits 28:22 */
        /* -- no errors are present */

	/* -- return token count : bits 31:29 */
	rtc = hmcsim_rqst_getrtc( hmc );
	tmp |= ( (uint64_t)(rtc & 0x7) << 29 );

	/* -- retrieve the crc : bits 63:32 */
	crc = hmcsim_crc32( addr, payload, (2*flits) );
	tmp |= ( (uint64_t)(crc & 0xFFFFFFFF) << 32 );

	/* write the request tail out */
	*rqst_tail	= tmp;

	return 0;
}

/* EOF */
