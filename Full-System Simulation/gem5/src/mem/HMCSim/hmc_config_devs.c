/*
 * _HMC_CONFIG_DEVS_C_
 *
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 *
 * HMC DEVICE CONFIGURATION FUNCTIONS
 *
 */


#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "hmc_sim.h"

/* ----------------------------------------------------- HMCSIM_CONFIG_CMC */
/*
 * HMCSIM_CONFIG_CMC
 *
 */
static int    hmcsim_config_cmc( struct hmcsim_t *hmc ){

      /* vars */
      int i = 0;
      /* ---- */

      for( i=0; i<HMC_MAX_CMC; i++ ){
        hmc->cmcs[i].type         = FLOW_NULL;  /* default to a null op */
        hmc->cmcs[i].cmd          = 0x00ll;
        hmc->cmcs[i].rsp_len      = 0x0;
        hmc->cmcs[i].rsp_cmd_code = 0x0;
        hmc->cmcs[i].active       = 0;          /* disable this op by default */
        hmc->cmcs[i].handle       = NULL;
        hmc->cmcs[i].cmc_register = NULL;
        hmc->cmcs[i].cmc_execute  = NULL;
        hmc->cmcs[i].cmc_str      = NULL;
      }

      return 0;
}

/* ----------------------------------------------------- HMCSIM_CONFIG_FEAT_REG */
/*
 * HMCSIM_CONFIG_FEAT_REG
 *
 */
static int	hmcsim_config_feat_reg( struct hmcsim_t *hmc, uint32_t dev )
{
	/*
	 * write the necessary data for the feature register
	 *
	 */
	uint64_t feat	= 0x00ll;
	uint64_t size	= 0x00ll;
	uint64_t vaults = 0x00ll;
	uint64_t banks  = 0x00ll;
	uint64_t phy	= HMC_PHY_SPEED;

	/*
	 * determine capacity
	 *
	 */
	switch( hmc->capacity )
	{
		case 2 :
			size = 0x00;
			break;
		case 4 :
			size = 0x01;
			break;
		case 8 :
			size = 0x02;
			break;
		case 16:
			size = 0x03;
			break;
		default:
			/*
	 		 * we currently don't support vendor specific
		     	 * capacities
			 *
			 */
			size = 0x00;
			break;
	}

	/*
	 * determine vaults
	 *
	 */
	switch( hmc->num_vaults )
	{
		case 16:
			vaults = 0x00;
			break;
		case 32:
			vaults = 0x01;
			break;
		default:
			/*
	 		 * we currently don't support vendor specific
		     	 * vaults
			 *
			 */
			vaults = 0x00;
			break;
	}

	/*
	 * banks per vault
	 *
	 */
	switch( hmc->num_banks )
	{
		case 8:
			banks = 0x00;
			break;
		case 16:
			banks = 0x01;
			break;
		default:
			/*
	 		 * we currently don't support vendor specific
		     	 * banks
			 *
			 */
			banks = 0x00;
			break;
	}

	feat |= size;
	feat |= (vaults << 4 );
	feat |= (banks  << 8 );
	feat |= (phy    << 12);

	/*
	 * write the register
 	 *
	 */
	hmc->devs[dev].regs[HMC_REG_FEAT_IDX].reg	= feat;


	return 0;
}

/* ----------------------------------------------------- HMCSIM_CONFIG_RVID_REG */
/*
 * HMCSIM_CONFIG_RVID_REG
 *
 */
static int	hmcsim_config_rvid_reg( struct hmcsim_t *hmc, uint32_t dev )
{
	/*
	 * write the necessary data for the revision, vendor, product
	 * protocol and phy
 	 *
	 */

	uint64_t rev = 0x00ll;

	/*
	 * vendor
	 *
	 */
	rev |= HMC_VENDOR_ID;

	/*
	 * product revision
	 *
 	 */
	rev |= (uint64_t)(HMC_PRODUCT_REVISION << 8 );


	/*
	 * protocol revision
	 *
	 */
	rev |= (uint64_t)(HMC_PROTOCOL_REVISION << 16 );

	/*
	 * phy revision
	 *
 	 */
	rev |= (uint64_t)(HMC_PHY_REVISION << 24 );

	/*
	 * write the register
	 *
 	 */
	hmc->devs[dev].regs[HMC_REG_RVID_IDX].reg	= rev;

	return 0;
}


/* ----------------------------------------------------- HMCSIM_CONFIG_DEV_REG */
/*
 * HMCSIM_CONFIG_DEV_REG
 *
 */
static int	hmcsim_config_dev_reg( struct hmcsim_t *hmc, uint32_t dev )
{

	hmc->devs[dev].regs[HMC_REG_EDR0_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_EDR0_IDX].phy_idx	= HMC_REG_EDR0; 
	hmc->devs[dev].regs[HMC_REG_EDR0_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_EDR1_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_EDR1_IDX].phy_idx	= HMC_REG_EDR1; 
	hmc->devs[dev].regs[HMC_REG_EDR1_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_EDR2_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_EDR2_IDX].phy_idx	= HMC_REG_EDR2; 
	hmc->devs[dev].regs[HMC_REG_EDR2_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_EDR3_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_EDR3_IDX].phy_idx	= HMC_REG_EDR3; 
	hmc->devs[dev].regs[HMC_REG_EDR3_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_ERR_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_ERR_IDX].phy_idx	= HMC_REG_ERR; 
	hmc->devs[dev].regs[HMC_REG_ERR_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_GC_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_GC_IDX].phy_idx	= HMC_REG_GC; 
	hmc->devs[dev].regs[HMC_REG_GC_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LC0_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LC0_IDX].phy_idx	= HMC_REG_LC0; 
	hmc->devs[dev].regs[HMC_REG_LC0_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LC1_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LC1_IDX].phy_idx	= HMC_REG_LC1; 
	hmc->devs[dev].regs[HMC_REG_LC1_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LC2_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LC2_IDX].phy_idx	= HMC_REG_LC2; 
	hmc->devs[dev].regs[HMC_REG_LC2_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LC3_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LC3_IDX].phy_idx	= HMC_REG_LC3; 
	hmc->devs[dev].regs[HMC_REG_LC3_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LRLL0_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LRLL0_IDX].phy_idx	= HMC_REG_LRLL0; 
	hmc->devs[dev].regs[HMC_REG_LRLL0_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LRLL1_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LRLL1_IDX].phy_idx	= HMC_REG_LRLL1; 
	hmc->devs[dev].regs[HMC_REG_LRLL1_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LRLL2_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LRLL2_IDX].phy_idx	= HMC_REG_LRLL2; 
	hmc->devs[dev].regs[HMC_REG_LRLL2_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LRLL3_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LRLL3_IDX].phy_idx	= HMC_REG_LRLL3; 
	hmc->devs[dev].regs[HMC_REG_LRLL3_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LR0_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LR0_IDX].phy_idx	= HMC_REG_LR0; 
	hmc->devs[dev].regs[HMC_REG_LR0_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LR1_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LR1_IDX].phy_idx	= HMC_REG_LR1; 
	hmc->devs[dev].regs[HMC_REG_LR1_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LR2_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LR2_IDX].phy_idx	= HMC_REG_LR2; 
	hmc->devs[dev].regs[HMC_REG_LR2_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_LR3_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_LR3_IDX].phy_idx	= HMC_REG_LR3; 
	hmc->devs[dev].regs[HMC_REG_LR3_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_IBTC0_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_IBTC0_IDX].phy_idx	= HMC_REG_IBTC0; 
	hmc->devs[dev].regs[HMC_REG_IBTC0_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_IBTC1_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_IBTC1_IDX].phy_idx	= HMC_REG_IBTC1; 
	hmc->devs[dev].regs[HMC_REG_IBTC1_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_IBTC2_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_IBTC2_IDX].phy_idx	= HMC_REG_IBTC2; 
	hmc->devs[dev].regs[HMC_REG_IBTC2_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_IBTC3_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_IBTC3_IDX].phy_idx	= HMC_REG_IBTC3; 
	hmc->devs[dev].regs[HMC_REG_IBTC3_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_AC_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_AC_IDX].phy_idx	= HMC_REG_AC; 
	hmc->devs[dev].regs[HMC_REG_AC_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_VCR_IDX].type	= HMC_RW; 
	hmc->devs[dev].regs[HMC_REG_VCR_IDX].phy_idx	= HMC_REG_VCR; 
	hmc->devs[dev].regs[HMC_REG_VCR_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_FEAT_IDX].type	= HMC_RO;
	hmc->devs[dev].regs[HMC_REG_FEAT_IDX].phy_idx	= HMC_REG_FEAT; 
	hmc->devs[dev].regs[HMC_REG_FEAT_IDX].reg	= 0x00ll;

	hmc->devs[dev].regs[HMC_REG_RVID_IDX].type	= HMC_RO;
	hmc->devs[dev].regs[HMC_REG_RVID_IDX].phy_idx	= HMC_REG_RVID; 
	hmc->devs[dev].regs[HMC_REG_RVID_IDX].reg	= 0x00ll;


	/*
	 * write the feature revision register
	 *
 	 */
	hmcsim_config_feat_reg( hmc, dev );


	/*
	 * write the device revision register
	 *
 	 */
	hmcsim_config_rvid_reg( hmc, dev );

	return 0;
}


/* ----------------------------------------------------- HMCSIM_CONFIG_DEVICES */
/*
 * HMCSIM_CONFIG_DEVICES
 *
 */
extern int	hmcsim_config_devices( struct hmcsim_t *hmc )
{
	/* vars */
	uint32_t i		= 0;
	uint32_t j		= 0;
	uint32_t k		= 0;
	uint32_t x		= 0;
	uint32_t y		= 0;
	uint32_t a		= 0;
	uint32_t cur_quad	= 0;
	uint32_t cur_vault	= 0;
	uint32_t cur_bank	= 0;
	uint32_t cur_dram	= 0;
	uint32_t cur_link	= 0;
	uint32_t cur_queue	= 0;
	uint32_t cur_xbar	= 0;
	/* ---- */

	/* 
	 * sanity check 
	 * 
	 */
	if( hmc == NULL ){ 
		return -1;
	}

        /*
         * init the cmc structure
         *
         */
        hmcsim_config_cmc( hmc );


	/* 
	 * set the device pointers
	 * 
	 */
	hmc->devs	= hmc->__ptr_devs;		

	/* 
	 * zero the sequence number
	 * 
	 */
	hmc->seq 	= 0x00;

	/* 
	 * for each device, set the sub-device pointers
	 * 
	 */

	for( i=0; i<hmc->num_devs; i++ ){

		/* 
		 * set the id 
		 *
		 */
		hmc->devs[i].id	= i;

		/* 
	 	 * zero the sequence number
		 * 
		 */
		hmc->devs[i].seq = 0x00;

		/* 
	 	 * config the register file 
		 * 
		 */
		hmcsim_config_dev_reg( hmc, i );

		/* 
		 * links on each device
		 *
		 */
		hmc->devs[i].links	= &(hmc->__ptr_links[cur_link]);

		/* 
		 * xbars on each device
		 * 
		 */
		hmc->devs[i].xbar	= &(hmc->__ptr_xbars[cur_link]);

		for( j=0; j<hmc->num_links; j++ ){

			/* 
			 * xbar queues
			 * 
			 */
			hmc->devs[i].xbar[j].xbar_rqst= &(hmc->__ptr_xbar_rqst[cur_xbar]);
			hmc->devs[i].xbar[j].xbar_rsp = &(hmc->__ptr_xbar_rsp[cur_xbar]);

#if 0
			printf( "hmc->devs[].xbar[].xbar_rsp  = 0x%016llx\n", 
							(uint64_t)&(hmc->__ptr_xbar_rsp[cur_xbar]) );
#endif

			for( a=0; a<hmc->xbar_depth; a++){ 
				hmc->devs[i].xbar[j].xbar_rqst[a].valid	= HMC_RQST_INVALID;
				hmc->devs[i].xbar[j].xbar_rsp[a].valid	= HMC_RQST_INVALID;
			}

			cur_xbar += hmc->xbar_depth;

			/* 
			 * set the id 
			 *
			 */
			hmc->devs[i].links[j].id	= j;

			/* 
			 * set the type and cubs 
			 * by default, everyone connects to the host
			 *
			 */	
			hmc->devs[i].links[j].type	= HMC_LINK_HOST_DEV;
			hmc->devs[i].links[j].src_cub	= hmc->num_devs+1;
			hmc->devs[i].links[j].dest_cub	= i;
		
			/* 
			 * set the associated quad
			 * quad == link 
			 */
			hmc->devs[i].links[j].quad = j;
		}

		cur_link += hmc->num_links;

		/* 
		 * quads on each device
		 * 
		 */
		hmc->devs[i].quads	= &(hmc->__ptr_quads[cur_quad]);

		for( j=0; j<hmc->num_links; j++ ){ 

			/* 
			 * set the id 
			 *
			 */
			hmc->devs[i].quads[j].id	= j;

			/* 
			 * vaults in each quad
			 *
			 */
			hmc->devs[i].quads[j].vaults	= &(hmc->__ptr_vaults[cur_vault]);

			//for( k=0; k<hmc->num_vaults; k++ ){ 
			for( k=0; k<4; k++ ){ 

				/* 
				 * set the id 
				 *
				 */
				hmc->devs[i].quads[j].vaults[k].id	= k;
	
				/* 
				 * banks in each vault
				 * 
				 */
				hmc->devs[i].quads[j].vaults[k].banks	= &(hmc->__ptr_banks[cur_bank]);

				/* 
				 * request and response queues 
				 * 
				 */
				hmc->devs[i].quads[j].vaults[k].rqst_queue	= &(hmc->__ptr_vault_rqst[cur_queue]);
				hmc->devs[i].quads[j].vaults[k].rsp_queue	= &(hmc->__ptr_vault_rsp[cur_queue]);
			
				/* 
				 * clear the valid bits
				 * 
				 */	
				for( a=0; a<hmc->queue_depth; a++ ){
					hmc->devs[i].quads[j].vaults[k].rqst_queue[a].valid	= HMC_RQST_INVALID;
					hmc->devs[i].quads[j].vaults[k].rsp_queue[a].valid	= HMC_RQST_INVALID;
				}
				
				for( x=0; x<hmc->num_banks; x++ ){ 

					/* 
					 * set the id 
					 *
					 */
					hmc->devs[i].quads[j].vaults[k].banks[x].id	= x;

					/* 
					 * drams in each bank
					 * 
					 */
#if 0
					hmc->devs[i].quads[j].vaults[k].banks[x].drams = 
									&(hmc->__ptr_drams[cur_dram]);	
#endif

					for( y=0; y<hmc->num_drams; y++ ){ 
			
						/* 
						 * set the id
						 *
						 */
#if 0
						hmc->devs[i].quads[j].vaults[k].banks[x].drams[y].id = y;
#endif
					}

					cur_dram += hmc->num_drams;

				}
			
				cur_bank += hmc->num_banks; 

				cur_queue += hmc->queue_depth;	
			}

			//cur_queue += hmc->queue_depth;	
			//cur_vault += hmc->num_vaults;
			cur_vault += 4;

		}

		cur_quad+=hmc->num_quads;
	}

	return 0;
}

/* EOF */
