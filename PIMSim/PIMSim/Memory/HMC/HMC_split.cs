using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PIMSim.Statistics;
using PIMSim.Configs;

namespace PIMSim.Memory.HMC
{
    public partial class HMCSim
    {
        public uint hmcsim_rqst_getrrp()
        {
            return 0x03;
        }

        public uint hmcsim_rqst_getfrp()
        {
            return 0x02;
        }
        public uint hmcsim_rqst_getrtc()
        {
            return 0x01;
        }
        public uint hmcsim_rqst_getseq(hmc_rqst type)
        {
            if ((type == hmc_rqst.PRET) || (type == hmc_rqst.IRTRY))
            {
                return this.seq;
            }

            this.seq++;

            if (this.seq > 0x07)
            {
                this.seq = 0x00;
            }

            return 0x01;
        }
        public UInt32 hmcsim_crc32(UInt64 addr, UInt64[] payload, UInt32 len)
        {
            /* vars */
            UInt32 crc = 0x11111111;
            /* ---- */

            /* FIXME : REPORT THE TRUE CRC */
            return crc;
        }


        public void HMCSIM_PRINT_ADDR_TRACE(string s, UInt64 a)
        {
            if (Config.DEBUG_MEMORY) DEBUG.WriteLine("HCMSIM_TRACE : " + a + " : 0x" + a.ToString("X"));
        }
        public void HMCSIM_PRINT_INT_TRACE(string s, int d)
        {
            if (Config.DEBUG_MEMORY) DEBUG.WriteLine("HCMSIM_TRACE : " + s + " : " + d);
        }
        public void HMCSIM_PRINT_TRACE(string s)
        {
            if (Config.DEBUG_MEMORY) DEBUG.WriteLine("HCMSIM_TRACE : " + s);
        }
        public int hmcsim_util_zero_packet(ref hmc_queue queue)
        {
            /* vars */
            UInt64 i = 0;
            /* ---- */

            /*
             * sanity check
             *
             */
            if (queue == null)
            {
                return -1;
            }

            for (i = 0; i < (ulong)Macros.HMC_MAX_UQ_PACKET; i++)
            {
                queue.packet[i] = 0x00L;
            }

            queue.valid = Macros.HMC_RQST_INVALID;

            return 0;
        }
        public void hmcsim_posted_rsp(UInt64 hdr)
        {
            UInt32 tag = 0;
            UInt32 cmd = 0;

            tag = (UInt32)((hdr >> 12) & 0x7FF);
            cmd = (UInt32)(hdr & 0x7F);

            switch (cmd)
            {
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 95:
                case 34:
                case 35:
                case 84:
                case 33:
                    /* posted-type packet; no response */
                    this.tokens[tag].status = 2;
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
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
                    /* cmc packet, check its response */
                    if (this.cmcs[(int)hmcsim_cmc_rawtoidx(cmd)].rsp_cmd == hmc_response.RSP_NONE)
                    {
                        /* cmc with posted response */
                        this.tokens[tag].status = 2;
                    }
                    break;
                default:
                    /* normal response, just return */
                    return;
                   
            }
        }
        public UInt32 hmcsim_cmc_rawtoidx(UInt32 raw)
        {
            UInt32 i = 0;

            for (i = 0; i < Macros.HMC_MAX_CMC; i++)
            {
                if (Ctable.ctable[i].cmd == raw)
                {
                    return i;
                }
            }
            return (uint)Macros.HMC_MAX_CMC; /* redundant, but squashes gcc warning */
        }

        public int hmcsim_clock_process_rqst_queue_new(UInt32 dev, UInt32 link, UInt32 i)
        {
            /* vars */
            UInt32 j = 0;
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

            if (this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].valid != Macros.HMC_RQST_INVALID)
            {

                /*
                 * process me
                 *
                 */
                if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                {
                    hmcsim_power_xbar_rqst_slot(dev, link, i);
                }

                if (HMC_DEBUG)
                    HMCSIM_PRINT_INT_TRACE("PROCESSING REQUEST QUEUE FOR SLOT", (int)(i));


                /*
                 * Step 1: Get the header
                 *
                 */
                header = this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].packet[0];

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
                tail = this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].packet[len - 1];


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
                                        i,
                                        t_quad,
                                        t_vault);
                        }
                        if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                        {

                            hmcsim_power_remote_route(
                                                  dev,
                                                  link,
                                                  i,
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
                                              i,
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
                        this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].valid = (uint)Macros.HMC_RQST_STALLED;

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
                                        i,
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
                        this.devs[(int)dev].quads[(int)t_quad].vaults[(int)t_vault].rqst_queue[(int)t_slot].valid = (uint)Macros.HMC_RQST_VALID;
                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {
                            this.devs[(int)dev].quads[(int)t_quad].vaults[(int)t_vault].rqst_queue[(int)t_slot].packet[j] =
                                this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].packet[j];
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

                        if (this.devs[(int)dev].links[(int)j].dest_cub == cub)
                        {
                            found = 1;
                            t_link = j;
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
                        this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].valid = (uint)Macros.HMC_RQST_ZOMBIE;
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
                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].valid = (uint)Macros.HMC_RQST_STALLED;
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
                                            i,
                                            3);
                            }

                            success = 0;
                        }
                        else {
                            /*
                             * put the new link in the link field
                             *
                             */
                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].packet[len - 1] |=
                                           ((UInt64)(plink) << 24);

                            /*
                             * transfer the packet to the target slot
                             *
                             */
                            this.devs[(int)cub].xbar[(int)t_link].xbar_rqst[(int)t_slot].valid =
                                (uint)Macros.HMC_RQST_VALID;
                            for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                            {
                                this.devs[(int)cub].xbar[(int)t_link].xbar_rqst[(int)t_slot].packet[j] =
                                this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i].packet[j];
                            }

                            if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                            {
                                hmcsim_power_route_extern(
                                                           cub,
                                                           t_link,
                                                           t_slot,
                                                           dev,
                                                           link,
                                                           i);
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

                    var item = this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i];

                    hmcsim_util_zero_packet(ref item);
                    this.devs[(int)dev].xbar[(int)link].xbar_rqst[(int)i] = item;

                }

            }

            success = 0;
            //}

            if (HMC_DEBUG)
            {
                hmcsim_clock_print_xbar_stats(this.devs[(int)dev].xbar[(int)link].xbar_rqst);

                HMCSIM_PRINT_TRACE("FINISHED PROCESSING REQUEST QUEUE");
            }

            return 0;
        }

        public int hmcsim_clock_print_xbar_stats(List<hmc_queue> queue)
        {
            /* vars */
            int nvalid = 0;
            int ninvalid = 0;
            int nstalled = 0;
            int nconflict = 0;
            int i = 0;
            /* ---- */

            for (i = 0; i < this.xbar_depth; i++)
            {
                if (queue[i].valid == Macros.HMC_RQST_VALID)
                {
                    nvalid++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_INVALID)
                {
                    ninvalid++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_CONFLICT)
                {
                    nconflict++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_STALLED)
                {
                    nstalled++;
                }
            }


            if (Config.DEBUG_MEMORY) DEBUG.WriteLine("XBAR:nvalid:ninvalid:nconflict:nstalled = " + nvalid + ":" + ninvalid + ":" + nconflict + ":" + nstalled);

            return 0;
        }
        public int hmcsim_util_get_max_blocksize(UInt32 dev, ref UInt32 bsize)
        {
            /* vars */
            UInt64 reg = 0x00L;
            UInt64 code = 0x00L;
            /* ---- */



            if (dev > (this.num_devs - 1))
            {

                /* 
                 * device out of range 
                 * 
                 */

                return -1;
            }



            /*
             * retrieve the register value 
             * 
             */
            reg = this.devs[(int)dev].regs[Macros.HMC_REG_AC_IDX].reg;


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
            switch (code)
            {
                case 0x0:
                    /* 32 bytes */
                    bsize = 32;
                    break;
                case 0x1:
                    /* 64 bytes */
                    bsize = 64;
                    break;
                case 0x2:
                    /* 128 bytes */
                    bsize = 128;
                    break;
                case 0x8:
                    /* 32 bytes */
                    bsize = 32;
                    break;
                case 0x9:
                    /* 64 bytes */
                    bsize = 64;
                    break;
                case 0xA:
                    /* 32 bytes */
                    bsize = 32;
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
        public int hmcsim_power_xbar_rqst_slot(UInt32 dev, UInt32 link, UInt32 slot)
        {


            this.power.t_xbar_rqst_slot += this.power.xbar_rqst_slot;

            hmcsim_trace_power_xbar_rqst_slot(dev, link, slot);

            if (this.num_links == 4)
            {
                this.power.H4L.xbar_rqst_power[link] += this.power.xbar_rqst_slot;
                this.power.H4L.xbar_rqst_btu[link] += (this.power.xbar_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.xbar_rqst_power[link] += this.power.xbar_rqst_slot;
                this.power.H8L.xbar_rqst_btu[link] += (this.power.xbar_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_trace_power_xbar_rqst_slot(UInt32 dev, UInt32 link, UInt32 slot)
        {


            this.tfile.WriteLine("%s%%s%%s%%s%%s%f",
                      "HMCSIM_TRACE : ",
                      this.clk,
                      " : XBAR_RQST_SLOT_POWER : ",
                      dev,
                      ":",
                      link,
                      ":",
                      slot,
                      ":",
                      this.power.xbar_rqst_slot);

            this.tfile.WriteLine("%s%%s%%s%%s%%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : XBAR_RQST_SLOT_BTU : ",
           dev,
           ":",
           link,
           ":",
           slot,
           ":",
           this.power.xbar_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }

        public int hmcsim_util_decode_vault(UInt32 dev, UInt32 bsize, UInt64 addr, ref UInt32 vault)
        {
            /* vars */
            UInt32 num_links = 0x00;
            UInt32 capacity = 0x00;
            UInt32 tmp = 0x00;
            /* ---- */

            num_links = this.num_links;
            capacity = this.capacity;

            /*
             * link layout
             *
             */
            if (num_links == 4)
            {
                /*
                 * 4-link device
                 *
                 */
                if (capacity == 2)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [6:5] */
                            tmp = (UInt32)((addr >> 5) & 0x7);
                            break;
                        case 64:
                            /* [7:6] */
                            tmp = (UInt32)((addr >> 6) & 0x7);
                            break;
                        case 128:
                            /* [8:7] */
                            tmp = (UInt32)((addr >> 7) & 0x7);
                            break;
                        default:
                            break;
                    }

                }
                else if (capacity == 4)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 5) & 0x7);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 6) & 0x7);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 7) & 0x7); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 8) & 0x7); // hkim
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (num_links == 8)
            {
                /*
                 * 8-link device
                 *
                 */
                if (capacity == 4)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 5) & 0x7);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 6) & 0x7);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 7) & 0x7); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 8) & 0x7); // hkim
                            break;
                        default:
                            break;
                    }
                }
                else if (capacity == 8)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 5) & 0x7);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 6) & 0x7);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 7) & 0x7); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 8) & 0x7); // hkim
                            break;
                        default:
                            break;
                    }
                }
            }
            else {
                return -1;
            }

            /* 
             * write out the value 
             * 
             */
            vault = tmp;

            return 0;
        }

        public int hmcsim_util_decode_quad(UInt32 dev, UInt32 bsize, UInt64 addr, ref UInt32 quad)
        {

            /* vars */
            UInt32 num_links = 0x00;
            UInt32 capacity = 0x00;
            UInt32 tmp = 0x00;
            /* ---- */



            num_links = this.num_links;
            capacity = this.capacity;

            /*
             * link layout
             *
             */
            if (num_links == 4)
            {
                /*
                 * 4-link device
                 *
                 */
                if (capacity == 2)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [6:5] */
                            tmp = (UInt32)((addr >> 8) & 0x3);
                            break;
                        case 64:
                            /* [7:6] */
                            tmp = (UInt32)((addr >> 9) & 0x3);
                            break;
                        case 128:
                            /* [8:7] */
                            tmp = (UInt32)((addr >> 10) & 0x3);
                            break;
                        default:
                            break;
                    }

                }
                else if (capacity == 4)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 8) & 0x3);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 8) & 0x3);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 10) & 0x3); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 11) & 0x3); // hkim
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (num_links == 8)
            {
                /*
                 * 8-link device
                 *
                 */
                if (capacity == 4)
                {
                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 8) & 0x3);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 9) & 0x3);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 10) & 0x3); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 11) & 0x3); // hkim
                            break;
                        default:
                            break;
                    }
                }
                else if (capacity == 8)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [9:5] */
                            tmp = (UInt32)((addr >> 8) & 0x3);
                            break;
                        case 64:
                            /* [10:6] */
                            tmp = (UInt32)((addr >> 9) & 0x3);
                            break;
                        case 128:
                            /* [11:7] */
                            tmp = (UInt32)((addr >> 10) & 0x3); // hkim
                            break;
                        case 256:
                            /* [12:8] */
                            tmp = (UInt32)((addr >> 11 & 0x3)); // hkim
                            break;
                        default:
                            break;
                    }
                }
            }
            else {
                return -1;
            }

            /* 
             * write out the value 
             * 
             */
            quad = tmp;

            return 0;
        }

        public int hmcsim_trace_latency(UInt32 dev, UInt32 link, UInt32 slot, UInt32 quad, UInt32 vault)
        {
            if (this.tfile == null)
            {
                return -1;
            }


            this.tfile.WriteLine("HMCSIM_TRACE : %llu : XBAR_LATENCY : %u:%u:%u:%u:%u",
                        this.clk,
                        dev,
                        link,
                        slot,
                        quad,
                        vault);

            return 0;
        }
        public int hmcsim_power_remote_route(UInt32 dev, UInt32 link, UInt32 slot, UInt32 quad, UInt32 vault)
        {


            this.power.t_link_remote_route += this.power.link_remote_route;

            hmcsim_trace_power_remote_route(dev, link, slot, quad, vault);

            if (this.num_links == 4)
            {
                this.power.H4L.link_remote_route_power[link] += this.power.link_remote_route;
                this.power.H4L.link_remote_route_btu[link] += (this.power.link_remote_route * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.link_remote_route_power[link] += this.power.link_remote_route;
                this.power.H8L.link_remote_route_btu[link] += (this.power.link_remote_route * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }
        public int hmcsim_trace_power_remote_route(UInt32 dev, UInt32 link, UInt32 slot, UInt32 quad, UInt32 vault)
        {


            this.tfile.WriteLine(
                      "%s%llu%s%u%s%u%s%u%s%u%s%u%s%f",
                      "HMCSIM_TRACE : ",
                      this.clk,
                      " : LINK_REMOTE_ROUTE_POWER : ",
                      dev,
                      ":",
                      link,
                      ":",
                      quad,
                      ":",
                      vault,
                      ":",
                      slot,
                      ":",
                      this.power.link_remote_route);

            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%u%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : LINK_REMOTE_ROUTE_BTU : ",
           dev,
           ":",
           link,
           ":",
           quad,
           ":",
           vault,
           ":",
           slot,
           ":",
           this.power.link_remote_route * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }


        public int hmcsim_power_local_route(UInt32 dev, UInt32 link, UInt32 slot, UInt32 quad, UInt32 vault)
        {


            this.power.t_link_local_route += this.power.link_local_route;

            hmcsim_trace_power_local_route(dev, link, slot, quad, vault);

            if (this.num_links == 4)
            {
                this.power.H4L.link_local_route_power[link] += this.power.link_local_route;
                this.power.H4L.link_local_route_btu[link] += (this.power.link_local_route * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.link_local_route_power[link] += this.power.link_local_route;
                this.power.H8L.link_local_route_btu[link] += (this.power.link_local_route * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_trace_power_local_route(UInt32 dev, UInt32 link, UInt32 slot, UInt32 quad, UInt32 vault)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : LINK_LOCAL_ROUTE_POWER : ",
                       dev,
                       ":",
                       link,
                       ":",
                       quad,
                       ":",
                       vault,
                       ":",
                       slot,
                       ":",
                       this.power.link_local_route);

            this.tfile.WriteLine(
                      "%s%llu%s%u%s%u%s%u%s%u%s%u%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : LINK_LOCAL_ROUTE_BTU : ",
           dev,
           ":",
           link,
           ":",
           quad,
           ":",
           vault,
           ":",
           slot,
           ":",
           this.power.link_local_route * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }
        public int hmcsim_trace_stall(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 src, UInt32 dest, UInt32 link, UInt32 slot, UInt32 type)
        {


            /*
             * Determine which stall type
             *
             */
            if (type == 0)
            {

                /*
                 * xbar stall
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : XBAR_RQST_STALL : %u:%u:%u:%u",
                              this.clk,
                              dev,
                              quad,
                              vault,
                              slot);

            }
            else if (type == 1)
            {

                /*
                 * vault request stall
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : VAULT_RQST_STALL : %u:%u:%u:%u",
                    this.clk,
                    dev,
                    quad,
                    vault,
                    slot);


            }
            else if (type == 2)
            {

                /*
                 * xbar response stall
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : XBAR_RSP_STALL : %u:%u:%u:%u",
                    this.clk,
                    dev,
                    quad,
                    vault,
                    slot);

            }
            else if (type == 3)
            {

                /*
                 * device request route stall
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : ROUTE_RQST_STALL : %u:%u:%u:%u",
                    this.clk,
                    dev,
                    src,
                    dest,
                    link,
                    slot);

            }
            else if (type == 4)
            {

                /*
                 * device response route stall
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : ROUTE_RSP_STALL : %u:%u:%u:%u",
                    this.clk,
                    dev,
                    src,
                    dest,
                    link,
                    slot);

            }
            else {

                /*
                 * undefined stall event
                 *
                 */

                this.tfile.WriteLine("HMCSIM_TRACE : %llu : UNDEF_STALL : %u:%u:%u:%u",
                    this.clk,
                    dev,
                    quad,
                    vault,
                    slot);

            }

            return 0;
        }
        public int hmcsim_power_route_extern(UInt32 srcdev, UInt32 srclink, UInt32 srcslot, UInt32 destdev, UInt32 destlink, UInt32 destslot)
        {


            this.power.t_xbar_route_extern += this.power.xbar_route_extern;

            hmcsim_trace_power_route_extern(srcdev, srclink, srcslot,
                                             destdev, destlink, destslot);

            if (this.num_links == 4)
            {
                this.power.H4L.xbar_route_extern_power[srclink] += this.power.xbar_route_extern;
                this.power.H4L.xbar_route_extern_btu[srclink] += (this.power.xbar_route_extern * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.xbar_route_extern_power[srclink] += this.power.xbar_route_extern;
                this.power.H8L.xbar_route_extern_btu[srclink] += (this.power.xbar_route_extern * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_trace_power_route_extern(UInt32 srcdev, UInt32 srclink, UInt32 srcslot, UInt32 destdev, UInt32 destlink, UInt32 destslot)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : XBAR_ROUTE_EXTERN_POWER : ",
                       srcdev,
                       ":",
                       srclink,
                       ":",
                       srcslot,
                       ":",
                       destdev,
                       ":",
                       destlink,
                       ":",
                       destslot,
                       ":",
                       this.power.xbar_rsp_slot);

            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : XBAR_ROUTE_EXTERN_BTU : ",
                       srcdev,
                       ":",
                       srclink,
                       ":",
                       srcslot,
                       ":",
                       destdev,
                       ":",
                       destlink,
                       ":",
                       destslot,
                       ":",
                       this.power.xbar_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }


        public int hmcsim_trace_rqst(string rqst, UInt32 dev, UInt32 quad, UInt32 vault, UInt32 bank, UInt64 addr1, UInt32 size)
        {
            if (this.tfile == null)
            {
                return -1;
            }


            this.tfile.WriteLine("HMCSIM_TRACE : %llu : %s : %u:%u:%u:%u:0x%016u:%u",
                           this.clk,
                           rqst,
                           dev,
                           quad,
                           vault,
                           bank,
                           addr1,
                           size);

            return 0;

        }


        public int hmcsim_power_row_access(UInt64 addr, UInt32 mult)
        {


            this.power.t_row_access += (this.power.row_access * (float)(mult));

            hmcsim_trace_power_row_access(addr, mult);

            if (this.num_links == 4)
            {
                this.power.H4L.row_access_power += (this.power.row_access * (float)(mult));
                this.power.H4L.row_access_btu += ((this.power.row_access * (float)(mult)) * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.row_access_power += (this.power.row_access * (float)(mult));
                this.power.H8L.row_access_btu += ((this.power.row_access * (float)(mult)) * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_trace_power_row_access(UInt64 addr, UInt32 mult)
        {


            this.tfile.WriteLine(
                       "%s%llu%s0x016%llx%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : ROW_ACCESS_POWER : ",
                       addr,
                       ":",
                       this.power.row_access * (float)(mult));

            this.tfile.WriteLine(
                       "%s%llu%s0x016%llx%s%f",
                       "HMCSIM_TRACE : ",
           this.clk,
           " : ROW_ACCESS_BTU : ",
           addr,
           ":",
           this.power.row_access * (float)(mult) * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }

        public int hmcsim_util_decode_bank(UInt32 dev, UInt32 bsize, UInt64 addr, ref UInt32 bank)
        {
            /* vars */
            UInt32 num_links = 0x00;
            UInt32 capacity = 0x00;
            UInt32 tmp = 0x00;
            /* ---- */

            /*
             * sanity check 
             * 
             */


            num_links = this.num_links;
            capacity = this.capacity;

            /* 
             * link layout 
             * 
             */
            if (num_links == 4)
            {
                /* 
                 * 4-link device 
                 *
                 */
                if (capacity == 2)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [11:9] */
                            tmp = (UInt32)((addr >> 9) & 0x7);
                            break;
                        case 64:
                            /* [12:10] */
                            tmp = (UInt32)((addr >> 10) & 0x7);
                            break;
                        case 128:
                            /* [13:11] */
                            tmp = (UInt32)((addr >> 11) & 0x7);
                            break;
                        default:
                            break;
                    }

                }
                else if (capacity == 4)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [12:9] */
                            //tmp = (UInt32)((addr>>9) & 0xF);
                            tmp = (UInt32)((addr >> 10) & 0x7);
                            break;
                        case 64:
                            /* [13:10] */
                            //tmp = (UInt32)((addr>>10) & 0xF);
                            tmp = (UInt32)((addr >> 11) & 0x7);
                            break;
                        case 128:
                            /* [14:11] */
                            //tmp = (UInt32)((addr>>11) & 0xF);
                            tmp = (UInt32)((addr >> 12) & 0x7);
                            break;
                        case 256:
                            /* [ 15:13] */
                            tmp = (UInt32)((addr >> 13) & 0x7);
                            break;
                        default:
                            break;
                    }

                }
            }
            else if (num_links == 8)
            {
                /* 
                 * 8-link device 
                 *
                 */
                if (capacity == 4)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [12:9] */
                            //tmp = (UInt32)((addr>>9) & 0xF);
                            tmp = (UInt32)((addr >> 10) & 0x7);
                            break;
                        case 64:
                            /* [13:10] */
                            //tmp = (UInt32)((addr>>10) & 0xF);
                            tmp = (UInt32)((addr >> 11) & 0x7);
                            break;
                        case 128:
                            /* [14:11] */
                            //tmp = (UInt32)((addr>>11) & 0xF);
                            tmp = (UInt32)((addr >> 12) & 0x7);
                            break;
                        case 256:
                            /* [ 15:13] */
                            tmp = (UInt32)((addr >> 13) & 0x7);
                            break;
                        default:
                            break;
                    }
                }
                else if (capacity == 8)
                {

                    switch (bsize)
                    {
                        case 32:
                            /* [13:10] */
                            tmp = (UInt32)((addr >> 10) & 0xF);
                            break;
                        case 64:
                            /* [14:11] */
                            tmp = (UInt32)((addr >> 11) & 0xF);
                            break;
                        case 128:
                            /* [15:12] */
                            tmp = (UInt32)((addr >> 12) & 0xF);
                            break;
                        case 256:
                            /* [15:12] */
                            tmp = (UInt32)((addr >> 13) & 0xF);
                            break;
                        default:
                            break;
                    }

                }
            }
            else {
                return -1;
            }

            /*
             * write out the value
             *
             */
            bank = tmp;

            return 0;
        }

        public int hmcsim_power_vault_rqst_slot(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 slot)
        {


            this.power.t_vault_rqst_slot += this.power.vault_rqst_slot;

            hmcsim_trace_power_vault_rqst_slot(dev, quad, vault, slot);

            if (this.num_links == 4)
            {
                this.power.H4L.vault_rqst_power[quad * vault] += this.power.vault_rqst_slot;
                this.power.H4L.vault_rqst_btu[quad * vault] += (this.power.vault_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.vault_rqst_power[quad * vault] += this.power.vault_rqst_slot;
                this.power.H8L.vault_rqst_btu[quad * vault] += (this.power.vault_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }
        public int hmcsim_trace_power_vault_rqst_slot(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 slot)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : VAULT_RQST_SLOT_POWER : ",
           dev,
           ":",
           quad,
           ":",
           vault,
           ":",
           slot,
           ":",
           this.power.vault_rqst_slot);

            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : VAULT_RQST_SLOT_BTU : ",
                       dev,
                       ":",
                       quad,
                       ":",
                       vault,
                       ":",
                       slot,
                       ":",
                       this.power.vault_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }
        public int hmcsim_process_cmc(UInt32 rawcmd, UInt32 dev, UInt32 quad, UInt32 vault, UInt32 bank, UInt64 addr, UInt32 length, UInt64 head, UInt64 tail,
                                UInt64[] rqst_payload, UInt64[] rsp_payload, ref UInt32 rsp_len, ref hmc_response rsp_cmd, ref uint raw_rsp_cmd, ref UInt32 row_ops,
                               ref float tpower)
        {

            /* vars */
            Int32 idx = 0;
            int rtn = 0;
            string op_name = "";


            /* ---- */

            /* resolve the index of the cmc in the lookup table */
            idx = (int)hmcsim_cmc_rawtoidx(rawcmd);

            if (idx == Macros.HMC_MAX_CMC)
            {
                /* erroneous request */
                return -1;
            }
            else if (this.cmcs[idx].active == 0)
            {
                /* command not active */
                return -1;
            }

            /* -- new power measurement items */


            /* command is active, process it */
            if (HMC_DEBUG)
            {
                HMCSIM_PRINT_TRACE("PROCESSING CMC PACKET");
                if (Config.DEBUG_MEMORY) DEBUG.WriteLine("CMC RAWCMD:IDX = " + rawcmd + ":" + idx);
            }
            var cmc_execute = cmcs[(int)idx].cmc_execute;
            rtn = cmc_execute(
                                  dev,
                                  quad,
                                  vault,
                                  bank,
                                  addr,
                                  length,
                                  head,
                                  tail,
                                  rqst_payload,
                                  rsp_payload);

            if (rtn == -1)
            {
                return Macros.HMC_ERROR;
            }
            if (HMC_DEBUG)
            {
                HMCSIM_PRINT_TRACE("DONE PROCESSING CMC PACKET");
                HMCSIM_PRINT_TRACE("REGISTERING RESPONSES IF NECESSARY");
            }

            /* register all the response data */
            rsp_len = this.cmcs[idx].rsp_len;
            rsp_cmd = this.cmcs[idx].rsp_cmd;

            if (rsp_len > 0)
            {
                if (rsp_cmd == hmc_response.RSP_CMC)
                {
                    raw_rsp_cmd = this.cmcs[idx].rsp_cmd_code;
                }
                else {
                    /* encode the normal reponse */
                    switch (rsp_cmd)
                    {
                        case hmc_response.RD_RS:
                            raw_rsp_cmd = 0x38;
                            break;
                        case hmc_response.WR_RS:
                            raw_rsp_cmd = 0x39;
                            break;
                        case hmc_response.MD_RD_RS:
                            raw_rsp_cmd = 0x3A;
                            break;
                        case hmc_response.MD_WR_RS:
                            raw_rsp_cmd = 0x3B;
                            break;
                        case hmc_response.RSP_ERROR:
                        default:
                            raw_rsp_cmd = 0x00;
                            break;
                    }
                }
            }
            else {
                raw_rsp_cmd = 0x00;
            }

            /* trace it */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("DUMPING TRACE DATA FOR CMC COMMAND");

            /* -- get the name of the op */
            var cmc_str = this.cmcs[idx].cmc_str;
            cmc_str(ref op_name);

            /* -- insert the trace */
            hmcsim_trace_rqst(
                               op_name,
                               dev,
                               quad,
                               vault,
                               bank,
                               addr,
                               length);

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("DUMPING POWER/THERMAL DATA FOR CMC COMMAND");

            /* -- get the power */
            if (this.cmcs[idx].track_power == 1)
            {
                var cmc_power = this.cmcs[idx].cmc_power;
                cmc_power(ref row_ops, ref tpower);
            }
            else {
                row_ops = 1;
                tpower = 0.0f;
            }

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("CMC PROCESSING COMPLETE");

            return 0;
        }

        public int hmcsim_power_vault_ctrl_transient(UInt32 vault, float p)
        {


            this.power.t_vault_ctrl += p;

            hmcsim_trace_power_vault_ctrl(vault);

            if (this.num_links == 4)
            {
                this.power.H4L.vault_ctrl_power[vault] += p;
                this.power.H4L.vault_ctrl_btu[vault] += (p * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.vault_ctrl_power[vault] += p;
                this.power.H8L.vault_ctrl_btu[vault] += (p * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }
        public int hmcsim_trace_power_vault_ctrl(UInt32 vault)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : VAULT_CTRL_POWER : ",
                       vault,
                       ":",
                       this.power.vault_ctrl);

            this.tfile.WriteLine(
                       "%s%llu%s%u%s%f",
                       "HMCSIM_TRACE : ",
           this.clk,
           " : VAULT_CTRL_BTU : ",
           vault,
           ":",
           this.power.vault_ctrl * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }
        public int hmcsim_decode_rsp_cmd(hmc_response rsp_cmd, ref uint cmd)
        {
            switch (rsp_cmd)
            {
                case hmc_response.RD_RS:
                    cmd = 0x38;
                    break;
                case hmc_response.WR_RS:
                    cmd = 0x39;
                    break;
                case hmc_response.MD_RD_RS:
                    cmd = 0x3A;
                    break;
                case hmc_response.MD_WR_RS:
                    cmd = 0x3B;
                    break;
                case hmc_response.RSP_ERROR:
                    cmd = 0x3E;
                    break;
                case hmc_response.RSP_NONE:
                    cmd = 0x00;
                    break;
                default:
                    cmd = 0x00;
                    break;
            }

            return Macros.HMC_OK;
        }

        public int hmcsim_util_decode_rsp_slid(List<hmc_queue> queue, UInt32 slot, ref UInt32 slid)
        {
            /* vars */
            UInt64 header = 0x00uL;
            UInt32 tmp = 0x00;
            /* ---- */

            /*
             * sanity check
             *
             */




            if (queue == null)
            {
                return -1;
            }

            /*
             * get the packet header
             *
             */
            header = queue[(int)slot].packet[0];

            tmp = (UInt32)((header >> 39) & 0x7);

            /*
             * write it out
             *
             */
            slid = tmp;

            return 0;
        }
        public int hmcsim_power_xbar_rsp_slot(UInt32 dev, UInt32 link, UInt32 slot)
        {

            this.power.t_xbar_rsp_slot += this.power.xbar_rsp_slot;

            hmcsim_trace_power_xbar_rsp_slot(dev, link, slot);

            if (this.num_links == 4)
            {
                this.power.H4L.xbar_rsp_power[link] += this.power.xbar_rsp_slot;
                this.power.H4L.xbar_rsp_btu[link] += (this.power.xbar_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.xbar_rsp_power[link] += this.power.xbar_rsp_slot;
                this.power.H8L.xbar_rsp_btu[link] += (this.power.xbar_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }

        public int hmcsim_trace_power_xbar_rsp_slot(UInt32 dev, UInt32 link, UInt32 slot)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : XBAR_RSP_SLOT_POWER : ",
                       dev,
                       ":",
                       link,
                       ":",
                       slot,
                       ":",
                       this.power.xbar_rsp_slot);

            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : XBAR_RSP_SLOT_BTU : ",
           dev,
           ":",
           link,
           ":",
           slot,
           ":",
           this.power.xbar_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }
        public void hmcsim_token_update(UInt64[] pkt, UInt32 device, UInt32 link, UInt32 slot)
        {
            int tag = -1;
            int i = 0;
            int shift = 0;
            int cur = 0;

            /* get the tag */
            tag = (int)((pkt[0] >> 12) & 0x3FF);

            if (this.tokens[tag].rsp == hmc_response.RSP_NONE)
            {
                /* null response, probably a posted request */
                this.tokens[tag].status = 0;
                this.tokens[tag].rsp = hmc_response.RSP_NONE;
                this.tokens[tag].rsp_size = 0;
                this.tokens[tag].device = 0;
                this.tokens[tag].link = 0;
                this.tokens[tag].slot = 0;
                this.tokens[tag].en_clock = 0x00uL;
                for (i = 0; i < 256; i++)
                {
                    this.tokens[tag].data[i] = 0x0;
                }
                return;
            }

            /* set the status */
            this.tokens[tag].status = 2;
            this.tokens[tag].device = device;
            this.tokens[tag].link = link;
            this.tokens[tag].slot = slot;

            /* copy the data */
            do
            {
                this.tokens[tag].data[i] = (uint)((pkt[cur] >> shift) & 0x1FF);
                i++;
                shift += 8;
                if (shift == 64)
                {
                    shift = 0;
                    cur++;
                }
            } while (i < 256);
        }
        public int hmcsim_clock_print_vault_stats(UInt32 vault, List<hmc_queue> queue, UInt32 type)
        {
            /* vars */
            int nvalid = 0;
            int ninvalid = 0;
            int nstalled = 0;
            int nconflict = 0;
            int i = 0;
            /* ---- */

            for (i = 0; i < this.queue_depth; i++)
            {
                if (queue[i].valid == Macros.HMC_RQST_VALID)
                {
                    nvalid++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_INVALID)
                {
                    ninvalid++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_CONFLICT)
                {
                    nconflict++;
                }
                else if (queue[i].valid == Macros.HMC_RQST_STALLED)
                {
                    nstalled++;
                }
            }

            if (type == 0)
            {
                /* request */
                if (Config.DEBUG_MEMORY) DEBUG.WriteLine("RQST_VAULT:nvalid:ninvalid:nconflict:nstalled =" + vault + ":" + nvalid + ":" + ninvalid + ":" + nconflict + ":" + nstalled);
            }
            else {
                /* response */
                if (Config.DEBUG_MEMORY) DEBUG.WriteLine("RSP_VAULT:nvalid:ninvalid:nconflict:nstalled =" + vault + ":" + nvalid + ":" + ninvalid + ":" + nconflict + ":" + nstalled);
            }

            return 0;
        }

        public int hmcsim_clock_reorg_xbar_rqst(UInt32 dev, UInt32 link)
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            Int32 slot = 0;
            /* ---- */

            /*
             * walk the request queue starting at slot 1
             *
             */
            for (i = 1; i < this.xbar_depth; i++)
            {
                /*
                 * if the slot is valid, look upstream in the queue
                 *
                 */
                //if( this.devs[dev].xbar[link].xbar_rqst[i].valid == HMC_RQST_VALID ){
                if (this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * find the lowest appropriate slot
                     *
                     */
                    slot = i;
                    for (j = 0; j < i; j++)
                    {
                        if (this.devs[(int)dev].xbar[(int)link].xbar_rqst[j].valid == Macros.HMC_RQST_INVALID)
                        {
                            slot = j;
                            break;
                        }
                    }

                    /*
                     * check to see if a new slot was found
                     * if so, perform the swap
                     *
                     */
                    if (slot != i)
                    {

                        /*
                         * perform the swap
                         *
                         */
                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {

                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[slot].packet[j] =
                                this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[j];

                            this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].packet[j] = 0x00L;
                        }

                        this.devs[(int)dev].xbar[(int)link].xbar_rqst[slot].valid = 1;
                        this.devs[(int)dev].xbar[(int)link].xbar_rqst[i].valid = 0;
                    }
                } /* else, slot not valid.. move along */
            }

            return 0;
        }

        public int hmcsim_clock_reorg_xbar_rsp(UInt32 dev, UInt32 link)
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            Int32 slot = 0;
            /* ---- */

            /*
             * walk the response queue starting at slot 1
             *
             */
            for (i = 1; i < this.xbar_depth; i++)
            {
                /*
                 * if the slot is valid, look upstream in the queue
                 *
                 */
                //if( this.devs[dev].xbar[link].xbar_rsp[i].valid == HMC_RQST_VALID ){
                if (this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * find the lowest appropriate slot
                     *
                     */
                    slot = i;
                    for (j = 0; j < i; j++)
                    {
                        if (this.devs[(int)dev].xbar[(int)link].xbar_rsp[j].valid == Macros.HMC_RQST_INVALID)
                        {
                            slot = j;
                            break;
                        }
                    }

                    /*
                     * check to see if a new slot was found
                     * if so, perform the swap
                     *
                     */
                    if (slot != i)
                    {

                        /*
                         * perform the swap
                         *
                         */
                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {

                            this.devs[(int)dev].xbar[(int)link].xbar_rsp[slot].packet[j] =
                                this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].packet[j];

                            this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].packet[j] = 0x00L;
                        }

                        this.devs[(int)dev].xbar[(int)link].xbar_rsp[slot].valid = 1;
                        this.devs[(int)dev].xbar[(int)link].xbar_rsp[i].valid = 0;
                    }
                } /* else, slot not valid.. move along */
            }

            return 0;
        }

        public int hmcsim_clock_reorg_vault_rqst(UInt32 dev, UInt32 quad, UInt32 vault)
        {
            /* vars */
            UInt32 i = 0;
            UInt32 j = 0;
            UInt32 slot = 0;
            /* ---- */

            /*
             * walk the request queue starting at slot 1
             *
             */
            for (i = 1; i < this.queue_depth; i++)
            {
                /*
                 * if the slot is valid, look upstream in the queue
                 *
                 */
                //if( this.devs[dev].quads[quad].vaults[vault].rqst_queue[i].valid == HMC_RQST_VALID ){
                if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * find the lowest appropriate slot
                     *
                     */
                    slot = i;
                    for (j = 0; j < i; j++)
                    {
                        if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)j].valid == Macros.HMC_RQST_INVALID)
                        {
                            slot = j;
                            break;
                        }
                    }

                    /*
                     * check to see if a new slot was found
                     * if so, perform the swap
                     *
                     */
                    if (slot != i)
                    {

                        /*
                         * perform the swap
                         *
                         */
                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {

                            this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)slot].packet[j] =
                                this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)i].packet[j];

                            this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)i].packet[j] = 0x00L;

                        }

                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)slot].valid = 1;
                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rqst_queue[(int)i].valid = 0;
                    }
                } /* else, slot not valid.. move along */
            }

            return 0;
        }

        public int hmcsim_clock_reorg_vault_rsp(UInt32 dev, UInt32 quad, UInt32 vault)
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            Int32 slot = 0;
            /* ---- */

            /*
             * walk the response queue starting at slot 1
             *
             */
            for (i = 1; i < this.queue_depth; i++)
            {
                /*
                 * if the slot is valid, look upstream in the queue
                 *
                 */
                //if( this.devs[dev].quads[quad].vaults[vault].rsp_queue[i].valid == HMC_RQST_VALID ){
                if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[i].valid != Macros.HMC_RQST_INVALID)
                {

                    /*
                     * find the lowest appropriate slot
                     *
                     */
                    slot = i;
                    for (j = 0; j < i; j++)
                    {
                        if (this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[j].valid == Macros.HMC_RQST_INVALID)
                        {
                            slot = j;
                            break;
                        }
                    }

                    /*
                     * check to see if a new slot was found
                     * if so, perform the swap
                     *
                     */
                    if (slot != i)
                    {

                        /*
                         * perform the swap
                         *
                         */
                        for (j = 0; j < Macros.HMC_MAX_UQ_PACKET; j++)
                        {

                            this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[slot].packet[j] =
                                this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[i].packet[j];

                            this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[i].packet[j] = 0x00L;

                        }

                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[slot].valid = 1;
                        this.devs[(int)dev].quads[(int)quad].vaults[(int)vault].rsp_queue[i].valid = 0;
                    }
                } /* else, slot not valid.. move along */
            }

            return 0;
        }
        public int hmcsim_power_links()
        {

            int i = 0;



            this.power.t_link_phy += (float)(this.num_devs * this.num_links)
                                      * this.power.link_phy;

            hmcsim_trace_power_links();

            if (this.num_links == 4)
            {
                for (i = 0; i < 4; i++)
                {
                    this.power.H4L.link_phy_power[i] += this.power.link_phy;
                    this.power.H4L.link_phy_btu[i] += (this.power.link_phy * Macros.HMC_MILLIWATT_TO_BTU);
                }
            }
            else {
                for (i = 0; i < 8; i++)
                {
                    this.power.H8L.link_phy_power[i] += this.power.link_phy;
                    this.power.H8L.link_phy_btu[i] += (this.power.link_phy * Macros.HMC_MILLIWATT_TO_BTU);
                }
            }

            return 0;
        }
        public int hmcsim_trace_power_links()
        {
            UInt32 i = 0;
            UInt32 j = 0;


            for (j = 0; j < this.num_devs; j++)
            {

                for (i = 0; i < this.num_links; i++)
                {
                    this.tfile.WriteLine(
                               "%s%llu%s%u%s%u%s%f",
                               "HMCSIM_TRACE : ",
                               this.clk,
                               " : LINK_PHY_POWER : ",
                               j,/*dev*/
                               ":",
                               i,/*link*/
                               ":",
                               this.power.link_phy);
                    this.tfile.WriteLine(
                               "%s%llu%s%u%s%u%s%f",
             "HMCSIM_TRACE : ",
             this.clk,
             " : LINK_PHY_BTU : ",
             j,/*dev*/
             ":",
             i,/*link*/
             ":",
             this.power.link_phy * Macros.HMC_MILLIWATT_TO_BTU);
                }
            }

            return 0;
        }

        public int hmcsim_trace_power()
        {
            if (this.tfile == null)
            {
                return -1;
            }

            /*
             * write out the total trace values for power on this clock cycle
             *
             */
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_PHY_POWER : ",
                                 this.power.t_link_phy);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_LOCAL_ROUTE_POWER : ",
                                 this.power.t_link_local_route);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_REMOTE_ROUTE_POWER : ",
                                 this.power.t_link_remote_route);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_RQST_SLOT_POWER : ",
                                 this.power.t_xbar_rqst_slot);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_RSP_SLOT_POWER : ",
                                 this.power.t_xbar_rsp_slot);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_ROUTE_EXTERN_POWER : ",
                                 this.power.t_xbar_route_extern);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_RQST_SLOT_POWER : ",
                                 this.power.t_vault_rqst_slot);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_RSP_SLOT_POWER : ",
                                 this.power.t_vault_rsp_slot);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_CTRL_POWER : ",
                                 this.power.t_vault_ctrl);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_ROW_ACCESS_POWER : ",
                                 this.power.t_row_access);

            /* write out the thermal totals */
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_PHY_BTU : ",
                                 this.power.t_link_phy * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_LOCAL_ROUTE_BTU : ",
                                 this.power.t_link_local_route * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_LINK_REMOTE_ROUTE_BTU : ",
                                 this.power.t_link_remote_route * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_RQST_SLOT_BTU : ",
                                 this.power.t_xbar_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_RSP_SLOT_BTU : ",
                                 this.power.t_xbar_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_XBAR_ROUTE_EXTERN_BTU : ",
                                 this.power.t_xbar_route_extern * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_RQST_SLOT_BTU : ",
                                 this.power.t_vault_rqst_slot * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_RSP_SLOT_BTU : ",
                                 this.power.t_vault_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_VAULT_CTRL_BTU : ",
                                 this.power.t_vault_ctrl * Macros.HMC_MILLIWATT_TO_BTU);
            this.tfile.WriteLine("%s%llu%s%f",
                                 "HMCSIM_TRACE : ",
                                 this.clk,
                                 " : T_ROW_ACCESS_BTU : ",
                                 this.power.t_row_access * Macros.HMC_MILLIWATT_TO_BTU);


            return 0;
        }
        public int hmcsim_tecplot()
        {

            if (this.num_links == 4)
            {
                return hmcsim_tecplot4(this.power.H4L, this.clk, this.power.prefix);
            }
            else if (this.num_links == 8)
            {
                return hmcsim_tecplot8(this.power.H8L, this.clk, this.power.prefix);
            }
            else {
                return -1;
            }


        }

        public int hmcsim_tecplot8(HMC8LinkTec Tec, UInt64 clock, string prefix)
        {
            /* vars */
            string fname_p = "";
            string fname_t = "";


            int i = 0;
            int j = 0;
            int cur = 0;
            /* ---- */

            /* -- build the file names */




            fname_p = string.Format("%s%s%llu%s", prefix, "-power.", clock, ".tec");
            fname_t = string.Format("%s%s%llu%s", prefix, "-therm.", clock, ".tec");
            FileStream fs = new FileStream(fname_p, FileMode.OpenOrCreate);
            /* -- write the power results */
            /* -- open the file */
            StreamWriter ofile = new StreamWriter(fs);

            /* -- write all the data */

            /* -- -- header */
            ofile.Write("%s%llu%s\n",
                     "TITLE = \"HMC 8Link Simulation Clock ", clock, "\"");
            ofile.Write("%s\n", "VARIABLES = \"X\", \"Y\", \"Z\", \"Power\"");
            ofile.Write("%s\n", "ZONE T=\"HMC\", I=144, J=144, F=POINT");
            ofile.Write("\n\n");

            /* -- -- link+quad 0,1 */
            for (i = 0; i < 2; i++)
            {
                /* X Y Z Data */
                ofile.Write("%s%f%s%f\n",
                         "18 4.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 13.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 22.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 27 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 31.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                /* vault data */
                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "4.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "13.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 2,3 */
            for (i = 2; i < 4; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "54 4.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 13.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 22.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 27 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 31.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "22.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "31.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 4,5 */
            for (i = 4; i < 6; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "90 4.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 13.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 22.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 27 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 31.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "40.5 40.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 49.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 58.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "49.5 40.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 49.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 58.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 6,7 */
            for (i = 6; i < 8; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "126 4.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 13.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 22.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 27 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 31.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "58.5 40.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 49.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 58.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "67.5 40.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 49.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 58.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- dram data */
            ofile.Write("%s%f\n", "72 63 72 ", Tec.row_access_power);

            /* -- close the file */
            ofile.Close();

            fs.Close();

            /* -- write the thermal results */
            /* -- open the file */
            fs = new FileStream(fname_t, FileMode.OpenOrCreate);
            ofile = new StreamWriter(fs);

            /* -- write all the data */
            /* -- -- header */
            ofile.Write("%s%llu%s\n",
                     "TITLE = \"HMC 8Link Simulation Clock ", clock, "\"");
            ofile.Write("%s\n", "VARIABLES = \"X\", \"Y\", \"Z\", \"Btu\"");
            ofile.Write("%s\n", "ZONE T=\"HMC\", I=144, J=144, F=POINT");
            ofile.Write("\n\n");

            cur = 0;

            /* -- -- link+quad 0,1 */
            for (i = 0; i < 2; i++)
            {
                /* X Y Z Data */
                ofile.Write("%s%f%s%f\n",
                         "18 4.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 13.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 22.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 27 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 31.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                /* vault data */
                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "4.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "13.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 2,3 */
            for (i = 2; i < 4; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "54 4.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 13.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 22.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 27 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 31.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "22.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "31.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 4,5 */
            for (i = 4; i < 6; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "90 4.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 13.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 22.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 27 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "90 31.5 ", ((float)(i - 4) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "40.5 40.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 49.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 58.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "49.5 40.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 49.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 58.5 ",
                             ((float)(i - 4) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 6,7 */
            for (i = 6; i < 8; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "126 4.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 13.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 22.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 27 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "126 31.5 ", ((float)(i - 6) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "58.5 40.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 49.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 58.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "67.5 40.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 49.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 58.5 ",
                             ((float)(i - 6) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- dram data */
            ofile.Write("%s%f\n", "72 63 72 ", Tec.row_access_btu);

            /* -- close the file */
            ofile.Close();
            fs.Close();


            fname_p = null;
            fname_t = null;


            return 0;
        }

        public int hmcsim_tecplot4(HMC4LinkTec Tec, UInt64 clock, string prefix)
        {
            /* vars */
            string fname_p = "";
            string fname_t = "";

            int i = 0;
            int j = 0;
            int cur = 0;
            /* ---- */



            fname_p = string.Format("%s%s%llu%s", prefix, "-power.", clock, ".tec");
            fname_t = string.Format("%s%s%llu%s", prefix, "-therm.", clock, ".tec");

            /* -- write the power results */
            /* -- open the file */
            FileStream fs = new FileStream(fname_p, FileMode.OpenOrCreate);
            StreamWriter ofile = new StreamWriter(fs);

            /* -- write all the data */

            /* -- -- header */
            ofile.Write("%s%llu%s\n",
           "TITLE = \"HMC 4Link Simulation Clock ", clock, "\"");
            ofile.Write("%s\n", "VARIABLES = \"X\", \"Y\", \"Z\", \"Power\"");
            ofile.Write("%s\n", "ZONE T=\"HMC\", I=72, J=72, F=POINT");
            ofile.Write("\n\n");

            /* -- -- link+quad 0,1 */
            for (i = 0; i < 2; i++)
            {
                /* X Y Z Data */
                ofile.Write("%s%f%s%f\n",
                         "18 4.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 13.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 22.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 27 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 31.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                /* vault data */
                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "4.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "13.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 4; j < 6; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "22.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 6; j < 8; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "31.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 2,3 */
            for (i = 2; i < 4; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "54 4.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 13.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 22.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 27 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_power[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 31.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_power[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "40.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "49.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 4; j < 6; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "58.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
                for (j = 6; j < 8; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "67.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_power[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_power[cur]);
                    cur++;
                }
            }

            /* -- -- dram data */
            ofile.Write("%s%f\n", "36 63 36 ", Tec.row_access_power);

            /* -- close the file */
            ofile.Close();
            fs.Close();

            /* -- write the thermal results */
            /* -- open the file */
            fs = new FileStream(fname_t, FileMode.OpenOrCreate);
            ofile = new StreamWriter(fs);

            /* -- write all the data */
            /* -- -- header */
            ofile.Write("%s%llu%s\n",
                     "TITLE = \"HMC 4Link Simulation Clock ", clock, "\"");
            ofile.Write("%s\n", "VARIABLES = \"X\", \"Y\", \"Z\", \"Btu\"");
            ofile.Write("%s\n", "ZONE T=\"HMC\", I=72, J=72, F=POINT");
            ofile.Write("\n\n");

            cur = 0;

            /* -- -- link+quad 0,1 */
            for (i = 0; i < 2; i++)
            {
                /* X Y Z Data */
                ofile.Write("%s%f%s%f\n",
                         "18 4.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 13.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 22.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 27 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "18 31.5 ", ((float)(i) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                /* vault data */
                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "4.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "4.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "13.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "13.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 4; j < 6; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "22.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "22.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 6; j < 8; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "31.5 40.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 49.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "31.5 58.5 ",
                             ((float)(i) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- link+quad 2,3 */
            for (i = 2; i < 4; i++)
            {
                ofile.Write("%s%f%s%f\n",
                         "54 4.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_phy_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 13.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rqst_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 22.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.xbar_rsp_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 27 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_local_route_btu[i]);
                ofile.Write("%s%f%s%f\n",
                         "54 31.5 ", ((float)(i - 2) * (float)(36.0)) + (float)(18.0), " ",
                         Tec.link_remote_route_btu[i]);

                for (j = 0; j < 2; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "40.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "40.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 2; j < 4; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "49.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "49.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 2) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 4; j < 6; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "58.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "58.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 4) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
                for (j = 6; j < 8; j++)
                {
                    ofile.Write("%s%f%s%f\n",
                             "67.5 40.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rqst_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 49.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_rsp_btu[cur]);
                    ofile.Write("%s%f%s%f\n",
                             "67.5 58.5 ",
                             ((float)(i - 2) * (float)(36.0)) + ((float)(j - 6) * (float)(18)) + ((float)(9.0)),
                             " ", Tec.vault_ctrl_btu[cur]);
                    cur++;
                }
            }

            /* -- -- dram data */
            ofile.Write("%s%f\n", "36 63 36 ", Tec.row_access_btu);

            /* -- close the file */
            ofile.Close();
            fs.Close();

            fname_p = null;
            fname_t = null;

            return 0;
        }


        public UInt64 hmcsim_get_clock()
        {
            return this.clk;
        }

        public int hmcsim_jtag_reg_read(UInt32 dev, UInt64 reg, ref UInt64 result)
        {


            if (dev > this.num_devs)
            {
                return -1;
            }

            switch (reg)
            {
                case Macros.HMC_REG_EDR0:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_EDR0_IDX].reg;
                    break;
                case Macros.HMC_REG_EDR1:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_EDR1_IDX].reg;
                    break;
                case Macros.HMC_REG_EDR2:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_EDR2_IDX].reg;
                    break;
                case Macros.HMC_REG_EDR3:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_EDR3_IDX].reg;
                    break;
                case Macros.HMC_REG_ERR:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_ERR_IDX].reg;
                    break;
                case Macros.HMC_REG_GC:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_GC_IDX].reg;
                    break;
                case Macros.HMC_REG_LC0:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LC0_IDX].reg;
                    break;
                case Macros.HMC_REG_LC1:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LC1_IDX].reg;
                    break;
                case Macros.HMC_REG_LC2:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LC2_IDX].reg;
                    break;
                case Macros.HMC_REG_LC3:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LC3_IDX].reg;
                    break;
                case Macros.HMC_REG_LRLL0:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LRLL0_IDX].reg;
                    break;
                case Macros.HMC_REG_LRLL1:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LRLL1_IDX].reg;
                    break;
                case Macros.HMC_REG_LRLL2:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LRLL2_IDX].reg;
                    break;
                case Macros.HMC_REG_LRLL3:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LRLL3_IDX].reg;
                    break;
                case Macros.HMC_REG_LR0:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LR0_IDX].reg;
                    break;
                case Macros.HMC_REG_LR1:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LR1_IDX].reg;
                    break;
                case Macros.HMC_REG_LR2:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LR2_IDX].reg;
                    break;
                case Macros.HMC_REG_LR3:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_LR3_IDX].reg;
                    break;
                case Macros.HMC_REG_IBTC0:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_IBTC0_IDX].reg;
                    break;
                case Macros.HMC_REG_IBTC1:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_IBTC1_IDX].reg;
                    break;
                case Macros.HMC_REG_IBTC2:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_IBTC2_IDX].reg;
                    break;
                case Macros.HMC_REG_IBTC3:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_IBTC3_IDX].reg;
                    break;
                case Macros.HMC_REG_AC:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_AC_IDX].reg;
                    break;
                case Macros.HMC_REG_VCR:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_VCR_IDX].reg;
                    break;
                case Macros.HMC_REG_FEAT:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_FEAT_IDX].reg;
                    break;
                case Macros.HMC_REG_RVID:
                    result = this.devs[(int)dev].regs[Macros.HMC_REG_RVID_IDX].reg;
                    break;
                default:
                    return -1;
            }

            return 0;
        }

        public int hmcsim_jtag_write_err(UInt64 dev, UInt64 value)
        {
            /*
             * or' out the lower 25 bits
             *
             */
            UInt64 temp = 0x00L;
            temp = (value & 0x3FFFFFF);

            this.devs[(int)dev].regs[Macros.HMC_REG_ERR_IDX].reg |= temp;


            /*
             * check and see if we need to write
             * the start bit
             *
             */
            if ((value & 0x80000000) > 0)
            {
                this.devs[(int)dev].regs[Macros.HMC_REG_ERR_IDX].reg |= 0x80000000;
            }

            return 0;
        }

        public int hmcsim_jtag_write_gc(UInt64 dev, UInt64 value)
        {

            /*
             * check for warm reset
             *
             */
            if ((value & 0x40) > 0)
            {
                /*
                 * initiate a warm reset
                 *
                 */
            }

            /*
             * check for error clear
             *
             */
            if ((value * 0x20) > 0)
            {
                /* 
                 * clear all the errors
                 * 
                 */
            }

            UInt64 temp = 0x00L;
            temp = (value & 0x1F);

            this.devs[(int)dev].regs[Macros.HMC_REG_GC_IDX].reg |= temp;

            return 0;
        }

        public int hmcsim_util_decode_slid(List<hmc_queue> queue, UInt32 slot, ref UInt32 slid)
        {
            /* vars */
            UInt64 header = 0x00uL;
            UInt32 tmp = 0x00;
            UInt64 len = 0x00uL;
            UInt64 tail = 0x00uL;
            /* ---- */

            /*
             * sanity check
             *
             */


            /*
             * get the packet header
             *
             */
            header = queue[(int)slot].packet[0];

            /*
             * get the length of the packet
             *
             */
            len = (UInt64)((header >> 7) & 0x1F);

            /*
             * get the tail placement
             *
             */
            tail = (len * 2) - 1;

            /*
             * get the slid value [41:39]
             *
             */
            tmp = (UInt32)((queue[(int)slot].packet[tail] >> 26) & 0x7);

            /*
             * write it out
             *
             */
            slid = tmp;

            return 0;
        }


        public int hmcsim_load_cmc(string cmc_lib)
        {

            if (cmc_lib == null)
            {
                return -1;
            }

            /* register the library functions */
            if (hmcsim_register_functions(cmc_lib) != 0)
            {
                return -1;
            }

            return 0;
        }


        public int hmcsim_register_functions(string cmc_lib)
        {

            /* vars */
            hmc_rqst rqst = hmc_rqst.CMC102;
            UInt32 cmd = 0;
            UInt32 idx = 0;
            UInt32 rqst_len = 0;
            UInt32 rsp_len = 0;
            hmc_response rsp_cmd = hmc_response.WR_RS;
            uint rsp_cmd_code = 0;



            /* ---- */

            /* attempt to load the library */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("LOADING CMC LIBRARY");



            /* library is loaded, resolve the functions */
            /* -- hmcsim_register_cmc */
            if (CMC.hmcsim_register_cmc(ref rqst,
                                 ref cmd,
                                 ref rqst_len,
                                 ref rsp_len,
                                 ref rsp_cmd,
                                 ref rsp_cmd_code) != 0)
            {
                return -1;
            }

            /* -- hmcsim_execute_cmc */


            /* done loading functions */

            idx = hmcsim_cmc_rawtoidx(cmd);
            if (HMC_DEBUG)
                if (Config.DEBUG_MEMORY) DEBUG.WriteLine("HMCSIM_REGISTER_FUNCTIONS: Setting CMC command at IDX=" + idx + " to ACTIVE");


            if (this.cmcs[(int)idx].active == 1)
            {
                /* previously activated, this is an error */

                return -1;
            }

            /* write the necessary references into the structure */

            // this.cmcs[(int)idx].cmc_power    = cmc_power;
            this.cmcs[(int)idx].cmc_power = null;
            this.cmcs[(int)idx].track_power = 1;

            this.cmcs[(int)idx].type = rqst;
            this.cmcs[(int)idx].cmd = cmd;
            this.cmcs[(int)idx].rqst_len = rqst_len;
            this.cmcs[(int)idx].rsp_len = rsp_len;
            this.cmcs[(int)idx].rsp_cmd = rsp_cmd;

            this.cmcs[(int)idx].active = 1;

            // this.cmcs[(int)idx].cmc_register = cmc_register;
            // this.cmcs[(int)idx].cmc_execute  = cmc_execute;
            //this.cmcs[(int)idx].cmc_str      = cmc_str;
            this.cmcs[(int)idx].cmc_register = null;
            this.cmcs[(int)idx].cmc_execute = null;
            this.cmcs[(int)idx].cmc_str = null;

            return 0;
        }

        public int hmcsim_power_clear()
        {


            this.power.t_link_phy = 0.0f;
            this.power.t_link_local_route = 0.0f;
            this.power.t_link_remote_route = 0.0f;
            this.power.t_xbar_rqst_slot = 0.0f;
            this.power.t_xbar_rsp_slot = 0.0f;
            this.power.t_xbar_route_extern = 0.0f;
            this.power.t_vault_rqst_slot = 0.0f;
            this.power.t_vault_rsp_slot = 0.0f;
            this.power.t_vault_ctrl = 0.0f;
            this.power.t_row_access = 0.0f;

            return 0;
        }


    }
}
