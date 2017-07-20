using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.Memory.HMC
{
    public partial  class HMCSim
    {
        public int hmcsim_process_rqst(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 slot)
        {
            /* vars */
            hmc_queue queue = null;
            UInt64 head = 0x00L;
            UInt64 tail = 0x00L;

            UInt64 rsp_head = 0x00L;
            UInt64 rsp_tail = 0x00L;
            UInt64 rsp_slid = 0x00L;
            UInt64 rsp_tag = 0x00L;
            UInt64 rsp_crc = 0x00L;
            UInt64 rsp_rtc = 0x00L;
            UInt64 rsp_seq = 0x00L;
            UInt64 rsp_frp = 0x00L;
            UInt64 rsp_rrp = 0x00L;
            UInt32 rsp_len = 0x00;
            UInt64[] packet = new UInt64[Macros.HMC_MAX_UQ_PACKET];

            UInt64 i;
            UInt64[] rqst_payload = new UInt64[16];
            UInt64[] rsp_payload = new UInt64[16];

            UInt32 cur = 0x00;
            UInt32 error = 0x00;
            UInt32 t_slot = this.queue_depth + 1;
            UInt32 j = 0x00;
            UInt32 length = 0x00;
            UInt32 cmd = 0x00;
            UInt32 tag = 0x00;
            UInt32 bsize = 0x00;
            UInt32 bank = 0x00;
            UInt64 addr = 0x00UL;
            int no_response = 0x00;
            int use_cmc = 0x00;
            hmc_response rsp_cmd = hmc_response.RSP_ERROR;
            uint tmp8 = 0x0;
            UInt32 row_ops = 0x00;
            float tpower = 0.0f;
            UInt32 op_latency = 0;
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



            if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
            {
                hmcsim_power_vault_rqst_slot(dev, quad, vault, slot);
            }

            /*
             * Step 1: get the request
             *
             */
            queue = this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)slot];
            head = queue.packet[0];

            /* -- get the packet length [11:7] */
            length = (UInt32)((head >> 7) & 0x1F);

            /* -- cmd = [6:0] */
            cmd = (UInt32)(head & 0x7F);

            if (cmd == 0x00)
            {
                /* command is flow control, dump out */
                no_response = 1;
                goto step4_vr;
            }

            /* -- decide where the tail is */
            tail = queue.packet[((length * 2) - 1)];

            /*
             * Step 2: decode it
             *
             */
            /* -- tag = [22:12] */
            tag = (UInt32)((head >> 12) & 0x3FF);

            /* -- addr = [57:24] */
            addr = ((head >> 24) & 0x1FFFFFFFF);

            /* -- block size */
            hmcsim_util_get_max_blocksize(dev, ref bsize);

            /* -- get the bank */
            hmcsim_util_decode_bank(dev, bsize, addr, ref bank);

            /* Return stall if the bank is not available */
            if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].delay > 0)
            {
                queue.valid = (uint)Macros.HMC_RQST_STALLED;
                if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                {
                    hmcsim_trace_stall(
                            dev,
                        quad,
                        vault,
                        0,
                        0,
                        0,
                        slot,
                        1);
                }
                return Macros.HMC_STALL;
            }

            /*
             * Step 3: find a response slot
             *         if no slots available, then this operation must stall
             *
             */

            /* -- find a response slot */
            if (false)
            {
                for (j = 0; j < this.queue_depth; j++)
                {
                    if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[(int)j].valid == Macros.HMC_RQST_INVALID)
                    {
                        t_slot = j;
                        break;
                    }
                }
            }
            /* if our dram latency is set to zero, the logic should bypass
             * the bank delay, go ahead and find a response slot
             */
            if (this.dramlatency == 0)
            {
                cur = this.queue_depth - 1;
                t_slot = this.queue_depth + 1;
                for (j = 0; j < this.queue_depth; j++)
                {
                    if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[(int)cur].valid == Macros.HMC_RQST_INVALID)
                    {
                        t_slot = cur;
                    }
                    cur--;
                }

                if (t_slot == this.queue_depth + 1)
                {
                    /* STALL */
                    queue.valid = Macros.HMC_RQST_STALLED;

                    /*
                     * print a stall trace
                     *
                     */
                    if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                    {

                        hmcsim_trace_stall(
                                    dev,
                                    quad,
                                    vault,
                                    0,
                                    0,
                                    0,
                                    slot,
                                    1);
                    }

                    return Macros.HMC_STALL;
                }
            }/* end this.dramlatency */

            /* zero the temp payloads */
            for (i = 0; i < 16; i++)
            {
                rqst_payload[i] = 0x00L;
                rsp_payload[i] = 0x00L;
            }

            /*
             * Step 3: perform the op
             *
             */
            switch (cmd)
            {
                case 8:
                    /* WR16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 9:
                    /* WR32 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR32",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 10:
                    /* WR48 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 48)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR48",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 11:
                    /* WR64 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 64)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR64",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 12:
                    /* WR80 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 80)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR80",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 13:
                    /* WR96 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 96)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR96",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 14:
                    /* WR112 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 112)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR112",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 15:
                    /* WR128 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 128)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR128",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 79:
                    /* WR256 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 256)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "WR256",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;

                case 16:
                    /* MD_WR */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "MD_WR",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.MD_WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 17:
                    /* BWR */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "BWR",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in flits */
                    rsp_len = 1;

                    break;
                case 18:
                    /* TWOADD8 */
                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "TWOADD8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 19:
                    /* ADD16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "ADD16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 24:
                    /* P_WR16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 25:
                    /* P_WR32 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR32",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 26:
                    /* P_WR48 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 48)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR48",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 27:
                    /* P_WR64 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 64)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR64",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 28:
                    /* P_WR80 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 80)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR80",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 29:
                    /* P_WR96 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 96)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR96",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 30:
                    /* P_WR112 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 112)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR112",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 31:
                    /* P_WR128 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 128)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR128",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 95:
                    /* P_WR256 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 256)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_WR256",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 33:
                    /* P_BWR */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_BWR",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 34:
                    /* P2ADD8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P2ADD8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 35:
                    /* P2ADD16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P2ADD16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 48:
                    /* RD16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 49:
                    /* RD32 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD32",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 3;

                    break;
                case 50:
                    /* RD48 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 48)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD48",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 4;

                    break;
                case 51:
                    /* RD64 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 64)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD64",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 5;

                    break;
                case 52:
                    /* RD80 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 80)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD80",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 6;

                    break;
                case 53:
                    /* RD96 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 96)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD96",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 7;

                    break;
                case 54:
                    /* RD112 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 112)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD112",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 8;

                    break;
                case 55:
                    /* RD128 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 128)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD128",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 9;

                    break;
                case 119:
                    /* RD256 */

                    /*
                     * check to see if we exceed maximum block size
                     *
                     */
                    if (bsize < 256)
                    {
                        error = 1;
                        break;
                    }

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "RD256",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 17;

                    break;
                case 40:
                    /* MD_RD */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "MD_RD",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 1);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.MD_RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 0x00:
                    /* FLOW_NULL */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "FLOW_NULL",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }

                    /* signal no response packet required */
                    no_response = 1;

                    break;
                case 0x01:
                    /* PRET */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "PRET",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }

                    /* signal no response packet required */
                    no_response = 1;

                    break;
                case 0x02:
                    /* TRET */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "TRET",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }

                    /* signal no response packet required */
                    no_response = 1;

                    break;
                case 0x03:
                    /* IRTRY */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "IRTRY",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }

                    /* signal no response packet required */
                    no_response = 1;

                    break;
                /* -- begin extended atomics -- */
                case 82:
                    /* 2ADDS8R */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "2ADDS8R",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 83:
                    /* ADDS16R */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "ADDS16R",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 80:
                    /* INC8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "INC8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 84:
                    /* P_INC8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "P_INC8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    no_response = 1;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    break;
                case 64:
                    /* XOR16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "XOR16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 65:
                    /* OR16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "OR16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 66:
                    /* NOR16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "NOR16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 67:
                    /* AND16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "AND16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 68:
                    /* NAND16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "NAND16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 96:
                    /* CASGT8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASGT8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 98:
                    /* CASGT16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASGT16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 97:
                    /* CASLT8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASLT8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 99:
                    /* CASLT16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASLT16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 100:
                    /* CASEQ8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASEQ8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 101:
                    /* CASZERO16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "CASZERO16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 105:
                    /* EQ8 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "EQ8",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 104:
                    /* EQ16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "EQ16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.WR_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 1;

                    break;
                case 81:
                    /* BWR8R */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "BWR8R",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

                    /* set the response length in FLITS */
                    rsp_len = 2;

                    break;
                case 106:
                    /* SWAP16 */

                    if ((this.tracelevel & Macros.HMC_TRACE_CMD) > 0)
                    {

                        hmcsim_trace_rqst(
                                    "SWAP16",
                                    dev,
                                    quad,
                                    vault,
                                    bank,
                                    addr,
                                    length);
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, 2);
                    }

                    /* set the response command */
                    rsp_cmd = hmc_response.RD_RS;

                    /* set the latency */
                    op_latency = this.dramlatency;

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
                    if (HMC_DEBUG)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("HMCSIM_PROCESS_PACKET: PROCESSING CMC PACKET REQUEST");
                    }
                    /* CMC OPERATIONS */
                    use_cmc = 1;

                    /* -- copy the request payload */
                    for (i = 1; i < (length * 2) - 1; i++)
                    {
                        rqst_payload[i - 1] = queue.packet[i];
                    }


                    /* -- attempt to make a call to the cmc lib */
                    if (hmcsim_process_cmc(
                                            cmd,
                                            dev,
                                            quad,
                                            vault,
                                            bank,
                                            addr,
                                            length,
                                            head,
                                            tail,
                                            rqst_payload,
                                            rsp_payload,
                                            ref rsp_len,
                                            ref rsp_cmd,
                                            ref tmp8,
                                            ref row_ops,
                                            ref tpower) != 0)
                    {
                        /* error occurred */
                        return Macros.HMC_ERROR;
                    }

                    /* power measurement */
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_row_access(addr, row_ops);
                        hmcsim_power_vault_ctrl_transient(vault, tpower);
                    }

                    /* -- operation was successful */
                    /* -- decode the response and see if we need
                       -- to send a response
                    */
                    op_latency = this.dramlatency * row_ops;

                    switch (rsp_cmd)
                    {
                        case hmc_response.MD_RD_RS:
                        case hmc_response.MD_WR_RS:
                        case hmc_response.RSP_NONE:
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
            if (no_response == 0)
            {
                if (HMC_DEBUG)
                {
                    HMCSIM_PRINT_TRACE("HANDLING PACKET RESPONSE");
                }
                /* -- build the response */
                rsp_slid = ((tail >> 26) & 0x07);
                rsp_tag = tag;
                rsp_crc = ((tail >> 32) & 0xFFFFFFFF);
                rsp_rtc = ((tail >> 29) & 0x7);
                rsp_seq = ((tail >> 18) & 0x7);
                rsp_frp = ((tail >> 9) & 0x1FF);
                rsp_rrp = (tail & 0xFF);

                /* -- decode the response command : see hmc_response.c */
                if (use_cmc != 1)
                {
                    /* only decode the response if not using cmc */
                    hmcsim_decode_rsp_cmd(rsp_cmd, ref tmp8);
                }

                /* -- packet head */
                rsp_head |= (tmp8 & 0x7F);
                rsp_head |= (rsp_len << 7);
                rsp_head |= (rsp_tag << 12);
                rsp_head |= (rsp_slid << 39);

                /* -- packet tail */
                rsp_tail |= (rsp_rrp);
                rsp_tail |= (rsp_frp << 9);
                rsp_tail |= (rsp_seq << 18);
                rsp_tail |= (rsp_rtc << 29);
                rsp_tail |= (rsp_crc << 32);

                packet[0] = rsp_head;
                packet[((rsp_len * 2) - 1)] = rsp_tail;

                /* build the cmc data payload */
                for (j = 1; j < ((rsp_len - 1) * 2); j++)
                {
                    packet[j] = rsp_payload[j];
                }

                /* -- register the response */
                if (HMC_DEBUG)
                {
                    HMCSIM_PRINT_TRACE("HANDLING OPERATION BANK LATENCY");
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("DEV:QUAD:VAULT:BANK = "+dev+":"+quad + ":" + vault + ":" + bank);
                }
                if (op_latency != 0)
                { /* Delay, stall the response for op_latency cycles */
                    if (HMC_DEBUG)
                    {
                        if (Config.DEBUG_MEMORY) DEBUG.WriteLine("STALLING BANK " + bank + " " + op_latency + " CYCLES");
                    }
                    this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].valid = Macros.HMC_RQST_VALID;
                    this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].delay = op_latency;

                    /* Record the response packet to be sent after the delay */
                    //for (j=0; j<rsp_len; j++) {
                    for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                    {
                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].packet[j] = packet[j];
                    }

                }
                else { /* No delay, forward response immediately */
                    if (HMC_DEBUG)
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("STALLING BANK " + bank + " " + op_latency + " CYCLES");
                    }
                    this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[(int)t_slot].valid = Macros.HMC_RQST_VALID;
                    //for( j=0; j<rsp_len; j++ ){
                    for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                    {
                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[(int)t_slot].packet[j] = packet[j];
                    }
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_vault_rsp_slot(dev, quad, vault, t_slot);
                    }
                }


            }
            else { /* else, no response required, probably flow control */
                   /* Stall the bank for op_latency cycles in the case where no response is generated */
                this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].valid = Macros.HMC_RQST_INVALID;
                this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].banks[(int)bank].delay = op_latency;
            }
            /*
             * Step 5: invalidate the request queue slot
             *
             */
            hmcsim_util_zero_packet(ref queue);

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("COMPLETED PACKET PROCESSING");

            return 0;
        }
        public int hmcsim_clock_reg_responses()
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            UInt32 cur = 0;
            Int32 k = 0;
            Int32 x = 0;
            Int32 y = 0;
            UInt32 r_link = 0;
            UInt32 r_slot = this.xbar_depth + 1;
            List<hmc_queue> lq = null;
            /* ---- */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STARTING HMCSIM_CLOCK_REG_RESPONSES");


            /*
             * Walk all the vault response queues
             * For each queue, attempt to push it into a crossbar
             * slot.  If not, signal a stall
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {
                for (j = 0; j < this.num_quads; j++)
                {
                    for (k = 0; k < 8; k++)
                    {

                        lq = this.devs[i].quads[j].vaults[k].rsp_queue;

                        for (x = 0; x < this.queue_depth; x++)
                        {

                            /*
                             * Determine if I am a live response
                             * If so, check for an appropriate
                             * response queue slot
                             *
                             */
                            if (lq[x].valid != Macros.HMC_RQST_INVALID)
                            {

                                /*
                                 * determine which link response
                                 * queue we're supposed to route
                                 * use the SLID value
                                 *
                                 */
                                hmcsim_util_decode_rsp_slid(
                                                 lq,
                                                 (uint)x,
                                                 ref r_link);

                                /* if link is not local, register latency */
                                if (r_link != j)
                                {
                                    /*
                                     * higher latency
                                     *
                                     */

                                    if ((this.tracelevel & Macros.HMC_TRACE_LATENCY) > 0)
                                    {

                                        hmcsim_trace_latency(
                                               (uint)i,
                                                r_link,
                                               (uint)x,
                                               (uint)j,
                                               (uint)k);
                                    }
                                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                                    {

                                        hmcsim_power_remote_route(
                                                  (uint)i,
                                                    r_link,
                                                  (uint)x,
                                                   (uint)j,
                                                   (uint)k);
                                    }
                                }
                                else {
                                    /* local route */
                                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                                    {

                                        hmcsim_power_local_route(
                                          (uint)i,
                                          r_link,
                                         (uint)x,
                                         (uint)j,
                                         (uint)k);
                                    }
                                }/* end tracing route latency */


                                /*
                                 * determine if the response
                                 * xbar queue has an empty slot
                                 *
                                 */
                                cur = this.xbar_depth - 1;
                                r_slot = this.xbar_depth + 1;
                                for (y = 0; y < this.xbar_depth; y++)
                                {
                                    if (this.devs[i].xbar[(int)r_link].xbar_rsp[(int)cur].valid ==
                                            Macros.HMC_RQST_INVALID)
                                    {
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
                                if (r_slot != (this.xbar_depth + 1))
                                {

                                    /*
                                     * slot found!
                                     * transfer the data
                                     *
                                     */
                                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                                    {
                                        hmcsim_power_xbar_rsp_slot(
                                                                    (uint)i,
                                                                    r_link,
                                                                    r_slot);
                                    }
                                    this.devs[i].xbar[(int)r_link].xbar_rsp[(int)r_slot].valid =
                                            Macros.HMC_RQST_VALID;
                                    for (y = 0; y < Macros.HMC_MAX_UQ_PACKET; y++)
                                    {
                                        this.devs[i].xbar[(int)r_link].xbar_rsp[(int)r_slot].packet[y] =
                                                lq[x].packet[y];
                                        lq[x].packet[y] = 0x00L;
                                    }

                                    /*
                                     * clear the source slot
                                     *
                                     */
                                    lq[x].valid = Macros.HMC_RQST_INVALID;

                                    /*
                                     * update the token log for the simple api
                                     *
                                     */
                                    hmcsim_token_update(
                                      this.devs[i].xbar[(int)r_link].xbar_rsp[(int)r_slot].packet,
                                      (uint)i, r_link, r_slot);

                                }
                                else {

                                    /*
                                     * STALL!
                                     *
                                     */

                                    lq[x].valid = Macros.HMC_RQST_STALLED;

                                    if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                                    {

                                        /*
                                         * print a trace signal 
                                         *
                                         */
                                        hmcsim_trace_stall(
                                             (uint)i,
                                             (uint)j,
                                             (uint)k,
                                                0,
                                                0,
                                                0,
                                              (uint)x,
                                                2);
                                    }
                                }
                            }/* else, request not valid */
                        }/* queue_depth */

                        if (HMC_DEBUG)
                        {
                            hmcsim_clock_print_vault_stats((uint)k, this.devs[i].quads[j].vaults[k].rqst_queue, 0);

                            hmcsim_clock_print_vault_stats((uint)k, this.devs[i].quads[j].vaults[k].rsp_queue, 1);
                        }
                    } /* vaults */
                } /* quads */
            } /* devs */

            return 0;
        }

        public int hmcsim_clock_queue_reorg()
        {
            /* vars */
            UInt32 i = 0;
            UInt32 j = 0;
            UInt32 k = 0;
            /* ---- */

            /*
             * crossbar queues
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {
                for (j = 0; j < this.num_links; j++)
                {
                    /*
                     * reorder:
                     * this.devs[i].xbar[j].xbar_rqst;
                     * this.devs[i].xbar[j].xbar_rsp;
                     *
                     */
                    hmcsim_clock_reorg_xbar_rqst(i, j);

                    hmcsim_clock_reorg_xbar_rsp(i, j);
                }
            }

            /*
             * vault queues
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {
                for (j = 0; j < this.num_quads; j++)
                {
                    for (k = 0; k < 8; k++)
                    {
                        /*
                         * reorder:
                         * this.devs[i].quads[j].vaults[k].rqst_queue;
                         * this.devs[i].quads[j].vaults[k].rsp_queue;
                         *
                         */
                        hmcsim_clock_reorg_vault_rqst(i, j, k);

                        hmcsim_clock_reorg_vault_rsp(i, j, k);
                    }
                }
            }

            return 0;
        }

        public int hmcsim_clock_process_rqst_queue(UInt32 dev, UInt32 link)
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            UInt32 cur = 0;
            UInt32 found = 0;
            UInt32 success = 0;
            UInt32 len = 0;
            UInt32 t_link = 0;
            UInt32 t_slot = 0;
            UInt32 t_quad = 0;
            UInt32 bsize = 0;
            UInt32 t_vault = this.queue_depth + 1;
            uint cub = 0;
            uint plink = 0;
            UInt64 header = 0x00L;
            UInt64 tail = 0x00L;
            UInt64 addr = 0x00L;
            /* ---- */

            /*
             * get the block size
             *
             */
            hmcsim_util_get_max_blocksize(dev, ref bsize);

            /*
             * walk the queue and process all the valid
             * slots
             *
             */

            for (i = 0; i < this.xbar_depth; i++)
            {

                if (this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * process me
                     *
                     */
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_xbar_rqst_slot(dev, link, (uint)i);
                    }

                    if (HMC_DEBUG)
                        HMCSIM_PRINT_INT_TRACE("PROCESSING REQUEST QUEUE FOR SLOT", (int)(i));


                    /*
                     * Step 1: Get the header
                     *
                     */
                    header = this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[0];

                    addr = ((header >> 24) & 0x1FFFFFFFF);

                    /*
                     * Step 2: Get the CUB.
                     *
                     */
                    cub = (uint)((header >> 61) & 0x7);

                    /* Step 3: If it is equal to `dev`
                     *         then we have a local request.
                     * 	   Otherwise, its a request to forward to
                     * 	   an adjacent device.
                     */

                    /*
                     * Stage 4: Get the packet length
                     *
                     */
                    len = (UInt32)((header >> 7) & 0x1F);
                    len *= 2;

                    /*
                     * Stage 5: Get the packet tail
                     *
                     */
                    tail = this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[len - 1];


                    /*
                     * Stage 6: Get the link
                     *
                     */
                    plink = (uint)((tail >> 26) & 0x7);

                    if (cub == (uint)(dev))
                    {
                        if (HMC_DEBUG)
                            HMCSIM_PRINT_INT_TRACE("LOCAL DEVICE REQUEST AT SLOT", (int)(i));

                        /*
                         * local request
                         *
                         */

                        /*
                         * 7a: Retrieve the vault id
                         *
                         */
                        hmcsim_util_decode_vault(
                                    dev,
                                    bsize,
                                    addr,
                                    ref t_vault);

                        /*
                         * 8a: Retrieve the quad id
                         *
                         */
                        hmcsim_util_decode_quad(
                                    dev,
                                    bsize,
                                    addr,
                                    ref t_quad);


                        /*
                         * if quad is not directly attached
                         * to my link, print a trace message
                         * indicating higher latency
                         */
                        if (link != t_quad)
                        {
                            /*
                             * higher latency
                             *
                             */

                            if ((this.tracelevel & Macros.HMC_TRACE_LATENCY) > 0)
                            {

                                hmcsim_trace_latency(
                                            dev,
                                            link,
                                            (uint)i,
                                            t_quad,
                                            t_vault);
                            }
                            if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                            {

                                hmcsim_power_remote_route(
                                                      dev,
                                                      link,
                                                     (uint)i,
                                                      t_quad,
                                                      t_vault);
                            }
                        }
                        else {
                            /* local route */
                            if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                            {

                                hmcsim_power_local_route(
                                                  dev,
                                                  link,
                                                 (uint)i,
                                                  t_quad,
                                                  t_vault);
                            }
                        }/* end tracing route latency */

                        /*
                         * 9a: Search the vault queue for valid slot
                         *     Search bottom-up
                         *
                         */
                        cur = this.queue_depth - 1;
                        t_slot = this.queue_depth + 1;
                        for (j = 0; j < this.queue_depth; j++)
                        {
                            if (this.devs[(int)dev].quads[(int)t_quad].vaults[(int)t_vault].rqst_queue[(int)cur].valid
                                            == Macros.HMC_RQST_INVALID)
                            {
                                t_slot = cur;
                            }
                            cur--;
                        }

                        if (t_slot == this.queue_depth + 1)
                        {


                            if (HMC_DEBUG)
                                HMCSIM_PRINT_INT_TRACE("STALLED REQUEST AT SLOT", (int)(i));

                            /* STALL */
                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid = Macros.HMC_RQST_STALLED;

                            /*
                             * print a stall trace
                             *
                             */
                            if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                            {

                                hmcsim_trace_stall(
                                            dev,
                                            t_quad,
                                            t_vault,
                                            0,
                                            0,
                                            0,
                                           (uint)i,
                                            0);
                            }

                            success = 0;
                        }
                        else {

                            if (HMC_DEBUG)
                            {
                                HMCSIM_PRINT_INT_TRACE("TRANSFERRING PACKET FROM SLOT", (int)(i));

                                HMCSIM_PRINT_INT_TRACE("TRANSFERRING PACKET TO SLOT", (int)(t_slot));
                            }
                            /*
                             * push it into the designated queue slot
                             *
                             */
                            this.devs[(int)dev].quads[(int)t_quad].vaults[(int)t_vault].rqst_queue[(int)t_slot].valid = Macros.HMC_RQST_VALID;
                            for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                            {
                                this.devs[(int)dev].quads[(int)t_quad].vaults[(int)t_vault].rqst_queue[(int)t_slot].packet[j] =
                                    this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[j];
                            }

                            success = 1;

                        }

                    }
                    else {

                        if (HMC_DEBUG)
                            HMCSIM_PRINT_INT_TRACE("REMOTE DEVICE REQUEST AT SLOT", (int)(i));

                        /*
                         * forward request to remote device
                         *
                         */

                        /*
                         * Stage 7b: Decide whether cub is accessible
                         *
                         */
                        found = 0;

                        while ((found != 1) && (j < this.num_links))
                        {

                            if (this.devs[(int)dev].links[j].dest_cub == cub)
                            {
                                found = 1;
                                t_link = (uint)j;
                            }

                            j++;
                        }

                        if (found == 0)
                        {
                            /*
                             * oh snap! can't route to that CUB
                             * Mark it as a zombie request
                             * Future: return an error packet
                             *
                             */
                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid = Macros.HMC_RQST_ZOMBIE;
                        }
                        else {
                            /*
                             * 8b: routing is good, look for an empty slot
                             * in the target xbar link queue
                             *
                             */
                            t_slot = this.xbar_depth + 1;
                            cur = this.xbar_depth - 1;
                            for (j = 0; j < this.xbar_depth; j++)
                            {
                                /*
                                 * walk the queue from the bottom
                                 * up
                                 */
                                if (this.devs[(int)cub].xbar[(int)t_link].xbar_rqst[(int)cur].valid ==
                                    Macros.HMC_RQST_INVALID)
                                {
                                    t_slot = cur;
                                }
                                cur--;
                            }

                            /*
                             * 9b: If available, insert into remote xbar slot 
                             *
                             */
                            if (t_slot == this.xbar_depth + 1)
                            {
                                /*
                                 * STALL!
                                 *
                                 */
                                this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid = Macros.HMC_RQST_STALLED;
                                /*
                                 * print a stall trace
                                 *
                                 */
                                if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                                {

                                    hmcsim_trace_stall(
                                                dev,
                                                0,
                                                0,
                                                dev,
                                                cub,
                                                link,
                                               (uint)i,
                                                3);
                                }

                                success = 0;
                            }
                            else {
                                /*
                                 * put the new link in the link field
                                 *
                                 */
                                this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[len - 1] |=
                                                ((UInt64)(plink) << 24);

                                /*
                                 * transfer the packet to the target slot
                                 *
                                 */
                                this.devs[(int)cub].xbar[(int)t_link].xbar_rqst[(int)t_slot].valid =
                                   Macros.HMC_RQST_VALID;
                                for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                                {
                                    this.devs[(int)cub].xbar[(int)t_link].xbar_rqst[(int)t_slot].packet[j] =
                                    this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[j];
                                }

                                if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                                {
                                    hmcsim_power_route_extern(
                                                               cub,
                                                               t_link,
                                                               t_slot,
                                                               dev,
                                                               link,
                                                              (uint)i);
                                }

                                /*
                                 * signal success
                                 *
                                 */
                                success = 1;
                            }
                        }
                    }

                    if (success == 1)
                    {

                        /*
                         * clear the packet
                         *
                         */
                        if (HMC_DEBUG)
                            HMCSIM_PRINT_TRACE("ZEROING PACKET");
                        var item = this.devs[(int)dev].xbar[(int)link].xbar_rqst[i];
                        hmcsim_util_zero_packet(ref item);
                        this.devs[(int)dev].xbar[(int)link].xbar_rqst[i] = item;
                    }

                }

                success = 0;
            }

            if (HMC_DEBUG)
            {
                hmcsim_clock_print_xbar_stats(this.devs[(int)dev].xbar[(int)link].xbar_rqst);

                HMCSIM_PRINT_TRACE("FINISHED PROCESSING REQUEST QUEUE");
            }

            return 0;
        }

        public int hmcsim_power_vault_ctrl(UInt32 vault)
        {

            this.power.t_vault_ctrl += this.power.vault_ctrl;

            hmcsim_trace_power_vault_ctrl(vault);

            if (this.num_links == 4)
            {
                this.power.H4L.vault_ctrl_power[vault] += this.power.vault_ctrl;
                this.power.H4L.vault_ctrl_btu[vault] += (this.power.vault_ctrl * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.vault_ctrl_power[vault] += this.power.vault_ctrl;
                this.power.H8L.vault_ctrl_btu[vault] += (this.power.vault_ctrl * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_clock_process_rsp_queue(UInt32 dev, UInt32 link)
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            UInt32 cur = 0;
            UInt32 dest = 0;
            UInt32 t_slot = this.xbar_depth + 1;
            List<hmc_queue> queue = null;
            /* ---- */

            if (hmcsim_util_is_root(dev) == 1)
            {

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
            for (i = 0; i < this.xbar_depth; i++)
            {

                if (this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * process me
                     *
                     */
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_xbar_rsp_slot(dev, link, (uint)i);
                    }


                    /*
                     * Stage 1: get the corresponding cub device
                     * 	    and its response queue
                     *
                     */
                    dest = this.devs[(int)dev].links[(int)link].dest_cub;
                    queue = this.devs[(int)dev].xbar[(int)dest].xbar_rsp;


                    /*
                     * Stage 2: determine if the destination cub
                     * 	    device has empty response queue
                     * 	    slots
                     *
                     */
                    cur = this.xbar_depth - 1;
                    t_slot = this.xbar_depth + 1;
                    for (j = 0; j < this.xbar_depth; j++)
                    {
                        if (queue[(int)cur].valid == Macros.HMC_RQST_INVALID)
                        {

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
                    if (t_slot != (this.xbar_depth + 1))
                    {

                        /*
                         * found a good slot
                         *
                         */

                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {
                            queue[(int)t_slot].packet[j] =
                                this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].packet[j];
                        }

                        queue[(int)t_slot].valid = Macros.HMC_RQST_VALID;

                        /*
                         * zero the packet
                         *
                         */
                        var item = this.devs[(int)dev].xbar[(int)link].xbar_rsp[i];
                        hmcsim_util_zero_packet(ref item);
                        this.devs[(int)dev].xbar[(int)link].xbar_rsp[i] = item;


                    }
                    else {

                        /*
                         * STALL!
                         *
                         */
                        this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].valid = Macros.HMC_RQST_STALLED;

                        /*
                         * Print a stall trace
                         *
                         */
                        if ((this.tracelevel & Macros.HMC_TRACE_STALL) > 0)
                        {

                            hmcsim_trace_stall(
                                        dev,
                                        0,
                                        0,
                                        dev,
                                        dest,
                                        link,
                                        (uint)i,
                                        4);
                        }
                    } /* end if+else */

                } /* end if */
            } /* i<this.xbar_depth */

            return 0;
        }
        public int hmcsim_util_decode_qv(UInt32 dev, UInt32 bsize, UInt64 addr, UInt32 quad, ref UInt32 vault)
        {



            /* 
             * decode the quad and vault
             * 
             */
            hmcsim_util_decode_quad(dev, bsize, addr, ref quad);

            hmcsim_util_decode_vault(dev, bsize, addr, ref vault);

            return 0;
        }


}
}
