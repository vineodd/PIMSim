/*
 * _HMC_TRACE_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * HMC TRACE CONFIGURATION/EXEC FUNCTIONS
 *
 */


#include <stdio.h>
#include <inttypes.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"


/* ----------------------------------------------------- FUNCTION PROTOTYPES */
extern void hmcsim_cmc_trace_header( struct hmcsim_t *hmc );



/* ----------------------------------------------------- HMCSIM_TRACE_HEADER */
/*
 * HMCSIM_TRACE_HEADER
 *
 */
extern int	hmcsim_trace_header( struct hmcsim_t *hmc )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	/*
	 * write all the necessary sim data to the trace file
	 * as a large comment block
	 *
	 */
	fprintf( hmc->tfile, "%s\n", 	"#---------------------------------------------------------" );
	fprintf( hmc->tfile, "%s\n", 	"# HMC-SIM VERSION : HMC_MAJOR_VERSION.HMC_MINOR_VERSION" ); 
	fprintf( hmc->tfile, "%s\n", 	"# DATE: " );
	fprintf( hmc->tfile, "%s\n", 	"#---------------------------------------------------------" );
	fprintf( hmc->tfile, "%s%d\n",	"# HMC_NUM_DEVICES       = ", hmc->num_devs );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_NUM_LINKS         = ", hmc->num_links );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_NUM_QUADS         = ", hmc->num_quads );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_NUM_VAULTS        = ", hmc->num_vaults );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_NUM_BANKS         = ", hmc->num_banks );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_NUM_DRAMS         = ", hmc->num_drams );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_CAPACITY_GB       = ", hmc->capacity );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_VAULT_QUEUE_DEPTH = ", hmc->queue_depth );
	fprintf( hmc->tfile, "%s%d\n",  "# HMC_XBAR_QUEUE_DEPTH  = ", hmc->xbar_depth );
	fprintf( hmc->tfile, "%s\n", 	"#---------------------------------------------------------" );

        /* -- todo; print the cmc info */
        hmcsim_cmc_trace_header(hmc);

        fflush( hmc->tfile );

	return 0;
}

/* ----------------------------------------------------- HMCSIM_TRACE_STALL */
/*
 * HMCSIM_TRACE_STALL
 *
 */
extern int	hmcsim_trace_stall( 	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t quad,
					uint32_t vault,
					uint32_t src,
					uint32_t dest,
					uint32_t link,
					uint32_t slot,
					uint32_t type )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	/*
	 * Determine which stall type
 	 *
	 */
	if( type == 0 ){

		/*
		 * xbar stall
	 	 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : XBAR_RQST_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					quad,
					vault,
					slot );

	} else if( type == 1 ){

		/*
	 	 * vault request stall
		 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : VAULT_RQST_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					quad,
					vault,
					slot );


	} else if( type == 2 ){

		/*
		 * xbar response stall
	 	 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : XBAR_RSP_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					quad,
					vault,
					slot );

	} else if( type == 3 ){

		/*
		 * device request route stall
	 	 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : ROUTE_RQST_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					src,
					dest,
					link,
					slot );

	}else if( type == 4 ){

		/*
		 * device response route stall
	 	 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : ROUTE_RSP_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					src,
					dest,
					link,
					slot );

	}else {

		/*
		 * undefined stall event
		 *
		 */

		fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
					" : UNDEF_STALL"
					" : %" PRIu32
					":%" PRIu32
					":%" PRIu32
					":%" PRIu32 "\n",
					hmc->clk,
					dev,
					quad,
					vault,
					slot );

	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_TRACE_RQST */
/*
 * HMCSIM_TRACE_RQST
 *
 */
extern int	hmcsim_trace_rqst( 	struct hmcsim_t *hmc,
					char *rqst,
					uint32_t dev,
					uint32_t quad,
					uint32_t vault,
					uint32_t bank,
					uint64_t addr1,
					uint32_t size )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
				" : %s"
				" : %" PRIu32
				":%" PRIu32
				":%" PRIu32
				":%" PRIu32
				":0x%016" PRIx64
				":%" PRIu32 "\n",
				hmc->clk,
				rqst,
				dev,
				quad,
				vault,
				bank,
				addr1,
				size );

	return 0;

}
/* ----------------------------------------------------- HMCSIM_TRACE_BANK_CONFLICT */
/*
 * HMCSIM_TRACE_BANK_CONFLICT
 *
 */
extern int	hmcsim_trace_bank_conflict( struct hmcsim_t *hmc,
						uint32_t dev,
						uint32_t quad,
						uint32_t vault,
						uint32_t bank,
						uint64_t addr1 )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
				" : BANK_CONFLICT : %" PRIu32
				":%" PRIu32
				":%" PRIu32
				":%" PRIu32
				":0x%016" PRIx64 "\n",
				hmc->clk,
				dev,
				quad,
				vault,
				bank,
				addr1 );

	return 0;

}

/* ----------------------------------------------------- HMCSIM_TRACE_LATENCY */
/*
 * HMCSIM_TRACE_LATENCY
 *
 */
extern int	hmcsim_trace_latency( 	struct hmcsim_t *hmc,
					uint32_t dev,
					uint32_t link,
					uint32_t slot,
					uint32_t quad,
					uint32_t vault )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
				" : XBAR_LATENCY : %" PRIu32
				":%" PRIu32
				":%" PRIu32
				":%" PRIu32
				":%" PRIu32 "\n",
				hmc->clk,
				dev,
				link,
				slot,
				quad,
				vault );

	return 0;
}

/* ----------------------------------------------------- HMCSIM_TRACE */
/*
 * HMCSIM_TRACE
 *
 */
extern int	hmcsim_trace( struct hmcsim_t *hmc, char *str )
{
	if( hmc->tfile == NULL ){
		return -1;
	}

	fprintf( hmc->tfile, 	"HMCSIM_TRACE : %" PRIu64
				" : %s\n",
				hmc->clk,
				str );

	/*
	fprintf( hmc->tfile, "%s%ld%s%s\n", 	"HMCSIM_TRACE : ", 
					hmc->clk, 
					" : ", 
					str );
	*/

	return 0;
}

/* ----------------------------------------------------- HMCSIM_TRACE_LEVEL */
/*
 * HMCSIM_TRACE_LEVEL
 *
 */
extern int	hmcsim_trace_level( struct hmcsim_t *hmc, uint32_t level )
{
	if( hmc == NULL ){
		return -1;
	}

	hmc->tracelevel	= level;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_TRACE_HANDLE */
/*
 * HMCSIM_TRACE_HANDLE
 *
 */
extern int	hmcsim_trace_handle( struct hmcsim_t *hmc, FILE *tfile )
{
	if( hmc == NULL ){
		return -1;
	}

	if( tfile == NULL ){
		return -1;
	}

	hmc->tfile = tfile;

	return 0;
}

/* EOF */
