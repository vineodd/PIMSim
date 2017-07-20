using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public static class CMC
    {
        static string __op_name = "TEMPLATE_OP";

        /* __rqst : Contains the respective command enum that the simulated
                  : application uses to initiate a request for this command.
                  : See hmc_rqst_t enums from hmc_sim_types.h
                  : MUST BE UNIQUE ACROSS CMC LIBS
        */
        static hmc_rqst __rqst = hmc_rqst.CMC04;

        /* __cmd : Contains the respective command code for this CMC operation.
                 : This MUST match the __rqst field.  For example, if we have
                 : CMC32 as the __rqst, then the __cmd is (UInt32)(32).
        */
        static UInt32 __cmd = 1;

        /* __rqst_len : Contains the respective command request packet len in flits
                      : Permissible values are 1->17.  This must include the header
                      : and tail flits.  Commands with just an address have 1 flit.
                      : Commands with data will include at least two flits.
                      : It is up to the implementor to decode the data flits
        */
        static UInt32 __rqst_len = 1;

        /* __rsp_len : Contains the respective command response packet len in flits
                     : Permissible values are 0->17.  This must include the header
                     : and tail flits.  If __rsp_len is 0, then the operation
                     : is assumed to be posted.
        */
        static UInt32 __rsp_len = 2;

        /* __rsp_cmd : Contains the respective response command.  See hmc_response_t
                     : enum in hmc_sim_types.h.  All normal commands are permissible.
                     : If RSP_CMC is selected, you must also set __rsp_cmd_code
        */
        static hmc_response __rsp_cmd = hmc_response. RD_RS;


        /* __rsp_cmd_code : Contains the command code for RSP_CMC command
                          : responses.  The code must be <= 127 decimal.
                          : Unused response commands are 64->127
        */
        static uint __rsp_cmd_code = 0x00;

        /* __transient_power : Contains the transient power of the respective
                             : CMC operation.  If this field is unknown,
                             : the CMC infrastructure will assume a value of 0.
        */
        static float __transient_power = 0.5f;

        /* __row_ops : Contains the number of row operations for the respective
                     : CMC operation.  If this field is unknown, the CMC
                     : infrastructure will assume a value of 1.
        */
        static UInt32 __row_ops = 2;

        /* ----------------------------------------------------- HMCSIM_EXECUTE_CMC */
        /*
         * Performs the actual CMC operation.  All your custom logic belongs in this
         * function.
         *
         * *hmc is a void pointer to the core hmc structure.  Note that this must
         *    be cast to (struct hmcsim_t *)
         * dev is the respective device where the op is occurring
         * quad is the respective quad where the op is occurring
         * vault is the respective vault where the op is occurring
         * bank is the respective bank where the op is occurring
         * addr is the base address of the incoming request
         * length is the length of the incoming request
         * head is the packet head
         * tail is the packet tail
         * *rqst_payload is the incoming request payload formatted as the maximum
         *    possible packet (256 bytes of data).  Its up to this function to
         *    pull the required bits from this payload.
         * *rsp_payload is the outgoing response data payload formatted as the
         *    maximum possible packet (256 bytes of data).  Its up to this function
         *    to write the required number of output bits in the response payload.
         *    Note that the calling infrastructure will only utilize the number of
         *    bytes as defined by the rsp_len of this CMC operation
         *
         */
        public static int hmcsim_execute_cmc(
                                        UInt32 dev,
                                        UInt32 quad,
                                        UInt32 vault,
                                        UInt32 bank,
                                        UInt64 addr,
                                        UInt32 length,
                                        UInt64 head,
                                        UInt64 tail,
                                        List<UInt64> rqst_payload,
                                        List<UInt64> rsp_payload)
        {
            /* perform your operation */

            return 0;
        }

        /* ----------------------------------------------------- HMCSIM_REGISTER_CMC */
        /*
         * Registers the target CMC library instance with the core simulation. This
         * function is loaded via dlopen and called from the HMC-Sim library when
         * the sim makes a call to hmcsim_load_cmc().  Most users will not need
         * to change this function.
         *
         * *rqst is a pointer to a valid hmc_rqst_t that defines which CMC operation
         *     command enum that this library will utilize.  See the hmc_rqst_t
         *     enums labeled CMCnn in ~/include/hmc_sim_types.h.
         *
         * *cmd is the respective command code that matches the *rqst command enum.
         *     For example, if *rqst returns CMC32, then the *cmd is "32".
         *
         * *rsp_len is the respective command's response packet length.
         *    This must fit within the standard HMC response packet sizes
         *
         * *rsp_cmd is the respective command's response command type.  See
         *    the values defined in the hmc_response_t enum in ~/include/hmc_sim_types.h
         *
         * *rsp_cmd_code is the respective command's response command code in raw form.
         *
         */
        public static int hmcsim_register_cmc(ref hmc_rqst rqst,
                                        ref UInt32 cmd,
                                        ref UInt32 rqst_len,
                                        ref UInt32 rsp_len,
                                        ref hmc_response rsp_cmd,
                                        ref uint rsp_cmd_code)
        {
            rqst = __rqst;
            cmd = __cmd;
            rqst_len = __rqst_len;
            rsp_len = __rsp_len;
            rsp_cmd = __rsp_cmd;
            rsp_cmd_code = __rsp_cmd_code;

            return 0;
        }
        
        /* ----------------------------------------------------- HMCSIM_CMC_STR */
        /*
         * Returns the name of the CMC operation for use in tracing
         * Most users will not need to change this function
         *
         * *out is the output string that is written to
         *
         */
        public static void hmcsim_cmc_str(ref string out_ )
        {
            out_ = __op_name;
        }


        /* ----------------------------------------------------- HMCSIM_CMC_POWER */
        /*
         * Returns the amount of transient power and the number of row operations
         * for this respective operation.  If these values are not known, then
         * the CMC infrastructure assumes a transient power of 0 and 1 row op.
         * Users can modify these values based upon the runtime of the operation.
         * This function is not called until AFTER the processing is complete
         *
         */
        public static void hmcsim_cmc_power(ref UInt32 row_ops, ref float tpower)
        {
            row_ops = __row_ops;
            tpower = __transient_power;
        }
    }
}
