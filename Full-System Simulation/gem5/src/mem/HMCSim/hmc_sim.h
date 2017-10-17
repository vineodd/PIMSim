/* 
 * _HMC_SIM_H_
 * 
 * HYBRID MEMORY CUBE SIMULATION LIBRARY
 * 
 * MAIN HEADER FILE 
 * 
 */

/*!	\file hmc_sim.h
	\brief Hybrid Memory Cube Functional Simulation Library Header File
	
	The HMC_SIM library is a standard C library that provides functional
	simulation support for an arbitrarily configured Hybrid Memory Cube
	device.  This libraries enables users to experiment with various
	HMC configurations in order to perform research in available bandwidth, 
	memory addressing, connectivity and general simulation archictecture. 
*/




#ifndef _HMC_SIM_H_
#define _HMC_SIM_H_


#ifdef __cplusplus
extern "C" {
#endif


#include <stdint.h>
#include <sys/types.h>
#include "hmc_sim_types.h"
#include "hmc_sim_macros.h"

/* -------------------------------------------- FUNCTION PROTOTYPES */

/*!	\fn int hmcsim_init( struct hmcsim_t *hmc, 
				uint32_t num_devs, 
				uint32_t num_links, 
				uint32_t num_vaults, 
				uint32_t queue_depth,
				uint32_t num_banks, 
				uint32_t num_drams, 
				uint32_t capacity, 
				uint32_t xbar_depth )
	\brief Initializes the internal HMC_SIM state with the necessary
		hardware configuration options. 
	\param *hmc is a pointer to a valid hmc structure. Must not be null.
	\param num_devs is the number of target HMC devices in the configuration
	\param num_links is the number of links per HMC device in the configuration
	\param num_vaults is the number of TSV vaults per HMC device in the configuration
	\param queue_depth is the depth of each vaults request and response queues, respectively
	\param num_banks is the number of memory banks per vault in the configuration
	\param num_drams is the number of drams per bank in the configuration
	\param capacity is the capacity in GB per HMC device
	\param xbar_depth is the queue depth of each device's crossbar request and response queues, respectively
	\return 0 on success, nonzero otherwise. 
*/
extern int	hmcsim_init(	struct hmcsim_t *hmc, 
				uint32_t num_devs, 
				uint32_t num_links, 
				uint32_t num_vaults, 
				uint32_t queue_depth,
				uint32_t num_banks, 
				uint32_t num_drams, 
				uint32_t capacity, 
				uint32_t xbar_depth );

/*!	\fn int hmcsim_free( struct hmcsim_t *hmc )
	\brief Frees all the internal memory structures associated with the target configuration. 
		Does not free the memory at the *hmc pointer.  
	\param *hmc is a pointer to a valid hmc structure.  Must not be null. 
	\return 0 on success, nonzero otherwise.
*/
extern int	hmcsim_free( struct hmcsim_t *hmc );

/*!	\fn int hmcsim_link_config( struct hmcsim_t *hmc, 
				uint32_t src_dev,
				uint32_t dest_dev,
				uint32_t src_link, 
				uint32_t dest_link, 
				hmc_link_def_t type )
	\brief Initializes individual device links for host-to-device or device-to-device configurations. 
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param src_dev is the cube ID [CUB] of a source device.
	\param dest_dev is the cube ID [CUB] of a destination device.
	\param src_link is the link ID of the source link
	\param dest_link is the link ID of the destination link 
	\param type is one of HMC_LINK_HOST_DEV or HMC_LINK_DEV_DEV
	\return 0 on success, nonzero otherwise
*/
extern int	hmcsim_link_config( struct hmcsim_t *hmc, 
					uint32_t src_dev,
					uint32_t dest_dev, 
					uint32_t src_link,
					uint32_t dest_link, 
					hmc_link_def_t type );

/*!	\fn int hmcsim_trace_handle( struct hmcsim_t *hmc, FILE *tfile )
	\brief Sets the tracefile output file handle for tracing internal device operation
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param *tfile is a pointer to a valid and open file handle.  Must not be null
	\return 0 on success, nonzero otherwise 
*/
extern int	hmcsim_trace_handle( struct hmcsim_t *hmc, FILE *tfile );


/*!	\fn int hmcsim_trace_header( struct hmcsim_t *hmc )
	\brief Prints a comment block in the tracefile header for future reference.  The 
		trace handle must be set prior to making a call to this function.  
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\return 0 on success, nonzero otherwise 
*/
extern int	hmcsim_trace_header( struct hmcsim_t *hmc );

/*!	\fn int hmcsim_trace_level( struct hmcsim_t *hmc, uint32_t level )
	\brief Sets the trace level of an initialized HMC library instance. 
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param level is the target trace level.  Larger integers indicate more tracing output.
	\returns 0 on success, nonzero otherwise 
*/
extern int	hmcsim_trace_level( struct hmcsim_t *hmc, uint32_t level );

/*!	\fn int hmcsim_build_memrequest( struct hmcsim_t *hmc, 
                                        uint8_t  cub,
                                        uint64_t addr,
                                        uint16_t  tag,
                                        hmc_rqst_t type,
                                        uint8_t link,
                                        uint64_t *payload,
                                        uint64_t *rqst_head,
                                        uint64_t *rqst_tail )
	\brief Builds a memory request packet with up to 9 FLITS of data. 
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param cub is the cube ID of a valid device.
	\param addr is the starting physical address of the required data block
	\param tag is the host-specified tag field to identify packets
	\param type is the request type.  See hmc_rqst_t enumerated type definition.
	\param link the desingated transfer link ID
	\param *payload is a pointer to the data payload [for write operations]
	\param *rqst_head is a pointer to an unsigned 8 byte location where the function
		shall return the completed packet request header in proper form. 
	\param *rqst_tail is a pointer to an unsigned 8 byte location where the function
		shall return the completed packet request tail in proper form. 
	\return 0 on success, nonzero otherwise 
*/
extern int      hmcsim_build_memrequest( struct hmcsim_t *hmc,
                                        uint8_t  cub,
                                        uint64_t addr,
                                        uint16_t  tag,
                                        hmc_rqst_t type,
                                        uint8_t link,
                                        uint64_t *payload,
                                        uint64_t *rqst_head,
                                        uint64_t *rqst_tail );

/*!	\fn int hmcsim_decode_memresponse( struct hmcsim_t *hmc, 
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
	\brief Decodes a valid response packet with up to 9 FLITS of data
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param *packet is a pointer to an unsigned 8 byte location where the packet is located
	\param *response_head is a pointer to an unsigned 8 byte location where the function
		shall return the entire response packet header intact
	\param *response_tail is a pointer to an unsigned 8 byte location where the function
		shall return the entire response packet tail intact
	\param *type is a pointer to a return type that specifies the return type of the packet
	\param *length is a pointer to an unsigned 8 bit integer that contans the packet length 
	\param *tag is a pointer to an unsigned 16 bit integer tha contains the tag
	\param *rtn_tag is a pointer to an unsigned 8 bit integer that contains the return tag
	\param *src_link is a pointer to an unsigned 8 bit integer that contains the source link ID
	\param *rrp is a pointer to an unsigned 8 bit integer that contains the contents of the RRP field
	\param *frp is a pointer to an unsigned 8 bit integer that contains the contents of the FRP field
	\param *seq is a pointer to an unsigned 8 bit integer that contains the sequence number
	\param *dinv is a pointer to an unsigned 8 bit integer that contains the contents of the DINV field
	\param *errstat is a pointer to an unsigned 8 bit integer that contains the contents of the errstat field 
	\param *rtc is a pointer to an unsigned 8 bit integer that contains the contents of the RTC field
	\param *crc is a pointer to an unsigned 32 bit integer that contains the CRC32 checksum 
	\return 0 on success, nonzero otherwise
*/	
extern int      hmcsim_decode_memresponse(      struct hmcsim_t *hmc,
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
                                                uint32_t *crc );

/*!	\fn int hmcsim_send( struct hmcsim_t *hmc, uint64_t *packet )
	\brief Attempts to send the target packet to an HMC device
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param *packet is a pointer to a valid packet structure of up to 9 FLITS
	\return HMC_OK on success, HMC_ERROR on error, HMC_STALL when there are no xbar queue slots available
*/
extern int	hmcsim_send( struct hmcsim_t *hmc, uint64_t *packet );

/*!	\fn int hmcsim_recv( struct hmcsim_t *hmc, uint32_t dev, uint32_t link, uint64_t *packet )
	\brief Attempts to poll an HMC device for a response packet
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param dev is the target device cube ID [cub] to poll for a response packet
	\param link is the target link on the respective cube device to poll for a response packet
	\param *packet is a pointer to a valid memory-back packet array to contain up to 9 FLITS of data
	\return HMC_OK on success, HMC_ERROR on error, HMC_STALL when there are no response packets available
*/

extern int	hmcsim_recv( struct hmcsim_t *hmc, uint32_t dev, uint32_t link, uint64_t *packet );

/*!	\fn int hmcsim_clock( struct hmcsim_t *hmc )
	\brief Instantiates a single leading edge and falling edge clock cycle on all devices  
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\return 0 on success, nonzero otherwise
*/
extern int	hmcsim_clock( struct hmcsim_t *hmc );

/*!     \fn uint64_t hmcsim_get_clock( struct hmcsim_t *hmc )
        \brief Retrieves the current value of the HMC clock from the *hmc structure
        \param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
        \return The current clock value
*/
extern uint64_t hmcsim_get_clock( struct hmcsim_t *hmc );

/*!	\fn int hmcsim_jtag_reg_read( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t *result )
	\brief Attempts to read the value from the specified HMC device register for the target device
		using the side-band JTAG interface
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param dev is the target device cube ID [cub] to read from
	\param reg is the target register to read the value
	\param *result is a pointer to an unsigned 64-bit storage location used to store the returned value. 
		Must be backed by a valid memory region. 
	\return 0 on success, nonzero otherwise
*/
extern int	hmcsim_jtag_reg_read( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t *result );

/*!	\fn int hmcsim_jtag_reg_write( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t value )
	\brief Attempts to write the value to the specified HMC device register for the target device
		using the side-band JTAG interface
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param dev is the target device cube ID [cub] to read from
	\param reg is the target register whose value is to be written
	\param value is the value to be written
	\return 0 on success, nonzero otherwise
*/
extern int	hmcsim_jtag_reg_write( struct hmcsim_t *hmc, uint32_t dev, uint64_t reg, uint64_t value );


/*!	\fn int hmcsim_util_set_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t bsize )
	\brief Sets the maximum request size for the target device
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param dev is the target device cube ID [cub] to read from
	\param bsize is the target block size [32,64,128]
	\return 0 on success, nonzero otherwise 
*/
extern int	hmcsim_util_set_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t bsize );

/*!	\fn int hmcsim_util_set_all_max_blocksize( struct hmcsim_t *hmc, uint32_t bsize )
	\brief Sets the maximum request size for all the devices
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param bsize is the target block size [32,64,128]
	\return 0 on success, nonzero otherwise 
*/
extern int	hmcsim_util_set_all_max_blocksize( struct hmcsim_t *hmc, uint32_t bsize );


/*!	\fn int hmcsim_util_get_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t *bsize )
	\brief Gets the maximum request size for the target device
	\param *hmc is a pointer to a valid and initialized hmc structure.  Must not be null. 
	\param dev is the target device cube ID [cub] to read from
	\param *bsize is a pointer to a valid uint32_t location that will contain the max block size
	\return 0 on success, nonzero otherwise 
*/
extern int	hmcsim_util_get_max_blocksize( struct hmcsim_t *hmc, uint32_t dev, uint32_t *bsize );

/*!
        \fn int hmcsim_load_cmc( struct hmcsim_t *cmc, char *cmc_lib )
        \brief Load the CMC library into the current simulation context
        \param *cmc is a pointer to a valid and initialized hmc structure.  Must not be null.
        \param *cmc_lib is a character string that contains the full path to the CMC library to load
        \return 0 on success, nonzero otherwise 
*/
extern int      hmcsim_load_cmc( struct hmcsim_t *hmc, char *cmc_lib );

#ifdef __cplusplus
} /* extern C */
#endif

#endif	/* _HMC_SIM_H_ */

/* EOF */
