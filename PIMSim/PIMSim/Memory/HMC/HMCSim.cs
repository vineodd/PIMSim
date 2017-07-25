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
        //these varibles will be added to overall configs

        public bool HMC_DEBUG = false;
        public bool HMC_ALLOC_MEM = true;


        /// ///////////////////////////

        public List<hmc_dev> devs;		/*! HMC-SIM: HMCSIM_T: DEVICE STRUCTURES */

        public List<hmc_cmc> cmcs;         /*! HMC-SIM: HMCSIM_T: REGISTERED CMC OPERATIONS */

        public UInt32 num_devs;      /*! HMC-SIM: HMCSIM_T: NUMBER OF DEVICES */
        public UInt32 num_links;     /*! HMC-SIM: HMCSIM_T: LINKS PER DEVICE */
        public UInt32 num_quads;     /*! HMC-SIM: HCMSIM_T: QUADS PER DEVICE */
        public UInt32 num_vaults;        /*! HMC-SIM: HMCSIM_T: VAULTS PER DEVICE */
        public UInt32 num_banks;     /*! HMC-SIM: HMCSIM_T: BANKS PER VAULT */
        public UInt32 num_drams;     /*! HMC-SIM: HMCSIM_T: DRAMS PER BANK */
        public UInt32 capacity;     /*! HMC-SIM: HMCSIM_T: CAPACITY PER DEVICE */

        public UInt32 num_cmc;               /*! HMC-SIM: HMCSIM_T: NUMBER OF REGISTERED CMC OPERATIONS */

        public UInt32 queue_depth;       /*! HMC-SIM: HMCSIM_T: VAULT QUEUE DEPTH */
        public UInt32 xbar_depth;       /*! HMC-SIM: HMCSIM_T: CROSSBAR QUEUE DEPTH */

        public UInt32 dramlatency;           /*! HMC-sIM: HMCSIM_T: DRAM ACCESS LATENCY IN CYCLES */

        public FileStream fs;
        public StreamWriter tfile;            /*! HMC-SIM: HMCSIM_T: TRACE FILE HANDLER */
        public UInt32 tracelevel;        /*! HMC-SIM: HMCSIM_T: TRACE LEVEL */

        public uint seq;            /*! HMC-SIM: HCMSIM_T: SEQUENCE NUMBER */

        public UInt64 clk;          /*! HMC-SIM: HMCSIM_T: CLOCK TICK */

        public UInt32 simple_link;           /*! HMC-SIM: HMCSIM_T: SIMPLIFIED API LINK HANDLER */

        public hmc_power power;       /*! HMC-SIM: HMCSIM_T: POWER MEASUREMENT VALUES */

        public hmc_token[] tokens;/*! HMC-SIM: HMCSIM_T: SIMPLE API TOKEN HANDLERS */

        //int (*readmem)(struct hmcsim_t *,
        //               UInt64,
        //               UInt64*,
        //               UInt32 );
        public delegate int readmem(UInt64 T1, List<UInt64> T2, UInt32 T3);

        //int (*writemem)(struct hmcsim_t *,
        //               UInt64,
        //               UInt64*,
        //               UInt32 );
        public delegate int writemem(UInt64 T1, List<UInt64> T2, UInt32 T3);

        public List<hmc_dev> __ptr_devs;
        public List<hmc_quad> __ptr_quads;
        public List<hmc_vault> __ptr_vaults;
        public List<hmc_bank> __ptr_banks;
        public List<hmc_dram> __ptr_drams;
        public List<hmc_link> __ptr_links;
        public List<hmc_xbar> __ptr_xbars;
        public List<hmc_queue> __ptr_xbar_rqst;
        public List<hmc_queue> __ptr_xbar_rsp;
        public List<hmc_queue> __ptr_vault_rqst;
        public List<hmc_queue> __ptr_vault_rsp;
        public List<UInt64> __ptr_stor;
        public List<UInt64> __ptr_end;

        public HMCSim()
        {
            devs = new List<hmc_dev>();
            cmcs = new List<hmc_cmc>();
            __ptr_devs = new List<hmc_dev>();
            __ptr_quads = new List<hmc_quad>();
            __ptr_vaults = new List<hmc_vault>();
            __ptr_banks = new List<hmc_bank>();
            __ptr_drams = new List<hmc_dram>();
            __ptr_links = new List<hmc_link>();

            __ptr_xbars = new List<hmc_xbar>();
            __ptr_xbar_rqst = new List<hmc_queue>();
            __ptr_xbar_rsp = new List<hmc_queue>();
            __ptr_vault_rqst = new List<hmc_queue>();
            __ptr_vault_rsp = new List<hmc_queue>();
            __ptr_stor = new List<ulong>();
            __ptr_end = new List<ulong>();
            tokens = new hmc_token[1024];
            for (int i = 0; i < 1024; i++)
                tokens[i] = new hmc_token();
        }
        public int hmcsim_init(UInt32 num_devs, UInt32 num_links, UInt32 num_vaults, UInt32 queue_depth, UInt32 num_banks, UInt32 num_drams, UInt32 capacity, UInt32 xbar_depth)
        {
            /* vars */
            UInt32 i = 0;
            /* ---- */


            /*
             * sanity check the args
             *
             */
            if ((num_devs > Macros.HMC_MAX_DEVS) || (num_devs < 1))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_links < Macros.HMC_MIN_LINKS) || (num_links > Macros.HMC_MAX_LINKS))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_vaults < Macros.HMC_MIN_VAULTS) || (num_vaults > Macros.HMC_MAX_VAULTS))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_banks < Macros.HMC_MIN_BANKS) || (num_banks > Macros.HMC_MAX_BANKS))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_drams < Macros.HMC_MIN_DRAMS) || (num_drams > Macros.HMC_MAX_DRAMS))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((capacity < Macros.HMC_MIN_CAPACITY) || (capacity > Macros.HMC_MAX_CAPACITY))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((queue_depth < Macros.HMC_MIN_QUEUE_DEPTH) || (queue_depth > Macros.HMC_MAX_QUEUE_DEPTH))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((xbar_depth < Macros.HMC_MIN_QUEUE_DEPTH) || (xbar_depth > Macros.HMC_MAX_QUEUE_DEPTH))
            {
                return Macros.HMC_ERROR_PARAMS;
            }

            if (HMC_DEBUG)
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("PASSED LEVEL1 INIT SANITY CHECK");


            /*
             * look deeper to make sure the default addressing works
             * and the vault counts
             *
             */
            if ((num_banks != 8) && (num_banks != 16))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_links != 4) && (num_links != 8))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((num_vaults / num_links) != 8)
            {
                /* always maintain 4 vaults per quad, or link */
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((capacity % 2) != 0)
            {
                return Macros.HMC_ERROR_PARAMS;
            }

            if (HMC_DEBUG)
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("PASSED LEVEL2 INIT SANITY CHECK");


            /*
             * go deeper still...
             *
             */
            if ((capacity == 2) && ((num_banks == 16) || (num_links == 8)))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((capacity == 4) && ((num_banks == 16) && (num_links == 8)))
            {
                return Macros.HMC_ERROR_PARAMS;
            }
            else if ((capacity == 8) && ((num_banks == 8) || (num_links == 4)))
            {
                return Macros.HMC_ERROR_PARAMS;
            }

            if (HMC_DEBUG)
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("PASSED LEVEL3 INIT SANITY CHECK");


            /*
             * init all the internals
             *
             */
            tfile = null;
            tracelevel = 0x00;

            this.num_devs = num_devs;
            this.num_links = num_links;
            this.num_vaults = num_vaults;
            this.num_banks = num_banks;
            this.num_drams = num_drams;
            this.capacity = capacity;
            this.num_cmc = 0x00;
            this.simple_link = 0;
            this.queue_depth = queue_depth;
            this.xbar_depth = xbar_depth;
            this.dramlatency = Macros.HMC_DEF_DRAM_LATENCY;  // default dram latency

            this.clk = 0x00;

            if (num_links == 4)
            {
                this.num_quads = 4;
            }
            else {
                this.num_quads = 8;
            }

            /*
             * pointers
             */
            this.__ptr_devs = null;
            this.__ptr_quads = null;
            this.__ptr_vaults = null;
            this.__ptr_banks = null;
            this.__ptr_drams = null;
            this.__ptr_links = null;
            this.__ptr_xbars = null;
            this.__ptr_stor = null;
            this.__ptr_xbar_rqst = null;
            this.__ptr_xbar_rsp = null;
            this.__ptr_vault_rqst = null;
            this.__ptr_vault_rsp = null;

            /*
             *
             * allocate memory
             *
             */
            if (hmcsim_allocate_memory() != 0)
            {
                /*
                 * probably ran out of memory
                 *
                 */
                if (HMC_DEBUG)
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("FAILED TO ALLOCATE INTERNAL MEMORY");


                return -1;
            }


            /*
             * configure all the devices
             *
             */
            if (hmcsim_config_devices() != 0)
            {
                if (HMC_DEBUG)
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("FAILED TO CONFIGURE THE INTERNAL DEVICE STRUCTURE");


                hmcsim_free_memory();
                return -1;
            }

            /*
             * warm reset all the devices
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {

                //hmc_reset_device( hmc, i );
            }

            /*
             * init the power values
             *
             */
            hmcsim_init_power();

            /*
             * init the token handlers for simplified api
             *
             */
            hmcsim_init_tokens();

            return 0;
        }
        public int hmcsim_allocate_memory()
        {


            this.cmcs = new List<hmc_cmc>(Macros.HMC_MAX_CMC);
            for (int i = 0; i < Macros.HMC_MAX_CMC; i++)
                this.cmcs.Add(new hmc_cmc());


            this.__ptr_devs = new List<hmc_dev>((int)this.num_devs);
            for (int i = 0; i < (int)this.num_devs; i++)
                this.__ptr_devs.Add(new hmc_dev());

            this.__ptr_quads = new List<hmc_quad>((int)this.num_devs * (int)this.num_quads);
            for (int i = 0; i < (int)this.num_devs * (int)this.num_quads; i++)
                this.__ptr_quads.Add(new hmc_quad());

            //this.__ptr_vaults = malloc( sizeof( struct hmc_vault_t ) * this.num_devs * this.num_vaults );
            // this should be *4, 8 vaults per quadrant
            this.__ptr_vaults = new List<hmc_vault>((int)this.num_devs * (int)this.num_vaults * 8);

            for (int i = 0; i < (int)this.num_devs * (int)this.num_vaults * 8; i++)
                this.__ptr_vaults.Add(new hmc_vault());


            this.__ptr_banks = new List<hmc_bank>(
                    //* this.num_devs * this.num_vaults * 8 * this.num_banks );
                    (int)this.num_devs * (int)this.num_vaults * (int)this.num_banks);
            for (int i = 0; i < (int)this.num_devs * (int)this.num_vaults * (int)this.num_banks; i++)
                this.__ptr_banks.Add(new hmc_bank());

            if (false)
            {
                __ptr_drams = new List<hmc_dram>(
                                (int)this.num_devs * (int)this.num_vaults * (int)this.num_banks
                                * (int)this.num_drams);
            }
            this.__ptr_links = new List<hmc_link>((int)this.num_devs * (int)this.num_links);
            for (int i = 0; i < (int)this.num_devs * (int)this.num_links; i++)
                this.__ptr_links.Add(new hmc_link());


            this.__ptr_xbars = new List<hmc_xbar>((int)this.num_devs * (int)this.num_links);

            for (int i = 0; i < (int)this.num_devs * (int)this.num_links; i++)
                this.__ptr_xbars.Add(new hmc_xbar());
            //            if (HMC_ALLOC_MEM)
            //                this.__ptr_stor = new List<ulong>((int)this.num_devs * (int)this.capacity * Macros.HMC_1GB);
            //	if( this.__ptr_stor == NULL ){ 
            //if( HMC_DEBUG)
            //                HMCSIM_PRINT_TRACE( "FAILED TO ALLOCATE __ptr_stor" );
            //#endif
            //                printf( "DUMPING OUT; CAN'T ALLOC MEMORY" );
            //		return -1;
            //	}

            //        this.__ptr_end  = (UInt64*)( this.__ptr_stor
            //                                        + (sizeof( UInt64 ) *
            //                                           this.num_devs*
            //                                           this.capacity* HMC_1GB) );
            //#endif

            this.__ptr_xbar_rqst = new List<hmc_queue>((int)this.num_devs
                                        * (int)this.xbar_depth
                                        * (int)this.num_links);

            for (int i = 0; i < (int)this.num_devs * (int)this.xbar_depth * (int)this.num_links; i++)
                this.__ptr_xbar_rqst.Add(new hmc_queue());


            this.__ptr_xbar_rsp = new List<hmc_queue>(
                       (int)this.num_devs
                                        * (int)this.xbar_depth
                                        * (int)this.num_links);
            for (int i = 0; i < (int)this.num_devs * (int)this.xbar_depth * (int)this.num_links; i++)
                this.__ptr_xbar_rsp.Add(new hmc_queue());


            this.__ptr_vault_rqst = new List<hmc_queue>((int)this.num_devs * (int)this.num_vaults * (int)this.queue_depth);
            for (int i = 0; i < (int)this.num_devs * (int)this.num_vaults * (int)this.queue_depth; i++)
                this.__ptr_vault_rqst.Add(new hmc_queue());

            this.__ptr_vault_rsp = new List<hmc_queue>((int)this.num_devs * (int)this.num_vaults * (int)this.queue_depth);

            for (int i = 0; i < (int)this.num_devs * (int)this.num_vaults * (int)this.queue_depth; i++)
                this.__ptr_vault_rsp.Add(new hmc_queue());
            //this.readmem  = &(hmcsim_readmem);
            //this.writemem = &(hmcsim_writemem);

            return 0;
        }

        public int hmcsim_config_devices()
        {
            /* vars */
            int i = 0;
            int j = 0;
            int k = 0;
            int x = 0;
            int y = 0;
            int a = 0;
            int cur_quad = 0;
            int cur_vault = 0;
            int cur_bank = 0;
            int cur_dram = 0;
            int cur_link = 0;
            int cur_queue = 0;
            int cur_xbar = 0;
            /* ---- */

            /*
             * sanity check
             *
             */


            /*
             * init the cmc structure
             *
             */
            hmcsim_config_cmc();


            /*
             * set the device pointers
             *
             */
            this.devs = this.__ptr_devs;

            /*
             * zero the sequence number
             *
             */
            this.seq = 0x00;

            /*
             * for each device, set the sub-device pointers
             *
             */

            for (i = 0; i < this.num_devs; i++)
            {

                /*
                 * set the id
                 *
                 */
                this.devs[i].id = (uint)i;

                /*
                 * zero the sequence number
                 *
                 */
                this.devs[i].seq = 0x00;

                /*
                 * config the register file
                 *
                 */
                hmcsim_config_dev_reg(i);

                /*
                 * links on each device
                 *
                 */
                //    this.devs[i].links = this.__ptr_links[cur_link];
                for (int tp = 0; tp < num_links; tp++)
                    this.devs[i].links.Add(__ptr_links[cur_link + tp]);


                /*
                 * xbars on each device
                 *
                 */

                //  this.devs[i].xbar = this.__ptr_xbars[cur_link];
                for (int tp = 0; tp < num_links; tp++)
                    this.devs[i].xbar.Add(__ptr_xbars[cur_link + tp]);

                for (j = 0; j < this.num_links; j++)
                {

                    /*
                     * xbar queues
                     *
                     */
                    //  this.devs[i].xbar[j].xbar_rqst = &(this.__ptr_xbar_rqst[cur_xbar]);
                    for (int tp = 0; tp < this.xbar_depth; tp++)
                        this.devs[i].xbar[j].xbar_rqst.Add(__ptr_xbar_rqst[cur_xbar + tp]);
                    // this.devs[i].xbar[j].xbar_rsp = &(this.__ptr_xbar_rsp[cur_xbar]);
                    for (int tp = 0; tp < this.xbar_depth; tp++)
                        this.devs[i].xbar[j].xbar_rsp.Add(__ptr_xbar_rsp[cur_xbar + tp]);
                    if (false)
                    {
                        //if(Config.DEBUG_MEMORY)DEBUG.WriteLine("this.devs[].xbar[].xbar_rsp  = 0x%016llx",
                        //                (UInt64) & (this.__ptr_xbar_rsp[cur_xbar]));
                    }

                    for (a = 0; a < this.xbar_depth; a++)
                    {
                        this.devs[i].xbar[j].xbar_rqst[a].valid = Macros.HMC_RQST_INVALID;
                        this.devs[i].xbar[j].xbar_rsp[a].valid = Macros.HMC_RQST_INVALID;
                    }

                    cur_xbar += (int)this.xbar_depth;

                    /*
                     * set the id
                     *
                     */
                    this.devs[i].links[j].id = (uint)j;

                    /*
                     * set the type and cubs
                     * by default, everyone connects to the host
                     *
                     */
                    this.devs[i].links[j].type = hmc_link_def.HMC_LINK_HOST_DEV;
                    this.devs[i].links[j].src_cub = this.num_devs + 1;
                    this.devs[i].links[j].dest_cub = (uint)i;

                    /* 
                     * set the associated quad
                     * quad == link 
                     */
                    this.devs[i].links[j].quad = (uint)j;
                }

                cur_link += (int)this.num_links;

                /*
                 * quads on each device
                 *
                 */
                // this.devs[i].quads = &(this.__ptr_quads[cur_quad]);
                for (int tp = 0; tp < this.num_quads; tp++)
                    this.devs[i].quads.Add(__ptr_quads[cur_quad + tp]);

                for (j = 0; j < this.num_links; j++)
                {

                    /*
                     * set the id
                     *
                     */
                    this.devs[i].quads[j].id = (uint)j;

                    /*
                     * vaults in each quad
                     *
                     */
                    //  this.devs[i].quads[j].vaults = &(this.__ptr_vaults[cur_vault]);

                    for (int tp = 0; tp < 8; tp++)
                        this.devs[i].quads[j].vaults.Add(__ptr_vaults[cur_vault + tp]);

                    /* always 8 vaults per quad */
                    for (k = 0; k < 8; k++)
                    {

                        /*
                         * set the id
                         *
                         */
                        this.devs[i].quads[j].vaults[k].id = (uint)k;

                        /*
                         * banks in each vault
                         *
                         */
                        //  this.devs[i].quads[j].vaults[k].banks = &(this.__ptr_banks[cur_bank]);
                        for (int tp = 0; tp < this.num_banks; tp++)
                            this.devs[i].quads[j].vaults[k].banks.Add(__ptr_banks[cur_bank + tp]);
                        /*
                         * request and response queues
                         *
                         */
                        //  this.devs[i].quads[j].vaults[k].rqst_queue = &(this.__ptr_vault_rqst[cur_queue]);
                        for (int tp = 0; tp < this.queue_depth; tp++)
                            this.devs[i].quads[j].vaults[k].rqst_queue.Add(__ptr_vault_rqst[cur_queue + tp]);

                        // this.devs[i].quads[j].vaults[k].rsp_queue = &(this.__ptr_vault_rsp[cur_queue]);
                        for (int tp = 0; tp < this.queue_depth; tp++)
                            this.devs[i].quads[j].vaults[k].rsp_queue.Add(__ptr_vault_rsp[cur_queue + tp]);

                        /*
                         * clear the valid bits
                         *
                         */
                        for (a = 0; a < this.queue_depth; a++)
                        {
                            this.devs[i].quads[j].vaults[k].rqst_queue[a].valid = Macros.HMC_RQST_INVALID;
                            this.devs[i].quads[j].vaults[k].rsp_queue[a].valid = Macros.HMC_RQST_INVALID;
                        }

                        for (x = 0; x < this.num_banks; x++)
                        {

                            /*
                             * set the id and initial delay
                             *
                             */
                            this.devs[i].quads[j].vaults[k].banks[x].id = (uint)x;
                            this.devs[i].quads[j].vaults[k].banks[x].delay = 0;
                            this.devs[i].quads[j].vaults[k].banks[x].valid = Macros.HMC_RQST_INVALID;

                            /*
         * drams in each bank
         *
         */
                            if (false)
                            {
                                //this.devs[i].quads[j].vaults[k].banks[x].drams =
                                //                &(this.__ptr_drams[cur_dram]);
                            }

                            for (y = 0; y < this.num_drams; y++)
                            {

                                /*
                                 * set the id
                                 *
                                 */
                                if (false)
                                {
                                    //this.devs[i].quads[j].vaults[k].banks[x].drams[y].id = y;
                                }
                            }
                            cur_dram += (int)this.num_drams;
                        }
                        cur_bank += (int)this.num_banks;
                        cur_queue += (int)this.queue_depth;
                    } /* k=0; k<8; k++ */
                    cur_vault += 8;
                }
                cur_quad += (int)this.num_quads;
            }

            return 0;
        }


        public int hmcsim_config_cmc()
        {

            /* vars */
            int i = 0;
            /* ---- */

            for (i = 0; i < Macros.HMC_MAX_CMC; i++)
            {
                this.cmcs[i].type = hmc_rqst.FLOW_NULL;  /* default to a null op */
                this.cmcs[i].cmd = 0x00;
                this.cmcs[i].rsp_len = 0x0;
                this.cmcs[i].rsp_cmd_code = 0x0;
                this.cmcs[i].active = 0;          /* disable this op by default */
                 this.cmcs[i].cmc_register = null;
                    this.cmcs[i].cmc_execute = null;
                   this.cmcs[i].cmc_str = null;
            }

            return 0;
        }


        public void hmcsim_init_power()
        {
            int i = 0;

            this.power = new hmc_power();
            /* -- local values */
            this.power.link_phy = 0.1f;
            this.power.link_local_route = 0.1f;
            this.power.link_remote_route = 0.1f;
            this.power.xbar_rqst_slot = 0.1f;
            this.power.xbar_rsp_slot = 0.1f;
            this.power.xbar_route_extern = 0.1f;
            this.power.vault_rqst_slot = 0.1f;
            this.power.vault_rsp_slot = 0.1f;
            this.power.vault_ctrl = 0.1f;
            this.power.row_access = 0.1f;

            /* -- totals */
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

            /* -- output formats */
            this.power.tecplot = 0;

            /* -- tecplot values */
            this.power.H4L.row_access_power = 0.0f;
            this.power.H4L.row_access_btu = 0.0f;
            this.power.H8L.row_access_power = 0.0f;
            this.power.H8L.row_access_btu = 0.0f;

            for (i = 0; i < 32; i++)
            {
                this.power.H4L.vault_rsp_power[i] = 0.0f;
                this.power.H4L.vault_rqst_power[i] = 0.0f;
                this.power.H4L.vault_ctrl_power[i] = 0.0f;
                this.power.H4L.vault_rsp_btu[i] = 0.0f;
                this.power.H4L.vault_rqst_btu[i] = 0.0f;
                this.power.H4L.vault_ctrl_btu[i] = 0.0f;

                this.power.H8L.vault_rsp_power[i] = 0.0f;
                this.power.H8L.vault_rqst_power[i] = 0.0f;
                this.power.H8L.vault_ctrl_power[i] = 0.0f;
                this.power.H8L.vault_rsp_btu[i] = 0.0f;
                this.power.H8L.vault_rqst_btu[i] = 0.0f;
                this.power.H8L.vault_ctrl_btu[i] = 0.0f;
            }

            for (i = 0; i < 4; i++)
            {
                this.power.H4L.xbar_rqst_power[i] = 0.0f;
                this.power.H4L.xbar_rsp_power[i] = 0.0f;
                this.power.H4L.xbar_route_extern_power[i] = 0.0f;
                this.power.H4L.link_local_route_power[i] = 0.0f;
                this.power.H4L.link_remote_route_power[i] = 0.0f;
                this.power.H4L.link_phy_power[i] = 0.0f;

                this.power.H4L.xbar_rqst_btu[i] = 0.0f;
                this.power.H4L.xbar_rsp_btu[i] = 0.0f;
                this.power.H4L.xbar_route_extern_btu[i] = 0.0f;
                this.power.H4L.link_local_route_btu[i] = 0.0f;
                this.power.H4L.link_remote_route_btu[i] = 0.0f;
                this.power.H4L.link_phy_btu[i] = 0.0f;
            }

            for (i = 0; i < 8; i++)
            {
                this.power.H8L.xbar_rqst_power[i] = 0.0f;
                this.power.H8L.xbar_rsp_power[i] = 0.0f;
                this.power.H8L.xbar_route_extern_power[i] = 0.0f;
                this.power.H8L.link_local_route_power[i] = 0.0f;
                this.power.H8L.link_remote_route_power[i] = 0.0f;
                this.power.H8L.link_phy_power[i] = 0.0f;

                this.power.H8L.xbar_rqst_btu[i] = 0.0f;
                this.power.H8L.xbar_rsp_btu[i] = 0.0f;
                this.power.H8L.xbar_route_extern_btu[i] = 0.0f;
                this.power.H8L.link_local_route_btu[i] = 0.0f;
                this.power.H8L.link_remote_route_btu[i] = 0.0f;
                this.power.H8L.link_phy_btu[i] = 0.0f;
            }
        }
        public void hmcsim_init_tokens()
        {
            int i = 0;
            int j = 0;
            for (i = 0; i < 1024; i++)
            {
                this.tokens[i].status = 0;
                this.tokens[i].rsp = hmc_response.RSP_NONE;
                this.tokens[i].rsp_size = 0;
                this.tokens[i].device = 0;
                this.tokens[i].link = 0;
                this.tokens[i].slot = 0;
                this.tokens[i].en_clock = 0x00ul;
                for (j = 0; j < 256; j++)
                {
                    this.tokens[i].data[j] = 0x0;
                }
            }
        }


        public int hmcsim_config_dev_reg(Int32 dev)
        {


            this.devs[dev].regs[Macros.HMC_REG_EDR0_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_EDR0_IDX].phy_idx = Macros.HMC_REG_EDR0;
            this.devs[dev].regs[Macros.HMC_REG_EDR0_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_EDR1_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_EDR1_IDX].phy_idx = Macros.HMC_REG_EDR1;
            this.devs[dev].regs[Macros.HMC_REG_EDR1_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_EDR2_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_EDR2_IDX].phy_idx = Macros.HMC_REG_EDR2;
            this.devs[dev].regs[Macros.HMC_REG_EDR2_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_EDR3_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_EDR3_IDX].phy_idx = Macros.HMC_REG_EDR3;
            this.devs[dev].regs[Macros.HMC_REG_EDR3_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_ERR_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_ERR_IDX].phy_idx = Macros.HMC_REG_ERR;
            this.devs[dev].regs[Macros.HMC_REG_ERR_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_GC_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_GC_IDX].phy_idx = Macros.HMC_REG_GC;
            this.devs[dev].regs[Macros.HMC_REG_GC_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LC0_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LC0_IDX].phy_idx = Macros.HMC_REG_LC0;
            devs[dev].regs[Macros.HMC_REG_LC0_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LC1_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LC1_IDX].phy_idx = Macros.HMC_REG_LC1;
            this.devs[dev].regs[Macros.HMC_REG_LC1_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LC2_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LC2_IDX].phy_idx = Macros.HMC_REG_LC2;
            this.devs[dev].regs[Macros.HMC_REG_LC2_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LC3_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LC3_IDX].phy_idx = Macros.HMC_REG_LC3;
            this.devs[dev].regs[Macros.HMC_REG_LC3_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LRLL0_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LRLL0_IDX].phy_idx = Macros.HMC_REG_LRLL0;
            this.devs[dev].regs[Macros.HMC_REG_LRLL0_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LRLL1_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LRLL1_IDX].phy_idx = Macros.HMC_REG_LRLL1;
            this.devs[dev].regs[Macros.HMC_REG_LRLL1_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LRLL2_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LRLL2_IDX].phy_idx = Macros.HMC_REG_LRLL2;
            this.devs[dev].regs[Macros.HMC_REG_LRLL2_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LRLL3_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LRLL3_IDX].phy_idx = Macros.HMC_REG_LRLL3;
            this.devs[dev].regs[Macros.HMC_REG_LRLL3_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LR0_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LR0_IDX].phy_idx = Macros.HMC_REG_LR0;
            this.devs[dev].regs[Macros.HMC_REG_LR0_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LR1_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LR1_IDX].phy_idx = Macros.HMC_REG_LR1;
            this.devs[dev].regs[Macros.HMC_REG_LR1_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LR2_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LR2_IDX].phy_idx = Macros.HMC_REG_LR2;
            this.devs[dev].regs[Macros.HMC_REG_LR2_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_LR3_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_LR3_IDX].phy_idx = Macros.HMC_REG_LR3;
            this.devs[dev].regs[Macros.HMC_REG_LR3_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_IBTC0_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_IBTC0_IDX].phy_idx = Macros.HMC_REG_IBTC0;
            this.devs[dev].regs[Macros.HMC_REG_IBTC0_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_IBTC1_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_IBTC1_IDX].phy_idx = Macros.HMC_REG_IBTC1;
            this.devs[dev].regs[Macros.HMC_REG_IBTC1_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_IBTC2_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_IBTC2_IDX].phy_idx = Macros.HMC_REG_IBTC2;
            this.devs[dev].regs[Macros.HMC_REG_IBTC2_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_IBTC3_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_IBTC3_IDX].phy_idx = Macros.HMC_REG_IBTC3;
            this.devs[dev].regs[Macros.HMC_REG_IBTC3_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_AC_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_AC_IDX].phy_idx = Macros.HMC_REG_AC;
            this.devs[dev].regs[Macros.HMC_REG_AC_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_VCR_IDX].type = hmc_reg_def.HMC_RW;
            this.devs[dev].regs[Macros.HMC_REG_VCR_IDX].phy_idx = Macros.HMC_REG_VCR;
            this.devs[dev].regs[Macros.HMC_REG_VCR_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_FEAT_IDX].type = hmc_reg_def.HMC_RO;
            this.devs[dev].regs[Macros.HMC_REG_FEAT_IDX].phy_idx = Macros.HMC_REG_FEAT;
            this.devs[dev].regs[Macros.HMC_REG_FEAT_IDX].reg = 0x00L;

            this.devs[dev].regs[Macros.HMC_REG_RVID_IDX].type = hmc_reg_def.HMC_RO;
            this.devs[dev].regs[Macros.HMC_REG_RVID_IDX].phy_idx = Macros.HMC_REG_RVID;
            this.devs[dev].regs[Macros.HMC_REG_RVID_IDX].reg = 0x00L;


            /*
             * write the feature revision register
             *
             */
            hmcsim_config_feat_reg(dev);


            /*
             * write the device revision register
             *
             */
            hmcsim_config_rvid_reg(dev);

            return 0;
        }


        public int hmcsim_config_feat_reg(Int32 dev)
        {
            /*
             * write the necessary data for the feature register
             *
             */
            UInt64 feat = 0x00L;
            UInt64 size = 0x00L;
            UInt64 vaults = 0x00L;
            UInt64 banks = 0x00L;
            UInt64 phy = Macros.HMC_PHY_SPEED;

            /*
             * determine capacity
             *
             */
            switch (this.capacity)
            {
                case 2:
                    size = 0x00;
                    break;
                case 4:
                    size = 0x01;
                    break;
                case 8:
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
            switch (this.num_vaults)
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
            switch (this.num_banks)
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
            feat |= (vaults << 4);
            feat |= (banks << 8);
            feat |= (phy << 12);

            /*
             * write the register
             *
             */
            this.devs[dev].regs[Macros.HMC_REG_FEAT_IDX].reg = feat;


            return 0;
        }
        public int hmcsim_config_rvid_reg(Int32 dev)
        {
            /*
             * write the necessary data for the revision, vendor, product
             * protocol and phy
             *
             */

            UInt64 rev = 0x00L;

            /*
             * vendor
             *
             */
            rev |= Macros.HMC_VENDOR_ID;

            /*
             * product revision
             *
             */
            rev |= (UInt64)(Macros.HMC_PRODUCT_REVISION << 8);


            /*
             * protocol revision
             *
             */
            rev |= (UInt64)(Macros.HMC_PROTOCOL_REVISION << 16);

            /*
             * phy revision
             *
             */
            rev |= (UInt64)(Macros.HMC_PHY_REVISION << 24);

            /*
             * write the register
             *
             */
            this.devs[dev].regs[Macros.HMC_REG_RVID_IDX].reg = rev;

            return 0;
        }

        public int hmcsim_util_set_all_max_blocksize(UInt32 bsize)
        {
            /* vars */
            Int32 i = 0;
            /* ---- */


            /* 
             * check the bounds of the block size
             *
             */
            if ((bsize != 32) &&
                (bsize != 64) &&
                (bsize != 128) && (bsize != 256))
            {
                return -1;
            }

            for (i = 0; i < this.num_devs; i++)
            {

                hmcsim_util_set_max_blocksize(i, bsize);
            }

            return 0;
        }
        /* ----------------------------------------------------- HMCSIM_UTIL_SET_MAX_BLOCKSIZE */
        /* 
         * HMCSIM_UTIL_SET_MAX_BLOCKSIZE
         * See Table38 in the HMC Spec : pg 58
         * 
         */
        public int hmcsim_util_set_max_blocksize(Int32 dev, UInt32 bsize)
        {
            /* vars */
            UInt64 tmp = 0x00L;
            /* ---- */

            /* 
             * sanity check 
             * 
             */


            if (dev > (this.num_devs - 1))
            {

                /* 
                 * device out of range 
                 * 
                 */

                return -1;
            }

            /* 
             * decide which values to set 
             * 
             */
            switch (bsize)
            {
                case 32:
                    tmp = 0x0000000000000000;
                    break;
                case 64:
                    tmp = 0x0000000000000001;
                    break;
                case 128:
                default:

                    tmp = 0x0000000000000002;
                    break;
            }

            /* 
             * write the register 
             * 
             */
            this.devs[dev].regs[Macros.HMC_REG_AC_IDX].reg |= tmp;

            return 0;
        }

        public int hmcsim_free()
        {
            if (this.tfile != null)
            {

                // fflush(this.tfile );
                if (tfile != null)
                    tfile.Close();
                if (fs != null)
                    fs.Close();
            }

            return hmcsim_free_memory();
        }
        public int hmcsim_free_memory()
        {
            //doing nothing in C#
            return 0;
        }

        public int hmcsim_trace_handle(ref FileStream fs)
        {
            this.fs = fs;
            if (fs == null)
                return -1;
            tfile = new StreamWriter(fs);
            return 0;
        }
        public int hmcsim_trace_header()
        {
            int major = Macros.HMC_MAJOR_VERSION;
            int minor = Macros.HMC_MINOR_VERSION;
            string text;
            DateTime time = DateTime.Now;


            if (tfile == null)
            {
                return -1;
            }

            /*
             * get the date+time combo
             *
             */
            text = time.ToUniversalTime().ToString();

            /*
             * write all the necessary sim data to the trace file
             * as a large comment block
             *
             */
            tfile.WriteLine("#---------------------------------------------------------");

            tfile.WriteLine("# HMC-SIM VERSION : " + major + "." + minor);

            tfile.WriteLine("# DATE: " + text);

            tfile.WriteLine("#---------------------------------------------------------");

            tfile.WriteLine("# HMC_NUM_DEVICES       = " + this.num_devs);

            tfile.WriteLine("# HMC_NUM_LINKS         = " + this.num_links);

            tfile.WriteLine("# HMC_NUM_QUADS         = " + this.num_quads);

            tfile.WriteLine("# HMC_NUM_VAULTS        = " + this.num_vaults);

            tfile.WriteLine("# HMC_NUM_BANKS         = " + this.num_banks);

            tfile.WriteLine("# HMC_NUM_DRAMS         = " + this.num_drams);

            tfile.WriteLine("# HMC_CAPACITY_GB       = " + this.capacity);

            tfile.WriteLine("# HMC_VAULT_QUEUE_DEPTH = " + this.queue_depth);

            tfile.WriteLine("# HMC_XBAR_QUEUE_DEPTH  = " + this.xbar_depth);

            tfile.WriteLine("#---------------------------------------------------------");

            /* print the power info */
            hmcsim_trace_power_header();

            /* print the cmc info */
            hmcsim_cmc_trace_header();

            //   fflush(this.tfile);

            return 0;

        }

        public void hmcsim_trace_power_header()
        {


            tfile.WriteLine("# LINK_PHY           = " + this.power.link_phy);
            tfile.WriteLine("# LINK_LOCAL_ROUTE   = " + this.power.link_local_route);
            tfile.WriteLine("# LINK_REMOTE_ROUTE  = " + this.power.link_remote_route);
            tfile.WriteLine("# XBAR_RQST_SLOT     = " + this.power.xbar_rqst_slot);
            tfile.WriteLine("# XBAR_RSP_SLOT      = " + this.power.xbar_rsp_slot);
            tfile.WriteLine("# XBAR_ROUTE_EXTERN  = " + this.power.xbar_route_extern);
            tfile.WriteLine("# VAULT_RQST_SLOT    = " + this.power.vault_rqst_slot);
            tfile.WriteLine("# VAULT_RSP_SLOT     = " + this.power.vault_rsp_slot);
            tfile.WriteLine("# VAULT_CTRL         = " + this.power.vault_ctrl);
            tfile.WriteLine("# ROW_ACCESS         = " + this.power.row_access);
            tfile.WriteLine("#---------------------------------------------------------");

            return;
        }
        public void hmcsim_cmc_trace_header()
        {

            /* vars */
            Int32 i = 0;
            UInt32 active = 0;
            string str = "";
            // void (*cmc_str)(char*)  = NULL;
            /* ---- */

            for (i = 0; i < Macros.HMC_MAX_CMC; i++)
            {
                active += this.cmcs[i].active;
            }

            if (active == 0)
            {
                /* nothing active, dump out */
                return;
            }

            /* print everything active */
            tfile.WriteLine("#---------------------------------------------------------");
            tfile.WriteLine("# CMC_OP:CMC_STR:RQST_LEN:RSP_LEN:RSP_CMD_CODE");
            for (i = 0; i < Macros.HMC_MAX_CMC; i++)
            {
                if (this.cmcs[i].active == 1)
                {

                    this.cmcs[i].cmc_str(ref str);
                    //(*cmc_str)(&(str[0]));

                    tfile.WriteLine("#" + this.cmcs[i].cmd + ":" + str + ":" + this.cmcs[i].rqst_len + ":" + this.cmcs[i].rsp_len + ":" + this.cmcs[i].rsp_cmd_code);
                }
            }
            tfile.WriteLine("#---------------------------------------------------------");
        }

        public int hmcsim_trace_level(UInt32 level)
        {


            this.tracelevel = level;

            return 0;
        }

        public int hmcsim_build_memrequest(uint cub, UInt64 addr, uint tag, hmc_rqst type, uint link, UInt64[] payload, ref UInt64 rqst_head, ref UInt64 rqst_tail)
        {
            /* vars */
            uint cmd = 0x00;
            uint rrp = 0x00;
            uint frp = 0x00;
            uint seq = 0x00;
            uint rtc = 0x00;
            UInt32 crc = 0x00000000;
            UInt32 flits = 0x00000000;
            UInt64 tmp = 0x00L;
            /* ---- */




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
            switch (type)
            {
                case hmc_rqst.WR16:
                    flits = 2;
                    cmd = 0x08;     /* 001000 */
                    break;
                case hmc_rqst.WR32:
                    flits = 3;
                    cmd = 0x09;     /* 001001 */
                    break;
                case hmc_rqst.WR48:
                    flits = 4;
                    cmd = 0x0A;     /* 001010 */
                    break;
                case hmc_rqst.WR64:
                    flits = 5;
                    cmd = 0x0B;     /* 001011 */
                    break;
                case hmc_rqst.WR80:
                    flits = 6;
                    cmd = 0x0C;     /* 001100 */
                    break;
                case hmc_rqst.WR96:
                    flits = 7;
                    cmd = 0x0D;     /* 001101 */
                    break;
                case hmc_rqst.WR112:
                    flits = 8;
                    cmd = 0x0E;     /* 001110 */
                    break;
                case hmc_rqst.WR128:
                    flits = 9;
                    cmd = 0x0F;     /* 001111 */
                    break;
                case hmc_rqst.WR256:
                    flits = 17;
                    cmd = 79;
                    break; ///???
                case hmc_rqst.MD_WR:
                    flits = 2;
                    cmd = 0x10;     /* 010000 */
                    break;
                case hmc_rqst.BWR:
                    flits = 2;
                    cmd = 0x11;     /* 010001 */
                    break;
                case hmc_rqst.TWOADD8:
                    flits = 2;
                    cmd = 0x12;     /* 010010 */
                    break;
                case hmc_rqst.ADD16:
                    flits = 2;
                    cmd = 0x13;     /* 010011 */
                    break;
                case hmc_rqst.P_WR16:
                    flits = 2;
                    cmd = 0x18;     /* 011000 */
                    break;
                case hmc_rqst.P_WR32:
                    flits = 3;
                    cmd = 0x19;     /* 011001 */
                    break;
                case hmc_rqst.P_WR48:
                    flits = 4;
                    cmd = 0x1A;     /* 011010 */
                    break;
                case hmc_rqst.P_WR64:
                    flits = 5;
                    cmd = 0x1B;     /* 011011 */
                    break;
                case hmc_rqst.P_WR80:
                    flits = 6;
                    cmd = 0x1C;     /* 011100 */
                    break;
                case hmc_rqst.P_WR96:
                    flits = 7;
                    cmd = 0x1D;     /* 011101 */
                    break;
                case hmc_rqst.P_WR112:
                    flits = 8;
                    cmd = 0x1E;     /* 011110 */
                    break;
                case hmc_rqst.P_WR128:
                    flits = 9;
                    cmd = 0x1F;     /* 011111 */
                    break;
                case hmc_rqst.P_WR256:
                    flits = 17;
                    cmd = 95;
                    break;
                case hmc_rqst.P_BWR:
                    flits = 2;
                    cmd = 0x21;     /* 100001 */
                    break;
                case hmc_rqst.P_2ADD8:
                    flits = 2;
                    cmd = 0x22;     /* 100010 */
                    break;
                case hmc_rqst.P_ADD16:
                    flits = 2;
                    cmd = 0x23;     /* 100011 */
                    break;
                case hmc_rqst.RD16:
                    flits = 1;
                    cmd = 0x30;     /* 110000 */
                    break;
                case hmc_rqst.RD32:
                    flits = 1;
                    cmd = 0x31;     /* 110001 */
                    break;
                case hmc_rqst.RD48:
                    flits = 1;
                    cmd = 0x32;     /* 110010 */
                    break;
                case hmc_rqst.RD64:
                    flits = 1;
                    cmd = 0x33;     /* 110011 */
                    break;
                case hmc_rqst.RD80:
                    flits = 1;
                    cmd = 0x34;     /* 110100 */
                    break;
                case hmc_rqst.RD96:
                    flits = 1;
                    cmd = 0x35;     /* 110101 */
                    break;
                case hmc_rqst.RD112:
                    flits = 1;
                    cmd = 0x36;     /* 110110 */
                    break;
                case hmc_rqst.RD128:
                    flits = 1;
                    cmd = 0x37;     /* 110111 */
                    break;
                case hmc_rqst.RD256:
                    flits = 1;
                    cmd = 119;
                    break;
                case hmc_rqst.MD_RD:
                    flits = 1;
                    cmd = 0x28;     /* 101000 */
                    break;
                case hmc_rqst.FLOW_NULL:
                    flits = 0;
                    cmd = 0x00;     /* 000000 */
                    break;
                case hmc_rqst.PRET:
                    flits = 1;
                    cmd = 0x01;     /* 000001 */
                    break;
                case hmc_rqst.TRET:
                    flits = 1;
                    cmd = 0x02;     /* 000010 */
                    break;
                case hmc_rqst.IRTRY:
                    flits = 1;
                    cmd = 0x03;     /* 000011 */
                    break;
                case hmc_rqst.TWOADDS8R:
                    flits = 2;
                    cmd = 82;
                    break;
                case hmc_rqst.ADDS16R:
                    flits = 2;
                    cmd = 83;
                    break;
                case hmc_rqst.INC8:
                    flits = 1;
                    cmd = 80;
                    break;
                case hmc_rqst.P_INC8:
                    flits = 1;
                    cmd = 84;
                    break;
                case hmc_rqst.XOR16:
                    flits = 2;
                    cmd = 64;
                    break;
                case hmc_rqst.OR16:
                    flits = 2;
                    cmd = 65;
                    break;
                case hmc_rqst.NOR16:
                    flits = 2;
                    cmd = 66;
                    break;
                case hmc_rqst.AND16:
                    flits = 2;
                    cmd = 67;
                    break;
                case hmc_rqst.NAND16:
                    flits = 2;
                    cmd = 68;
                    break;
                case hmc_rqst.CASGT8:
                    flits = 2;
                    cmd = 96;
                    break;
                case hmc_rqst.CASGT16:
                    flits = 2;
                    cmd = 98;
                    break;
                case hmc_rqst.CASLT8:
                    flits = 2;
                    cmd = 97;
                    break;
                case hmc_rqst.CASLT16:
                    flits = 2;
                    cmd = 99;
                    break;
                case hmc_rqst.CASEQ8:
                    flits = 2;
                    cmd = 100;
                    break;
                case hmc_rqst.CASZERO16:
                    flits = 2;
                    cmd = 101;
                    break;
                case hmc_rqst.EQ8:
                    flits = 2;
                    cmd = 105;
                    break;
                case hmc_rqst.EQ16:
                    flits = 2;
                    cmd = 104;
                    break;
                case hmc_rqst.BWR8R:
                    flits = 2;
                    cmd = 81;
                    break;
                case hmc_rqst.SWAP16:
                    flits = 2;
                    cmd = 106;
                    break;
                /* CMC OPS */
                case hmc_rqst.CMC04:
                case hmc_rqst.CMC05:
                case hmc_rqst.CMC06:
                case hmc_rqst.CMC07:
                case hmc_rqst.CMC20:
                case hmc_rqst.CMC21:
                case hmc_rqst.CMC22:
                case hmc_rqst.CMC23:
                case hmc_rqst.CMC32:
                case hmc_rqst.CMC36:
                case hmc_rqst.CMC37:
                case hmc_rqst.CMC38:
                case hmc_rqst.CMC39:
                case hmc_rqst.CMC41:
                case hmc_rqst.CMC42:
                case hmc_rqst.CMC43:
                case hmc_rqst.CMC44:
                case hmc_rqst.CMC45:
                case hmc_rqst.CMC46:
                case hmc_rqst.CMC47:
                case hmc_rqst.CMC56:
                case hmc_rqst.CMC57:
                case hmc_rqst.CMC58:
                case hmc_rqst.CMC59:
                case hmc_rqst.CMC60:
                case hmc_rqst.CMC61:
                case hmc_rqst.CMC62:
                case hmc_rqst.CMC63:
                case hmc_rqst.CMC69:
                case hmc_rqst.CMC70:
                case hmc_rqst.CMC71:
                case hmc_rqst.CMC72:
                case hmc_rqst.CMC73:
                case hmc_rqst.CMC74:
                case hmc_rqst.CMC75:
                case hmc_rqst.CMC76:
                case hmc_rqst.CMC77:
                case hmc_rqst.CMC78:
                case hmc_rqst.CMC85:
                case hmc_rqst.CMC86:
                case hmc_rqst.CMC87:
                case hmc_rqst.CMC88:
                case hmc_rqst.CMC89:
                case hmc_rqst.CMC90:
                case hmc_rqst.CMC91:
                case hmc_rqst.CMC92:
                case hmc_rqst.CMC93:
                case hmc_rqst.CMC94:
                case hmc_rqst.CMC102:
                case hmc_rqst.CMC103:
                case hmc_rqst.CMC107:
                case hmc_rqst.CMC108:
                case hmc_rqst.CMC109:
                case hmc_rqst.CMC110:
                case hmc_rqst.CMC111:
                case hmc_rqst.CMC112:
                case hmc_rqst.CMC113:
                case hmc_rqst.CMC114:
                case hmc_rqst.CMC115:
                case hmc_rqst.CMC116:
                case hmc_rqst.CMC117:
                case hmc_rqst.CMC118:
                case hmc_rqst.CMC120:
                case hmc_rqst.CMC121:
                case hmc_rqst.CMC122:
                case hmc_rqst.CMC123:
                case hmc_rqst.CMC124:
                case hmc_rqst.CMC125:
                case hmc_rqst.CMC126:
                case hmc_rqst.CMC127:


                    if (HMC_DEBUG)
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("HMCSIM_BUILD_MEMREQUEST : CMC PACKET TYPE ="+ type);

                    /* check for an active cmc op */
                    if (hmcsim_query_cmc(type, ref flits, ref cmd) != 0)
                    {
                        /* no active cmc op */
                        return -1;
                    }

                    /*
                     * cmc op is active
                     * flits and cmd are initialized
                     */
                    if (HMC_DEBUG)
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("HMCSIM_BUILD_MEMREQUEST : CMC PACKET COMMAND = "+ cmd);

                    break;
                default:
                    return -1;

            }

            /*
             * build the request packet header
             *
             */

            /* -- cmd field : bits 6:0 */
            tmp |= (cmd & 0x7F);

            /* -- lng field in flits : bits 11:7 */
            tmp |= ((UInt64)(flits & 0x1F) << 7);

            /* -- dln field; duplicate of lng : bits 14:11 */
            /* this is disabled in the 2.0 spec */
            //tmp |= ( (UInt64)(flits & 0xF) << 11 );

            /* -- tag field: bits 22:12 */
            tmp |= ((UInt64)(tag & 0x7FF) << 12);

            /* -- address field : bits 57:24 */
            tmp |= ((addr & 0x3FFFFFFFF) << 24);

            /* -- cube id field : bits 63:61 */
            tmp |= ((UInt64)(cub & 0x7) << 61);

            /* write the request header out */
            rqst_head = tmp;

            tmp = 0x00L;

            /*
             * build the request packet tail
             *
             */

            /* -- return retry pointer : bits 8:0 */
            rrp = hmcsim_rqst_getrrp();
            tmp |= rrp;

            /* -- forward retry pointer : bits 17:9 */
            frp = hmcsim_rqst_getfrp();
            tmp |= ((UInt64)(frp & 0x1FF) << 9);

            /* -- sequence number : bits 20:18 */
            seq = hmcsim_rqst_getseq(type);
            tmp |= ((UInt64)(seq & 0x7) << 18);

            /* -- data valid bit : bit 21 */
            tmp |= ((UInt64)(0x1 << 21));

            /* -- source source link id : bits 28:26 */
            tmp |= ((UInt64)(link & 0x7) << 26);

            /* -- error status bits : bits 28:22 */
            /* -- no errors are present */

            /* -- return token count : bits 31:29 */
            rtc = hmcsim_rqst_getrtc();
            tmp |= ((UInt64)(rtc & 0x7) << 29);

            /* -- retrieve the crc : bits 63:32 */
            crc = hmcsim_crc32(addr, payload, (2 * flits));
            tmp |= ((UInt64)(crc & 0xFFFFFFFF) << 32);

            /* write the request tail out */
            rqst_tail = tmp;

            return 0;
        }

        public UInt32 hmcsim_cmc_cmdtoidx(hmc_rqst rqst)
        {
            UInt32 i = 0;

            for (i = 0; i < Macros.HMC_MAX_CMC; i++)
            {
                if (Ctable.ctable[i].type == rqst)
                {
                    return i;
                }
            }
            return (UInt32)Macros.HMC_MAX_CMC; /* redundant, but squashes gcc warning */
        }

        public int hmcsim_query_cmc(hmc_rqst type, ref UInt32 flits, ref uint cmd)
        {
            /* vars */
            UInt32 idx = (UInt32)Macros.HMC_MAX_CMC;
            /* ---- */

            idx = hmcsim_cmc_cmdtoidx(type);

            if (HMC_DEBUG)
                if (Config.DEBUG_MEMORY) DEBUG.WriteLine("HMCSIM_QUERY_CMC: RQST_TYPE = " + type + "; IDX = " + idx);


            if (idx == Macros.HMC_MAX_CMC)
            {
                return -1;
            }
            else if (this.cmcs[(int)idx].active == 0)
            {
                if (HMC_DEBUG)
                    if (Config.DEBUG_MEMORY) DEBUG.WriteLine("ERROR : HMCSIM_QUERY_CMC: CMC OP AT IDX=" + idx + " IS INACTIVE");

                return -1;
            }

            flits = this.cmcs[(int)idx].rqst_len;
            cmd = this.cmcs[(int)idx].cmd;

            return 0;
        }
        public int hmcsim_decode_memresponse(
                        UInt64[] packet,
                        ref UInt64 response_head,
                        ref UInt64 response_tail,
                        ref hmc_response type,
                        ref uint length,
                        ref UInt16 tag,
                        ref uint rtn_tag,
                        ref uint src_link,
                        ref uint rrp,
                        ref uint frp,
                        ref uint seq,
                        ref uint dinv,
                        ref uint errstat,
                        ref uint rtc,
                        ref UInt32 crc)
        {
            /* vars */
            UInt64 tmp64 = 0x00L;
            UInt32 tmp32 = 0x00000000;
            UInt16 tmp16 = 0x00;
            uint tmp8 = 0x00;
            /* ---- */



            if (packet == null)
            {
                return -1;
            }

            /*
             * pull out and decode the packet header
             *
             */

            tmp64 = packet[0];
            response_head = tmp64;

            /*
             * shift out cmd field
             *
             */
            tmp8 = (uint)(tmp64 & 0x7F);

            switch (tmp8)
            {
                case 0x00:
                    type = hmc_response.RSP_NONE;
                    break;
                case 0x38:
                    /* 111000 */
                    type = hmc_response.RD_RS;
                    break;
                case 0x39:
                    /* 111001 */
                    type = hmc_response.WR_RS;
                    break;
                case 0x3A:
                    /* 111010 */
                    type = hmc_response.MD_RD_RS;
                    break;
                case 0x3B:
                    /* 111011 */
                    type = hmc_response.MD_WR_RS;
                    break;
                case 0x3E:
                    /* 111110 */
                    type = hmc_response.RSP_ERROR;
                    break;
                default:
                    /* ASSUME THE RESPONSE IS A CMC */
                    type = hmc_response.RSP_CMC;

                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("response type failure");
                    break;
            }

            tmp8 = 0x00;

            /*
             * packet length field
             *
             */
            tmp8 = (uint)((tmp64 >> 7) & 0x1F);

            length = tmp8;
            tmp8 = 0x00;

            /*
             * tag field
             *
             */
            tmp16 = (UInt16)((tmp64 >> 12) & 0x3FF);

            tag = tmp16;
            tmp16 = 0x0000;

            /*
             * return tag field
             * !DEPRECATED!
             */
            rtn_tag = tag;
            //tmp16	= (UInt16)( (tmp64 & 0x1FF000000) >> 24);
            //*rtn_tag = tmp16;

            /*
             * source link field
             *
             */
            tmp8 = (uint)((tmp64 >> 39) & 0x7);

            src_link = tmp8;

            /*
             * done decoding the header
             *
             */

            /*
             * decode the tail
             *
             */
            tmp64 = packet[(int)(length * 2) - 1];

            response_tail = tmp64;

            /*
             * rrp
             *
             */
            tmp8 = (uint)(tmp64 & 0xFF);

            rrp = tmp8;
            tmp8 = 0x00;

            /*
             * frp
             *
             */
            tmp8 = (uint)((tmp64 >> 9) & 0x1FF);

            frp = tmp8;
            tmp8 = 0x00;

            /*
             * sequence number
             *
             */
            tmp8 = (uint)((tmp64 >> 18) & 0x7);

            seq = tmp8;
            tmp8 = 0x00;

            /*
             * dinv
             *
             */
            tmp8 = (uint)((tmp64 >> 21) & 0x1);

            dinv = tmp8;
            tmp8 = 0x00;

            /*
             * errstat
             *
             */
            tmp8 = (uint)((tmp64 >> 22) & 0x7F);

            dinv = tmp8;
            tmp8 = 0x00;

            /*
             * rtc
             *
             */
            tmp8 = (uint)((tmp64 >> 29) & 0x7);

            rtc = tmp8;
            tmp8 = 0x00;

            /*
             * crc
             *
             */
            tmp32 = (UInt32)((tmp64 & 0xFFFFFFFF00000000) >> 32);

            crc = tmp32;
            tmp32 = 0x00000000;


            return Macros.HMC_OK;
        }
        public int hmcsim_send(UInt64[] packet)
        {
            /* vars */
            UInt64 header = 0x00L;
            UInt64 tail = 0x00L;
            UInt32 len = 0;
            UInt32 t_len = 0;
            UInt32 target = this.xbar_depth + 1;  /* -- out of bounds to check for stalls */
            UInt32 i = 0;
            UInt32 cur = 0;
            uint link = 0;
            uint cub = 0;
            hmc_queue queue = null;
            /* ---- */



            if (packet == null)
            {
                return -1;
            }

            /* 
             * pull the packet header
             * we need to know the packet length and the packet destination
             *
             */
            header = packet[0];

            if (HMC_DEBUG)
                HMCSIM_PRINT_ADDR_TRACE("PACKET HEADER", header);


            /*
             * pull the packet length and grab the tail
             *
             */
            len = (UInt32)((header >> 7) & 0x1F);
            t_len = len * 2;

            tail = packet[(int)t_len - 1];

            if (HMC_DEBUG)
            {
                HMCSIM_PRINT_ADDR_TRACE("PACKET TAIL", tail);

                HMCSIM_PRINT_INT_TRACE("PACKET T_LEN", (int)(t_len));
            }

            /*
             * grab the cub
             *
             */
            cub = (uint)((header >> 61) & 0x7);

            if (HMC_DEBUG)
                HMCSIM_PRINT_INT_TRACE("PACKET CUB", (int)(cub));


            /*
             * grab the link id
             *
             */
            link = (uint)((tail >> 26) & 0x7);

            if (HMC_DEBUG)
                HMCSIM_PRINT_INT_TRACE("PACKET LINK", (int)(link));



            /*
             * validate the cub:link
             *
             */
            if (cub > (this.num_devs + 1))
            {
                return -1;
            }
            else if (cub == this.num_devs)
            {
                return -1;
            }

            if (this.devs[(int)cub].links[(int)link].type != hmc_link_def.HMC_LINK_HOST_DEV)
            {
                /* 
                 * NOT A HOST LINK!!
                 * 
                 */

                return -1;
            }

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("FOUND A VALID PACKET STRUCTURE");


            /* 
             * Now that we have the locality details
             * of the packet request destination, 
             * go out and walk the respective link
             * xbar queues and try to push the request
             * into the first empty queue slot.
             * 
             * NOTE: this will likely need to be changed
             *       if we ever support proper ordering
             * 	 constraints on the devices
             * 
             */

            cur = this.xbar_depth - 1;
            for (i = 0; i < this.xbar_depth; i++)
            {
                if (this.devs[(int)cub].xbar[(int)link].xbar_rqst[(int)cur].valid
                    == Macros.HMC_RQST_INVALID)
                {
                    target = cur;
                }
                cur--;
            }

            if (HMC_DEBUG)
                HMCSIM_PRINT_INT_TRACE("TARGET SLOT", (int)(target));



            if (target == (this.xbar_depth + 1))
            {
                /*
                 * stall the request
                 *
                 */
                return Macros.HMC_STALL;
            }

            /* else, push the packet into the designate queue slot */
            queue = this.devs[(int)cub].xbar[(int)link].xbar_rqst[(int)target];


            hmcsim_util_zero_packet(ref queue);

            /* set the packet to valid */
            queue.valid = (uint)Macros.HMC_RQST_VALID;

            for (i = 0; i < t_len; i++)
            {
                queue.packet[i] = packet[(int)i];
            }

            /* check for posted packets */
            hmcsim_posted_rsp(packet[0]);

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("PACKET INJECTION SUCCESSFUL");


            return 0;
        }
        public int hmcsim_recv(UInt32 dev, UInt32 link, ref UInt64[] packet)
        {
            /* vars */
            UInt32 target = this.xbar_depth + 1;
            UInt32 i = 0;
            UInt32 cur = 0;
            /* ---- */



            if (dev > this.num_devs)
            {
                return -1;
            }

            if (link > this.num_links)
            {
                return -1;
            }
            if (HMC_DEBUG)
            {
                HMCSIM_PRINT_TRACE("CHECKING LINK FOR CONNECTIVITY");

                HMCSIM_PRINT_INT_TRACE("DEV", (int)(dev));

                HMCSIM_PRINT_INT_TRACE("LINK", (int)(link));
            }

            if (this.devs[(int)dev].links[(int)link].type != hmc_link_def.HMC_LINK_HOST_DEV)
            {
                /*
                 * oops, I'm not connected to this link
                 *
                 */
                return -1;
            }

            /*
             * ok, sanity check complete;
             * go walk the response queues associated
             * with the target device+link combo
             *
             * If nothing is found, return a stall signal
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("CHECKING LINK FOR VALID RESPONSE");


            cur = this.xbar_depth - 1;

            for (i = 0; i < this.xbar_depth; i++)
            {

                if (HMC_DEBUG)
                {
                    HMCSIM_PRINT_INT_TRACE("CHECKING XBAR RESPONSE QUEUE SLOT", (int)(cur));


                    //       HMCSIM_PRINT_ADDR_TRACE("xbar_rsp",
                    //                  (UInt64)(this.devs[(int)dev].xbar[(int)link].xbar_rsp));
                    //
                    //     HMCSIM_PRINT_ADDR_TRACE("xbar_rsp[cur]",
                    //                (UInt64) & (this.devs[(int)dev].xbar[(int)link].xbar_rsp[cur]));
                }

                if (this.devs[(int)dev].xbar[(int)link].xbar_rsp[(int)cur].valid == Macros.HMC_RQST_VALID)
                {
                    if (HMC_DEBUG)
                        HMCSIM_PRINT_INT_TRACE("FOUND A VALID RESPONSE PACKET AT SLOT", (int)cur);

                    target = cur;
                }

                cur--;
            }

            if (target == this.xbar_depth + 1)
            {
                /*
                 * no responses found
                 *
                 */
                return Macros.HMC_STALL;
            }

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("VALID RESPONSE FOUND");


            /* -- else, pull the response and clear the queue entry */
            for (i = 0; i < Macros.HMC_MAX_UQ_PACKET; i++)
            {
                packet[(int)i] = this.devs[(int)dev].xbar[(int)link].xbar_rsp[(int)target].packet[i];
            }

            var item = this.devs[(int)dev].xbar[(int)link].xbar_rsp[(int)target];
            hmcsim_util_zero_packet(ref item);
            this.devs[(int)dev].xbar[(int)link].xbar_rsp[(int)target] = item;

            return 0;
        }
        public int hmcsim_clock()
        {


            /*
             * Overview of the clock handler structure
             *
             * Stage 1: Walk all the child devices and drain
             * 	    their xbars
             *
             * Stage 2: Start with the root device(s) and drain
             * 	    the xbar of any messages
             *
             * Stage 3: Walk each device's vault queues and
             *          look for bank conflicts
             *
             * Stage 4: Walk each device's vault queues and
             * 	    perform the respective operations
             *
             * Stage 5: Register any necessary responses with
             * 	    the xbar
                 *
                 * Stage 6: Reorder the request queues
             *
                 * Stage 7: [optional] Print power tracing data
                 *
             * Stage 8: Update the internal clock value
             *
             */

            /*
             * Stage 1: Drain the child devices
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE1: DRAIN CHILD DEVICES");

            if (hmcsim_clock_child_xbar() != 0)
            {
                return -1;
            }

            /*
             * Stage 2: Drain the root devices
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE2: DRAIN ROOT DEVICES");

            if (hmcsim_clock_root_xbar() != 0)
            {
                return -1;
            }

            /*
             * Stage 3: Walk the vault queues and perform
             *          any integrated analysis
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE3: ANALYSIS PHASE");

            if (hmcsim_clock_analysis_phase() != 0)
            {
                return -1;
            }

            /*
             * Stage 4: Walk the vault queues and perform
             *          read and write operations
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE4: R/W OPERATIONS");

            if (hmcsim_clock_rw_ops() != 0)
            {
                return -1;
            }

            /*
             * Stage 5: Register any responses
             *          with the crossbar.
             *          This is registering responses
             * 	    from the respective vault response
             * 	    queues with a crossbar response queue
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE5: REGISTER RESPONSES");

            if (hmcsim_clock_reg_responses() != 0)
            {
                return -1;
            }

            /*
             * Stage 6: Reorder all the request queues
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE5a: REORDER THE QUEUES");

            if (hmcsim_clock_queue_reorg() != 0)
            {
                return -1;
            }

            /*
             * Stage 7: optionally print all the power tracing data
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE6: OUTPUT POWER TRACING DATA");

            if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
            {
                hmcsim_power_links();
                hmcsim_trace_power();
                if (this.power.tecplot == 1)
                {
                    hmcsim_tecplot();
                }
            }

            /*
             * Stage 8: update the clock value
             *
             */
            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("STAGE7: UPDATE THE CLOCK");

            this.clk++;

            return 0;
        }
        public int hmcsim_clock_child_xbar()
        {
            /* vars */
            UInt32 i = 0;
            UInt32 j = 0;
            UInt32 host_l = 0;
            /* ---- */

            /*
             * walk each device and interpret the links
             * if you find a HOST link, no dice.
             * otherwise, process the xbar queues
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {

                /*
                 * determine if i am connected to the host device
                 *
                 */

                if (hmcsim_util_is_root( i) == 1)
                {
                    host_l++;
                }

                /*
                 * if no host links found, process the queues
                 *
                 */
                if (host_l == 0)
                {

                    /*
                     * walk the xbar queues and process the
                     * incoming packets
                     *
                     */
                    for (j = 0; j < this.num_links; j++)
                    {


                        hmcsim_clock_process_rqst_queue( i, j);

                        hmcsim_clock_process_rsp_queue( i, j);
                    }
                }

                /*
                 * reset the host links
                 *
                 */
                host_l = 0;
            }

            return 0;
        }
        public int hmcsim_util_is_root(UInt32 dev)
        {
            /* vars */
            UInt32 is_root = 0;
            UInt32 i = 0;
            /* ---- */

            /*
             * walk the links and see if i am a root device
             * root devices have a src_cub == num_devs+1
             *
             */
            for (i = 0; i < this.num_links; i++)
            {
                if (this.devs[(int)dev].links[(int)i].src_cub == (this.num_devs + 1))
                {
                    is_root = 1;
                }
            }

            return (int)is_root;
        }
        public int hmcsim_clock_root_xbar()
        {
            /* vars */
            UInt32 i = 0;
            UInt32 j = 0;
            UInt32 k = 0;
            UInt32 host_l = 0;
            /* ---- */

            /*
             * walk each device and interpret the links
             * if you find a HOST link, process it.
             * otherwise, ignore it
             *
             */
            for (i = 0; i < this.num_devs; i++)
            {

                /*
                 * determine if i am connected to the host device
                 *
                 */

                if (hmcsim_util_is_root(i) == 1)
                {
                    host_l++;
                }

                /*
                 * if no host links found, process the queues
                 *
                 */
                if (host_l != 0)
                {


                    if (HMC_DEBUG)
                        HMCSIM_PRINT_INT_TRACE("HMCSIM_CLOCK_ROOT_XBAR:PROCESSING DEVICE", (int)(i));


                    /*
                     * walk the xbar queues and process the
                     * incoming packets
                     *
                     */
                    for (k = 0; k < this.xbar_depth; k++)
                    {
                        for (j = 0; j < this.num_links; j++)
                        {

                            hmcsim_clock_process_rqst_queue_new(i, j, k);
                        }
                    }
                }

                /*
                 * reset the host links
                 *
                 */
                host_l = 0;
            }

            return 0;
        }
        public int hmcsim_clock_analysis_phase()
        {

            /* Update bank delays */
            if (hmcsim_clock_bank_update() != 0)
            {
                return -1;
            }
            /*
             * This is where we put all the inner-clock
             * analysis phases.  The current phases
             * are as follows:
             *
             * 1) Bank conflict analysis
             * 2) TODO: cache detection analysis
             *
             */

            /*
             * 1) Bank Conflict Analysis
             *
             */
            if (false)
            {
                // temporarily removing for performance
                //   if (hmcsim_clock_bank_conflicts() != 0) {
                //       return -1;
                //   }
            }

            /*
             * 2) Cache Detection Analysis
             *
             */

            return 0;
        }
        public int hmcsim_clock_bank_update()
        {
            /* vars */
            Int32 dev = 0;
            Int32 quad = 0;
            Int32 vault = 0;
            Int32 bank = 0;
            Int32 i = 0;
            UInt32 t_slot = this.queue_depth + 1;
            UInt32 cur = this.queue_depth - 1;

            /* Iterate through all banks and decrement delay timestamp if needed */
            for (dev = 0; dev < this.num_devs; dev++)
            {
                for (quad = 0; quad < this.num_quads; quad++)
                {
                    for (vault = 0; vault < 8; vault++)
                    {
                        for (bank = 0; bank < this.num_banks; bank++)
                        {
                            if (this.devs[dev].quads[quad].vaults[vault].banks[bank].delay > 0)
                            {
                                this.devs[dev].quads[quad].vaults[vault].banks[bank].delay--;
                                //printf( "quad:vault:bank %d:%d:%d has a latency", quad, vault, bank );

                                /* If bank becomes available and a response is waiting, forward it */
                                if ((this.devs[dev].quads[quad].vaults[vault].banks[bank].delay == 0) &&
                                       (this.devs[dev].quads[quad].vaults[vault].banks[bank].valid == Macros.HMC_RQST_VALID))
                                {
                                    //( this.devs[dev].quads[quad].vaults[vault].banks[bank].valid != HMC_RQST_INVALID)) {
                                    t_slot = this.queue_depth + 1;
                                    cur = this.queue_depth - 1;
                                    for (i = 0; i < this.queue_depth; i++)
                                    {
                                        if (this.devs[dev].quads[quad].vaults[vault].rsp_queue[(int)cur].valid == Macros.HMC_RQST_INVALID)
                                        {
                                            t_slot = cur;
                                        }
                                        cur--;
                                    }

                                    /* Found slot, send response */
                                    if (t_slot != this.queue_depth + 1)
                                    {
                                        this.devs[dev].quads[quad].vaults[vault].rsp_queue[(int)t_slot].valid = Macros.HMC_RQST_VALID;

                                        for (i = 0; i < Macros.HMC_MAX_UQ_PACKET; i++)
                                        {
                                            this.devs[dev].quads[quad].vaults[vault].rsp_queue[(int)t_slot].packet[i] =
                                              this.devs[dev].quads[quad].vaults[vault].banks[bank].packet[i];
                                            this.devs[dev].quads[quad].vaults[vault].banks[bank].packet[i] = 0x00L;
                                        }
                                        if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                                        {
                                            hmcsim_power_vault_rsp_slot((uint)dev, (uint)quad,(uint) vault, t_slot);
                                        }
                                        this.devs[dev].quads[quad].vaults[vault].banks[bank].valid = Macros.HMC_RQST_INVALID;
                                        this.devs[dev].quads[quad].vaults[vault].banks[bank].delay = 0;

                                    }
                                    else { /* Did not find free slot, delay for another cycle */
                                        this.devs[dev].quads[quad].vaults[vault].banks[bank].delay = 1;
                                    }/* end else */
                                }/* end if delay==0 */
                                 /* we don't need an else case here, this was probably a flow control packet */
                            }

                        } // bank
                    } // vault
                } // quad
            } // dev

            return 0;
        }
        public int hmcsim_power_vault_rsp_slot(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 slot)
        {


            this.power.t_vault_rsp_slot += this.power.vault_rsp_slot;

            hmcsim_trace_power_vault_rsp_slot(dev, quad, vault, slot);

            if (this.num_links == 4)
            {
                this.power.H4L.vault_rsp_power[quad * vault] += this.power.vault_rsp_slot;
                this.power.H4L.vault_rsp_btu[quad * vault] += (this.power.vault_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }
            else {
                this.power.H8L.vault_rsp_power[quad * vault] += this.power.vault_rsp_slot;
                this.power.H8L.vault_rsp_btu[quad * vault] += (this.power.vault_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);
            }

            return 0;
        }
        public int hmcsim_trace_power_vault_rsp_slot(UInt32 dev, UInt32 quad, UInt32 vault, UInt32 slot)
        {


            this.tfile.WriteLine(
                       "%s%llu%s%u%s%u%s%u%s%u%s%f",
                       "HMCSIM_TRACE : ",
                       this.clk,
                       " : VAULT_RSP_SLOT_POWER : ",
                       dev,
                       ":",
                       quad,
                       ":",
                       vault,
                       ":",
                       slot,
                       ":",
                       this.power.vault_rsp_slot);

            this.tfile.WriteLine(
                       "%s%u%s%u%s%u%s%u%s%u%s%f",
           "HMCSIM_TRACE : ",
           this.clk,
           " : VAULT_RSP_SLOT_BTU : ",
           dev,
           ":",
           quad,
           ":",
           vault,
           ":",
           slot,
           ":",
           this.power.vault_rsp_slot * Macros.HMC_MILLIWATT_TO_BTU);

            return 0;
        }
        public int hmcsim_clock_rw_ops()
        {
            /* vars */
            Int32 i = 0;
            Int32 j = 0;
            Int32 k = 0;
            Int32 x = 0;
            UInt32 test = 0x00000000;
            UInt32[] venable = new UInt32[8];
            /* ---- */

            /* reset the vault enable array */
            for (i = 0; i < 8; i++)
            {
                venable[i] = 0;
            }

            for (i = 0; i < this.num_devs; i++)
            {
                for (j = 0; j < this.num_quads; j++)
                {
                    for (k = 0; k < 8; k++)
                    {
                        for (x = 0; x < this.queue_depth; x++)
                        {
                            test = 0x00000000;
                            test = this.devs[i].quads[j].vaults[k].rqst_queue[x].valid;

                            if ((test > 0) && (test != 2))
                            {
                                /*
                                 * valid and no conflict
                                 * process the request
                                 *
                                 */
                                hmcsim_process_rqst((uint)i, (uint)j, (uint)k, (uint)x);
                                venable[k] = 1;
                            }/* end if */
                        } /* end x<this.queue_depth */
                    } /* vaults */
                } /* num_quads */
            } /* num_devs */

            /* record the control power enable for each active vault */
            for (i = 0; i < 8; i++)
            {
                if (venable[i] == 1)
                {
                    if ((this.tracelevel & Macros.HMC_TRACE_POWER) > 0)
                    {
                        hmcsim_power_vault_ctrl((uint)i);
                    }
                }
            }

            if (HMC_DEBUG)
                HMCSIM_PRINT_TRACE("COMPLETED PROCESSING RW_OP");

            return 0;
        }
        public int hmcsim_jtag_reg_write(UInt32 dev, UInt64 reg, UInt64 value)
        {


            if (dev > this.num_devs)
            {
                return -1;
            }

            switch (reg)
            {
                case Macros.HMC_REG_EDR0:
                    this.devs[(int)dev].regs[Macros.HMC_REG_EDR0_IDX].reg = value;
                    break;
                case Macros.HMC_REG_EDR1:
                    this.devs[(int)dev].regs[Macros.HMC_REG_EDR1_IDX].reg = value;
                    break;
                case Macros.HMC_REG_EDR2:
                    this.devs[(int)dev].regs[Macros.HMC_REG_EDR2_IDX].reg = value;
                    break;
                case Macros.HMC_REG_EDR3:
                    this.devs[(int)dev].regs[Macros.HMC_REG_EDR3_IDX].reg = value;
                    break;
                case Macros.HMC_REG_ERR:

                    hmcsim_jtag_write_err(dev, value);
                    break;
                case Macros.HMC_REG_GC:

                    hmcsim_jtag_write_gc(dev, value);
                    break;
                case Macros.HMC_REG_LC0:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LC0_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LC1:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LC1_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LC2:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LC2_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LC3:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LC3_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LRLL0:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LRLL0_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LRLL1:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LRLL1_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LRLL2:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LRLL2_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LRLL3:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LRLL3_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LR0:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LR0_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LR1:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LR1_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LR2:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LR2_IDX].reg = value;
                    break;
                case Macros.HMC_REG_LR3:
                    this.devs[(int)dev].regs[Macros.HMC_REG_LR3_IDX].reg = value;
                    break;
                case Macros.HMC_REG_IBTC0:
                    this.devs[(int)dev].regs[Macros.HMC_REG_IBTC0_IDX].reg = value;
                    break;
                case Macros.HMC_REG_IBTC1:
                    this.devs[(int)dev].regs[Macros.HMC_REG_IBTC1_IDX].reg = value;
                    break;
                case Macros.HMC_REG_IBTC2:
                    this.devs[(int)dev].regs[Macros.HMC_REG_IBTC2_IDX].reg = value;
                    break;
                case Macros.HMC_REG_IBTC3:
                    this.devs[(int)dev].regs[Macros.HMC_REG_IBTC3_IDX].reg = value;
                    break;
                case Macros.HMC_REG_AC:
                    this.devs[(int)dev].regs[Macros.HMC_REG_AC_IDX].reg = value;
                    break;
                case Macros.HMC_REG_VCR:
                    this.devs[(int)dev].regs[Macros.HMC_REG_VCR_IDX].reg = value;
                    break;
                case Macros.HMC_REG_FEAT:
                    /*
                     * Read-Only
                     *
                     */
                    return Macros.HMC_ERROR;

                case Macros.HMC_REG_RVID:
                    /*
                     * Read-Only
                     *
                     */
                    return Macros.HMC_ERROR;

                default:
                    return -1;
            }

            return 0;
        }
        public int hmcsim_util_set_max_blocksize(UInt32 dev, UInt32 bsize)
        {
            /* vars */
            UInt64 tmp = 0x00L;
            /* ---- */

            /* 
             * sanity check 
             * 
             */


            if (dev > (this.num_devs - 1))
            {

                /* 
                 * device out of range 
                 * 
                 */

                return -1;
            }

            /* 
             * decide which values to set 
             * 
             */
            switch (bsize)
            {
                case 32:
                    tmp = 0x0000000000000000;
                    break;
                case 64:
                    tmp = 0x0000000000000001;
                    break;
                case 128:
                default:

                    tmp = 0x0000000000000002;
                    break;
            }

            /* 
             * write the register 
             * 
             */
            this.devs[(int)dev].regs[Macros.HMC_REG_AC_IDX].reg |= tmp;

            return 0;
        }

        public int getshiftamount(UInt32 num_links,  UInt32 capacity,  UInt32 bsize,  ref UInt32 shiftamt)
        {

            if (num_links == 4)
            {
                /*
                * 4 link devices
                *
                */
                if (capacity == 2)
                {
                    /*
                    * 2GB capacity
                    *
                    */
                    switch (bsize)
                    {
                        case 32:
                            shiftamt = 5;
                            break;
                        case 64:
                            shiftamt = 6;
                            break;
                        case 128:
                            shiftamt = 7;
                            break;
                        default:
                            return -1;

                    }
                }
                else if (capacity == 4)
                {
                    /*
                    * 4GB capacity
                    *
                    */
                    switch (bsize)
                    {
                        case 32:
                            shiftamt = 5;
                            break;
                        case 64:
                            shiftamt = 6;
                            break;
                        case 128:
                            shiftamt = 7;
                            break;
                        default:
                            return -1;
                           
                    }
                }
                else {
                    return -1;
                }

            }
            else if (num_links == 8)
            {
                /*
                * 8 link devices
                *
                */
                if (capacity == 4)
                {
                    /*
                    * 4GB capacity
                    *
                    */
                    switch (bsize)
                    {
                        case 32:
                            shiftamt = 5;
                            break;
                        case 64:
                            shiftamt = 6;
                            break;
                        case 128:
                            shiftamt = 7;
                            break;
                        default:
                            return -1;
                           
                    }
                }
                else if (capacity == 8)
                {
                    /*
                    * 8GB capacity
                    *
                    */
                    switch (bsize)
                    {
                        case 32:
                            shiftamt = 5;
                            break;
                        case 64:
                            shiftamt = 6;
                            break;
                        case 128:
                            shiftamt = 7;
                            break;
                        default:
                            return -1;
                            
                    }
                }
                else {
                    return -1;
                }
            }
            else {
                return -1;
            }


            return 0;
        }
        public int hmcsim_link_config(UInt32 src_dev, UInt32 dest_dev, UInt32 src_link, UInt32 dest_link, hmc_link_def type)
        {
            /* sanity check */


            if (src_dev > (this.num_devs + 1))
            {
                return -1;
            }

            if (dest_dev >= this.num_devs)
            {
                return -1;
            }
            if (src_link >= this.num_links)
            {
                return -1;
            }

            if (dest_link >= this.num_links)
            {
                return -1;
            }

            /*
             * ok, we're sane.. setup the links
             *
             */

            if (type == hmc_link_def.HMC_LINK_HOST_DEV)
            {

                /*
                 * host to device link
                 *
                 */


                this.devs[(int)dest_dev].links[(int)dest_link].src_cub = (this.num_devs + 1);
                this.devs[(int)dest_dev].links[(int)dest_link].dest_cub = dest_dev;
                this.devs[(int)dest_dev].links[(int)dest_link].type = hmc_link_def. HMC_LINK_HOST_DEV;

            }
            else {
                /*
                 * device to device link
                 * dest && src must be different; no loops
                 *
                 */

                if (dest_dev == src_dev)
                {
                    return -1;
                }

                /*
                 * config the src
                 *
                 */
                this.devs[(int)src_dev].links[(int)src_link].src_cub = src_dev;
                this.devs[(int)src_dev].links[(int)src_link].dest_cub = dest_dev;
                this.devs[(int)src_dev].links[(int)src_link].type = hmc_link_def.HMC_LINK_DEV_DEV;

                /*
                 * config the dest
                 */
                this.devs[(int)dest_dev].links[(int)dest_link].src_cub = dest_dev;
                this.devs[(int)dest_dev].links[(int)dest_link].dest_cub = src_dev;
                this.devs[(int)dest_dev].links[(int)dest_link].type = hmc_link_def.HMC_LINK_HOST_DEV;
            }

            return 0;
        }


    }




}
