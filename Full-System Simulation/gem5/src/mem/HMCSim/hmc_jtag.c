/* 
 * _HMC_JTAG_C_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY 
 * 
 * HMC JTAG READ AND WRITE FUNCTIONS
 * 
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"



/* ----------------------------------------------------- HMCSIM_JTAG_WRITE_GC */
/* 
 * HMCSIM_JTAG_WRITE_GC
 * 
 */
static int hmcsim_jtag_write_gc( struct hmcsim_t *hmc, uint64_t dev, uint64_t value )
{

	/* 
	 * check for warm reset 
	 * 
 	 */
	if( (value & 0x40) > 0 ){
		/*
		 * initiate a warm reset
	 	 *
		 */
	}

	/* 
	 * check for error clear
	 * 
	 */
	if( (value * 0x20 ) > 0 ){
		/* 
		 * clear all the errors
		 * 
		 */
	}	

	uint64_t temp = 0x00ll;
	temp = (value & 0x1F);
	
	hmc->devs[dev].regs[HMC_REG_GC_IDX].reg |= temp;

	return 0;
}

/* ----------------------------------------------------- HMCSIM_JTAG_WRITE_ERR */
/* 
 * HMCSIM_JTAG_WRITE_ERR
 * 
 */
static int hmcsim_jtag_write_err( struct hmcsim_t *hmc, uint64_t dev, uint64_t value )
{
	/* 
	 * or' out the lower 25 bits
	 * 
	 */
	uint64_t temp = 0x00ll;
	temp = (value & 0x3FFFFFF);

	hmc->devs[dev].regs[HMC_REG_ERR_IDX].reg |= temp;
	

	/* 
	 * check and see if we need to write
	 * the start bit
	 * 
	 */
	if( (value & 0x80000000 ) > 0 ){
		hmc->devs[dev].regs[HMC_REG_ERR_IDX].reg	|= 0x80000000;
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_JTAG_REG_READ */
/* 
 * HMCSIM_JTAG_REG_READ
 * 
 */
extern int	hmcsim_jtag_reg_read( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t *result )
{
	if( hmc == NULL ){ 
		return -1;
	}	

	if( dev > hmc->num_devs ){
		return -1;
	}

	switch( reg )
	{
		case HMC_REG_EDR0:
			*result	= hmc->devs[dev].regs[HMC_REG_EDR0_IDX].reg;
			break;
		case HMC_REG_EDR1:
			*result	= hmc->devs[dev].regs[HMC_REG_EDR1_IDX].reg;
			break;
		case HMC_REG_EDR2:
			*result	= hmc->devs[dev].regs[HMC_REG_EDR2_IDX].reg;
			break;
		case HMC_REG_EDR3:
			*result	= hmc->devs[dev].regs[HMC_REG_EDR3_IDX].reg;
			break;
		case HMC_REG_ERR:
			*result	= hmc->devs[dev].regs[HMC_REG_ERR_IDX].reg;
			break;
		case HMC_REG_GC:
			*result	= hmc->devs[dev].regs[HMC_REG_GC_IDX].reg;
			break;
		case HMC_REG_LC0:
			*result	= hmc->devs[dev].regs[HMC_REG_LC0_IDX].reg;
			break;
		case HMC_REG_LC1:
			*result	= hmc->devs[dev].regs[HMC_REG_LC1_IDX].reg;
			break;
		case HMC_REG_LC2:
			*result	= hmc->devs[dev].regs[HMC_REG_LC2_IDX].reg;
			break;
		case HMC_REG_LC3:
			*result	= hmc->devs[dev].regs[HMC_REG_LC3_IDX].reg;
			break;
		case HMC_REG_LRLL0:
			*result	= hmc->devs[dev].regs[HMC_REG_LRLL0_IDX].reg;
			break;
		case HMC_REG_LRLL1:
			*result	= hmc->devs[dev].regs[HMC_REG_LRLL1_IDX].reg;
			break;
		case HMC_REG_LRLL2:
			*result	= hmc->devs[dev].regs[HMC_REG_LRLL2_IDX].reg;
			break;
		case HMC_REG_LRLL3:
			*result	= hmc->devs[dev].regs[HMC_REG_LRLL3_IDX].reg;
			break;
		case HMC_REG_LR0:
			*result	= hmc->devs[dev].regs[HMC_REG_LR0_IDX].reg;
			break;
		case HMC_REG_LR1:
			*result	= hmc->devs[dev].regs[HMC_REG_LR1_IDX].reg;
			break;
		case HMC_REG_LR2:
			*result	= hmc->devs[dev].regs[HMC_REG_LR2_IDX].reg;
			break;
		case HMC_REG_LR3:
			*result	= hmc->devs[dev].regs[HMC_REG_LR3_IDX].reg;
			break;
		case HMC_REG_IBTC0:
			*result	= hmc->devs[dev].regs[HMC_REG_IBTC0_IDX].reg;
			break;
		case HMC_REG_IBTC1:
			*result	= hmc->devs[dev].regs[HMC_REG_IBTC1_IDX].reg;
			break;
		case HMC_REG_IBTC2:
			*result	= hmc->devs[dev].regs[HMC_REG_IBTC2_IDX].reg;
			break;
		case HMC_REG_IBTC3:
			*result	= hmc->devs[dev].regs[HMC_REG_IBTC3_IDX].reg;
			break;
		case HMC_REG_AC:
			*result	= hmc->devs[dev].regs[HMC_REG_AC_IDX].reg;
			break;
		case HMC_REG_VCR:
			*result	= hmc->devs[dev].regs[HMC_REG_VCR_IDX].reg;
			break;
		case HMC_REG_FEAT:
			*result	= hmc->devs[dev].regs[HMC_REG_FEAT_IDX].reg;
			break;
		case HMC_REG_RVID:
			*result	= hmc->devs[dev].regs[HMC_REG_RVID_IDX].reg;
			break;
		default:
			return -1;
	}

	return 0;
}

/* ----------------------------------------------------- HMCSIM_JTAG_REG_WRITE */
/* 
 * HMCSIM_JTAG_REG_WRITE
 * 
 */
extern int	hmcsim_jtag_reg_write( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t value )
{
	if( hmc == NULL ){ 
		return -1;
	}	

	if( dev > hmc->num_devs ){
		return -1;
	}

	switch( reg )
	{
		case HMC_REG_EDR0:
			hmc->devs[dev].regs[HMC_REG_EDR0_IDX].reg	= value;
			break;
		case HMC_REG_EDR1:
			hmc->devs[dev].regs[HMC_REG_EDR1_IDX].reg	= value;
			break;
		case HMC_REG_EDR2:
			hmc->devs[dev].regs[HMC_REG_EDR2_IDX].reg	= value;
			break;
		case HMC_REG_EDR3:
			hmc->devs[dev].regs[HMC_REG_EDR3_IDX].reg	= value;
			break;
		case HMC_REG_ERR:
			hmcsim_jtag_write_err( hmc, dev, value );
			break;
		case HMC_REG_GC:
			hmcsim_jtag_write_gc( hmc, dev, value );
			break;
		case HMC_REG_LC0:
			hmc->devs[dev].regs[HMC_REG_LC0_IDX].reg	= value;
			break;
		case HMC_REG_LC1:
			hmc->devs[dev].regs[HMC_REG_LC1_IDX].reg	= value;
			break;
		case HMC_REG_LC2:
			hmc->devs[dev].regs[HMC_REG_LC2_IDX].reg	= value;
			break;
		case HMC_REG_LC3:
			hmc->devs[dev].regs[HMC_REG_LC3_IDX].reg	= value;
			break;
		case HMC_REG_LRLL0:
			hmc->devs[dev].regs[HMC_REG_LRLL0_IDX].reg	= value;
			break;
		case HMC_REG_LRLL1:
			hmc->devs[dev].regs[HMC_REG_LRLL1_IDX].reg	= value;
			break;
		case HMC_REG_LRLL2:
			hmc->devs[dev].regs[HMC_REG_LRLL2_IDX].reg	= value;
			break;
		case HMC_REG_LRLL3:
			hmc->devs[dev].regs[HMC_REG_LRLL3_IDX].reg	= value;
			break;
		case HMC_REG_LR0:
			hmc->devs[dev].regs[HMC_REG_LR0_IDX].reg	= value;
			break;
		case HMC_REG_LR1:
			hmc->devs[dev].regs[HMC_REG_LR1_IDX].reg	= value;
			break;
		case HMC_REG_LR2:
			hmc->devs[dev].regs[HMC_REG_LR2_IDX].reg	= value;
			break;
		case HMC_REG_LR3:
			hmc->devs[dev].regs[HMC_REG_LR3_IDX].reg	= value;
			break;
		case HMC_REG_IBTC0:
			hmc->devs[dev].regs[HMC_REG_IBTC0_IDX].reg	= value;
			break;
		case HMC_REG_IBTC1:
			hmc->devs[dev].regs[HMC_REG_IBTC1_IDX].reg	= value;
			break;
		case HMC_REG_IBTC2:
			hmc->devs[dev].regs[HMC_REG_IBTC2_IDX].reg	= value;
			break;
		case HMC_REG_IBTC3:
			hmc->devs[dev].regs[HMC_REG_IBTC3_IDX].reg	= value;
			break;
		case HMC_REG_AC:
			hmc->devs[dev].regs[HMC_REG_AC_IDX].reg		= value;
			break;
		case HMC_REG_VCR:
			hmc->devs[dev].regs[HMC_REG_VCR_IDX].reg	= value;
			break;
		case HMC_REG_FEAT:
			/*
			 * Read-Only
			 *
			 */
			return HMC_ERROR;
			break;
		case HMC_REG_RVID:
			/*
			 * Read-Only
			 *
			 */
			return HMC_ERROR;
			break;
		default:
			return -1;
	}

	return 0;
}

/* EOF */
