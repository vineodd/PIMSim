/*
 * _HMC_PROCESS_PACKET_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * HMC PACKET PROCESSORS FOR MEM OPS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern int	hmcsim_trace( struct hmcsim_t *hmc, char *str );
extern int	hmcsim_trace_rqst( 	struct hmcsim_t *hmc,
					char *rqst,
					uint32_t dev,
					uint32_t quad,
					uint32_t vault,
					uint32_t bank,
					uint64_t addr1,
					uint32_t size );
extern int	hmcsim_trace_stall(	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t quad,
					uint32_t vault,
					uint32_t src,
					uint32_t dest,
					uint32_t link,
					uint32_t slot,
					uint32_t type );
extern int	hmcsim_util_zero_packet( struct hmc_queue_t *queue );
extern int	hmcsim_util_decode_bank( struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t bsize,
					uint64_t addr,
					uint32_t *bank );
extern int	hmcsim_decode_rsp_cmd( 	hmc_response_t rsp_cmd,
					uint8_t *cmd );
extern uint32_t hmcsim_cmc_cmdtoidx( hmc_rqst_t rqst );
extern int  hmcsim_process_cmc( struct hmcsim_t *hmc,
                                uint32_t rawcmd,
                                uint32_t dev,
                                uint32_t quad,
                                uint32_t vault,
                                uint32_t bank,
                                uint64_t addr,
                                uint32_t length,
                                uint64_t head,
                                uint64_t tail,
                                uint64_t *rqst_payload,
                                uint64_t *rsp_payload,
                                uint32_t *rsp_len,
                                hmc_response_t *rsp_cmd,
                                uint8_t *raw_rsp_cmd );




/* ----------------------------------------------------- HMCSIM_PROCESS_RQST */
/*
 * HMCSIM_PROCESS_RQST
 *
 */
extern int	hmcsim_process_rqst( 	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t quad,
					uint32_t vault,
					uint32_t slot )
{
	/* vars */
	struct hmc_queue_t *queue	= NULL;
	uint64_t head			= 0x00ll;
	uint64_t tail			= 0x00ll;

	uint64_t rsp_head		= 0x00ll;
	uint64_t rsp_tail		= 0x00ll;
	uint64_t rsp_slid		= 0x00ll;
	uint64_t rsp_tag		= 0x00ll;
	uint64_t rsp_crc		= 0x00ll;
	uint64_t rsp_rtc		= 0x00ll;
	uint64_t rsp_seq		= 0x00ll;
	uint64_t rsp_frp		= 0x00ll;
	uint64_t rsp_rrp		= 0x00ll;
	uint32_t rsp_len		= 0x00;
	uint64_t packet[HMC_MAX_UQ_PACKET];

        uint64_t i;
        uint64_t rqst_payload[16];
        uint64_t rsp_payload[16];

	uint32_t cur			= 0x00;
	uint32_t error			= 0x00;
	uint32_t t_slot			= hmc->queue_depth+1;
	uint32_t j			= 0x00;
	uint32_t length			= 0x00;
	uint32_t cmd			= 0x00;
	uint32_t tag			= 0x00;
	uint32_t bsize			= 0x00;
	uint32_t bank			= 0x00;
	uint64_t addr			= 0x00ull;
	int no_response			= 0x00;
        int use_cmc                     = 0x00;
	hmc_response_t rsp_cmd		= RSP_ERROR;
	uint8_t tmp8			= 0x0;
	/* ---- */


	/*
	 * -- Description of error types --
	 * Given that the various requests can return
	 * varying results and errors, we define a
 	 * generic error type above that is handled
	 * when building the response packets.
	 * In this manner, we can signal a varying
	 * number of errors in the packet handlers
	 * without disrupting everything too much.
	 * The error codes are described as follows:
	 *
	 * error = 0 : no error has occurred [default]
	 * error = 1 : packet request exceeds maximum
	 *             block size [bsize]
	 *
 	 */


	if( hmc == NULL ){
		return -1;
	}

	/*
	 * Step 1: get the request
	 *
	 */
	queue	= &(hmc->devs[dev].quads[quad].vaults[vault].rqst_queue[slot]);
	head	= queue->packet[0];

	/* -- get the packet length [11:7] */
	length 	= (uint32_t)( (head >> 7) & 0x1F );

	/* -- cmd = [6:0] */
	cmd	= (uint32_t)(head & 0x7F);

	if( cmd == 0x00 ){
		/* command is flow control, dump out */
		no_response = 1;
		goto step4_vr;
	}

	/* -- decide where the tail is */
	tail	= queue->packet[ ((length*2)-1) ];

	/*
	 * Step 2: decode it
	 *
 	 */
	/* -- tag = [22:12] */
	tag	= (uint32_t)((head >> 12) & 0x7FF);

	/* -- addr = [57:24] */
	addr	= ((head >> 24) & 0x3FFFFFFFF );

	/* -- block size */
	hmcsim_util_get_max_blocksize( hmc, dev, &bsize );

	/* -- get the bank */
	hmcsim_util_decode_bank( hmc, dev, bsize, addr, &bank );

	/*
 	 * Step 3: find a response slot
	 *         if no slots available, then this operation must stall
	 *
 	 */

	/* -- find a response slot */
	cur = hmc->queue_depth-1;

	for( j=0; j<hmc->queue_depth; j++){

		if( hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[cur].valid == HMC_RQST_INVALID ){
			t_slot = cur;
		}

		cur--;
	}

	if( t_slot == hmc->queue_depth+1 ){

		/* STALL */

		queue->valid = HMC_RQST_STALLED;

		/*
		 * print a stall trace
		 *
		 */
		if( (hmc->tracelevel & HMC_TRACE_STALL) > 0 ){
			hmcsim_trace_stall(	hmc,
						dev,
						quad,
						vault,
						0,
						0,
						0,
						slot,
						1 );
		}

		return HMC_STALL;
	}

       /* zero the temp payloads */
       for( i=0; i<16; i++ ){
         rqst_payload[i] = 0x00ll;
         rsp_payload[i] = 0x00ll;
       }

	/*
	 * Step 3: perform the op
	 *
 	 */
	switch( cmd )
	{
		case 8:
			/* WR16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 9:
			/* WR32 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR32",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 10:
			/* WR48 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 48 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR48",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 11:
			/* WR64 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 64 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR64",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 12:
			/* WR80 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 80 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR80",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 13:
			/* WR96 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 96 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR96",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 14:
			/* WR112 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 112 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR112",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 15:
			/* WR128 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 128 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR128",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
                case 79:
			/* WR256 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 256 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"WR256",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;

		case 16:
			/* MD_WR */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"MD_WR",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = MD_WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 17:
			/* BWR */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"BWR",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

                        /* set the response length in flits */
                        rsp_len = 1;

			break;
		case 18:
			/* TWOADD8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"TWOADD8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			break;
		case 19:
			/* ADD16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"ADD16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

			break;
		case 24:
			/* P_WR16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 25:
			/* P_WR32 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR32",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 26:
			/* P_WR48 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 48 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR48",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 27:
			/* P_WR64 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 64 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR64",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 28:
			/* P_WR80 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 80 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR80",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 29:
			/* P_WR96 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 96 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR96",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 30:
			/* P_WR112 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 112 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR112",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 31:
			/* P_WR128 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 128 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR128",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
                case 95:
			/* P_WR256 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 256 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_WR256",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 33:
			/* P_BWR */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_BWR",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 34:
			/* P2ADD8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P2ADD8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 35:
			/* P2ADD16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P2ADD16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

			break;
		case 48:
			/* RD16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

			break;
		case 49:
			/* RD32 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD32",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 3;

			break;
		case 50:
			/* RD48 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 48 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD48",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 4;

			break;
		case 51:
			/* RD64 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 64 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD64",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 5;

			break;
		case 52:
			/* RD80 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 80 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD80",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 6;

			break;
		case 53:
			/* RD96 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 96 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD96",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 7;

			break;
		case 54:
			/* RD112 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 112 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD112",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 8;

			break;
		case  55:
			/* RD128 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 128 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD128",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 9;

			break;
                case 119:
			/* RD256 */

			/*
			 * check to see if we exceed maximum block size
			 *
			 */
			if( bsize < 256 ){
				error = 1;
				break;
			}

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"RD256",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 17;

			break;
		case 40:
			/* MD_RD */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"MD_RD",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = MD_RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

			break;
		case 0x00:
			/* FLOW_NULL */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"FLOW_NULL",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* signal no response packet required */
			no_response = 1;

			break;
		case 0x01:
			/* PRET */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"PRET",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* signal no response packet required */
			no_response = 1;

			break;
		case 0x02:
			/* TRET */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"TRET",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* signal no response packet required */
			no_response = 1;

			break;
		case 0x03:
			/* IRTRY */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"IRTRY",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* signal no response packet required */
			no_response = 1;

			break;
                /* -- begin extended atomics -- */
                case 82:
                        /* 2ADDS8R */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"2ADDS8R",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 83:
                        /* ADDS16R */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"ADDS16R",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 80:
                        /* INC8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"INC8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

                        break;
                case 84:
                        /* P_INC8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"P_INC8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
                        no_response = 1;

                        break;
                case 64:
                        /* XOR16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"XOR16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 65:
                        /* OR16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"OR16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 66:
                        /* NOR16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"NOR16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 67:
                        /* AND16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"AND16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 68:
                        /* NAND16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"NAND16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 96:
                        /* CASGT8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASGT8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 98:
                        /* CASGT16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASGT16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 97:
                        /* CASLT8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASLT8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 99:
                        /* CASLT16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASLT16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 100:
                        /* CASEQ8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASEQ8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 101:
                        /* CASZERO16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"CASZERO16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 105:
                        /* EQ8 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"EQ8",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

                        break;
                case 104:
                        /* EQ16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"EQ16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = WR_RS;

			/* set the response length in FLITS */
			rsp_len = 1;

                        break;
                case 81:
                        /* BWR8R */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"BWR8R",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;
                case 106:
                        /* SWAP16 */

			if( (hmc->tracelevel & HMC_TRACE_CMD) > 0 ){
				hmcsim_trace_rqst(	hmc,
							"SWAP16",
							dev,
							quad,
							vault,
							bank,
							addr,
							length );
			}

			/* set the response command */
			rsp_cmd = RD_RS;

			/* set the response length in FLITS */
			rsp_len = 2;

                        break;

                /* begin CMC commands */
                case 4:
                case 5:
                case 6:
                case 7:
                case 20:
                case 21:
                case 22:
                case 23:
                case 32:
                case 36:
                case 37:
                case 38:
                case 39:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 56:
                case 57:
                case 58:
                case 59:
                case 60:
                case 61:
                case 62:
                case 63:
                case 69:
                case 70:
                case 71:
                case 72:
                case 73:
                case 74:
                case 75:
                case 76:
                case 77:
                case 78:
                case 85:
                case 86:
                case 87:
                case 88:
                case 89:
                case 90:
                case 91:
                case 92:
                case 93:
                case 94:
                case 102:
                case 103:
                case 107:
                case 108:
                case 109:
                case 110:
                case 111:
                case 112:
                case 113:
                case 114:
                case 115:
                case 116:
                case 117:
                case 118:
                case 120:
                case 121:
                case 122:
                case 123:
                case 124:
                case 125:
                case 126:
                case 127:
#ifdef HMC_DEBUG
                        printf( "HMCSIM_PROCESS_PACKET: PROCESSING CMC PACKET REQUEST\n" );
#endif
                        /* CMC OPERATIONS */
                        use_cmc = 1;

                        /* -- copy the request payload */
                        for( i=1; i<(length*2)-1; i++ ){
                          rqst_payload[i-1] = queue->packet[i];
                        }


                        /* -- attempt to make a call to the cmc lib */
                        if( hmcsim_process_cmc( hmc,
                                                cmd,
                                                dev,
                                                quad,
                                                vault,
                                                bank,
                                                addr,
                                                length,
                                                head,
                                                tail,
                                                &(rqst_payload[0]),
                                                &(rsp_payload[0]),
                                                &rsp_len,
                                                &rsp_cmd,
                                                &tmp8)!=0){
                          /* error occurred */
                          return HMC_ERROR;
                        }

                        /* -- operation was successful */
                        /* -- decode the response and see if we need
                           -- to send a response
                        */
                        switch( rsp_cmd ){
                        case MD_RD_RS:
                        case MD_WR_RS:
                        case RSP_NONE:
                          /* no response packet */
                          no_response = 1;
                          break;
                        default:
                          /* response packet */
                          no_response = 0;
                          break;
                        }

                        break;
		default:
			break;
	}

	/*
 	 * Step 4: build and register the response with vault response queue
	 *
 	 */
step4_vr:
	if( no_response == 0 ){

		/* -- build the response */
		rsp_slid 	= ((tail>>26) & 0x07);
		rsp_tag		= tag;
		rsp_crc		= ((tail>>32) & 0xFFFFFFFF);
		rsp_rtc		= ((tail>>29) & 0x7);
		rsp_seq		= ((tail>>18) & 0x7);
		rsp_frp		= ((tail>>9) & 0x1FF);
		rsp_rrp		= (tail & 0xFF);

		/* -- decode the response command : see hmc_response.c */
                if( use_cmc != 1 ){
                  /* only decode the response if not using cmc */
		  hmcsim_decode_rsp_cmd( rsp_cmd, &(tmp8) );
                }

                /* -- packet head */
		rsp_head	|= (tmp8 & 0x7F);
		rsp_head	|= (rsp_len<<7);
		rsp_head	|= (rsp_tag<<12);
		rsp_head	|= (rsp_slid<<39);

		/* -- packet tail */
		rsp_tail	|= (rsp_rrp);
		rsp_tail	|= (rsp_frp<<9);
		rsp_tail	|= (rsp_seq<<18);
		rsp_tail	|= (rsp_rtc<<29);
		rsp_tail	|= (rsp_crc<<32);

		packet[0] 		= rsp_head;
		packet[((rsp_len*2)-1)]	= rsp_tail;

                /* build the cmc data payload */
                for( j=1; j<((rsp_len-1)*2); j++ ){
                  packet[j] = rsp_payload[j];
                }

		/* -- register the response */
		hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[t_slot].valid = HMC_RQST_VALID;
		for( j=0; j<rsp_len; j++ ){
			hmc->devs[dev].quads[quad].vaults[vault].rsp_queue[t_slot].packet[j] = packet[j];
		}

	}/* else, no response required, probably flow control */

	/*
	 * Step 5: invalidate the request queue slot
	 *
 	 */
	hmcsim_util_zero_packet( queue );

	return 0;
}

/* EOF */
