using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Memory;
using SimplePIM.Procs;
using SimplePIM.Configs;
using System.IO;
using SimplePIM.PIM;

namespace SimplePIM.Memory.HMC
{
    public class HMCMem : MemObject
    {
        public List<Mctrl> mctrl;
        public List<Proc> proc;
        public Queue<MemRequest> TransationQueue;

        public HMCSim hmc;
        public int current_statue = Macros.HMC_OK;
        public FileStream fs;
        public uint tag = 0;
        public List<Tuple<UInt64, List<int>, UInt64, UInt64,bool>> callback = new List<Tuple<ulong, List<int>, ulong, ulong,bool>>();
        public override bool addTransation(MemRequest req_)
        {
            this.TransationQueue.Enqueue(req_);
            return false;
        }

        public override void attach_mctrl(ref Mctrl mctrl_)
        {
            mctrl.Add(  mctrl_);
        }
        public HMCMem(ref List<Proc> proc_)
        {

            proc = proc_;
            mctrl = new List<Mctrl>();
            hmc = new HMCSim();
            hmc.hmcsim_init(Config.hmc_config.num_devs, Config.hmc_config.num_links,
                               Config.hmc_config.num_vaults, Config.hmc_config.queue_depth,
                               Config.hmc_config.num_banks, Config.hmc_config.num_drams,
                               Config.hmc_config.capacity, Config.hmc_config.xbar_depth);
            if (Config.hmc_config.num_devs > 1)
            {

                /* -- TODO */

            }
            else
            {
                /*
                * single device, connect everyone
                *
                */
                for (int i = 0; i < Config.hmc_config.num_links; i++)
                {

                    current_statue = hmc.hmcsim_link_config(
                          (Config.hmc_config.num_devs + 1),
                          0,
                        (uint)i,
                        (uint)i,
                       hmc_link_def.HMC_LINK_HOST_DEV);

                    if (current_statue != 0)
                    {
                        Console.WriteLine("ERROR : ");
                        Environment.Exit(1);
                    }
                    else {
                        Console.WriteLine("SUCCESS : INITIALIZED LINK %d\n", i);
                    }
                }
            }
            hmc.hmcsim_util_set_all_max_blocksize(Config.hmc_config.bsize);
            fs = new FileStream("out.txt", FileMode.OpenOrCreate);
            hmc.hmcsim_trace_handle(ref fs);
            hmc.hmcsim_trace_level((Macros.HMC_TRACE_BANK |
         Macros.HMC_TRACE_QUEUE |
        Macros.HMC_TRACE_CMD |
        Macros.HMC_TRACE_STALL |
        Macros.HMC_TRACE_LATENCY));
            TransationQueue = new Queue<MemRequest>();
        }
        ~HMCMem()
        {
            fs.Close();
        }
        public override void Step()
        {
            cycle++;
            current_statue = Macros.HMC_OK;
            MemRequest req_ = new MemRequest();
            foreach (var m in mctrl)
            {
                while (m.get_req(this.pid, ref req_))
                {
                    TransationQueue.Enqueue(req_);
                }
            }
            if (TransationQueue.Count() > 0)
            {
                MemRequest req = TransationQueue.Peek();
                UInt64[] packet = new UInt64[Macros.HMC_MAX_UQ_PACKET];
                hmc_rqst type = hmc_rqst.RD64;
                UInt64 d_response_head = 0;
                UInt64 d_response_tail = 0;
                hmc_response d_type = hmc_response.MD_RD_RS;
                uint d_length = 0;
                UInt16 d_tag = 0;
                uint d_rtn_tag = 0;
                uint d_src_link = 0;
                uint d_rrp = 0;
                uint d_frp = 0;
                uint d_seq = 0;
                uint d_dinv = 0;
                uint d_errstat = 0;
                uint d_rtc = 0;
                UInt32 d_crc = 0;
                zero_packet(ref packet);

                switch (req.memtype)
                {
                    case MemReqType.READ:
                        type = hmc_rqst.RD64;
                        break;
                    case MemReqType.WRITE:
                        type = hmc_rqst.WR64;
                        break;
                    case MemReqType.RETURN_DATA:


                    default:

                        break;


                }
                if (current_statue != Macros.HMC_STALL)
                {
                    uint cub = 0;

                    uint link = 0;
                    ulong[] payload = { 0x00L, 0x00L, 0x00L, 0x00L, 0x00L, 0x00L, 0x00L, 0x00L };
                    UInt64 head = 0x00L;
                    UInt64 tail = 0x00L;
                    hmc.hmcsim_build_memrequest(
                     cub,
                     req.address,
                     tag,
                   type,
                     link,
                     payload,
                     ref head,
                     ref tail);
                    /*
                    * read packets have:
                    * head +
                    * tail
                    *
                    */
                    tag++;
                    if (type == hmc_rqst.RD64)
                    {
                        packet[0] = head;
                        packet[1] = tail;
                    }
                    if (type == hmc_rqst.WR64)
                    {
                        packet[0] = head;
                        packet[1] = 0x05L;
                        packet[2] = 0x06L;
                        packet[3] = 0x07L;
                        packet[4] = 0x08L;
                        packet[5] = 0x09L;
                        packet[6] = 0x0AL;
                        packet[7] = 0x0BL;
                        packet[8] = 0x0CL;
                        packet[9] = tail;
                    }
                    current_statue = hmc.hmcsim_send(packet);
                    if (current_statue == 0)
                    {
                        callback.Add(new Tuple<ulong, List<int>, ulong, ulong,bool>(tag - 1, req.pid, req.block_addr, req.address,req.pim));
                        TransationQueue.Dequeue();
                        current_statue = Macros.HMC_OK;
                        while (current_statue != Macros.HMC_STALL)
                        {
                            int stall_sig = 0;
                            for (int z = 0; z < hmc.num_links; z++)
                            {

                                int res = hmc.hmcsim_recv(cub, (uint)z, ref packet);

                                if (res == Macros.HMC_STALL)
                                {
                                    stall_sig++;
                                }
                                else {
                                    /* successfully received a packet */
                                    Console.WriteLine("SUCCESS : RECEIVED A SUCCESSFUL PACKET RESPONSE");
                                    hmc.hmcsim_decode_memresponse(
                                        packet,
                                        ref d_response_head,
                                        ref d_response_tail,
                                        ref d_type,
                                        ref d_length,
                                        ref d_tag,
                                        ref d_rtn_tag,
                                        ref d_src_link,
                                        ref d_rrp,
                                        ref d_frp,
                                        ref d_seq,
                                        ref d_dinv,
                                        ref d_errstat,
                                        ref d_rtc,
                                        ref d_crc);
                                    Console.WriteLine("RECV tag={0}; rtn_tag={1}", d_tag, d_rtn_tag);
                                    // all_recv++;
                                    var item = callback.FindIndex(s => s.Item1 == d_tag);
                                    if (item < 0)
                                    {
                                        //error
                                        Console.WriteLine("");

                                    }

                                    if (d_type == hmc_response.RD_RS)
                                    {
                                        foreach (var id in callback[item].Item2)
                                        {
                                            proc[id].read_callback(callback[item].Item3, callback[item].Item4);
                                        }
                                    }
                                    else
                                    {
                                        if (d_type == hmc_response.WR_RS)
                                        {
                                            foreach (var id in callback[item].Item2)
                                            {
                                                proc[id].write_callback(callback[item].Item3, callback[item].Item4);
                                            }
                                        }
                                        else
                                        {
                                            //error
                                            Environment.Exit(0);
                                        }
                                    }
                                    if (Coherence.consistency == Consistency.SpinLock)
                                    {
                                        if (callback[item].Item5)
                                        {
                                            Coherence.spin_lock.relese_lock(callback[item].Item4);
                                        }
                                    }

                                    callback.RemoveAt(item);
                                }

                                /*
                                * zero the packet
                                *
                                */
                                zero_packet(ref packet);
                            }

                            if (stall_sig == hmc.num_links)
                            {
                                /*
                                * if all links returned stalls,
                                * then we're done receiving packets
                                *
                                */

                                Console.WriteLine("STALLED : STALLED IN RECEIVING\n");
                                current_statue = Macros.HMC_STALL;

                            }

                            stall_sig = 0;


                        }
                    }
                    else
                    {
                        //   hmc.hmcsim_clock();
                    }
                    hmc.hmcsim_clock();
                }
            }
            else
            {

                uint cub = 0;
                UInt64[] packet = new UInt64[Macros.HMC_MAX_UQ_PACKET];
                uint d_length = 0;
                UInt16 d_tag = 0;
                uint d_rtn_tag = 0;
                uint d_src_link = 0;
                uint d_rrp = 0;
                uint d_frp = 0;
                uint d_seq = 0;
                uint d_dinv = 0;
                uint d_errstat = 0;
                uint d_rtc = 0;
                UInt32 d_crc = 0;
                UInt64 d_response_head = 0;
                UInt64 d_response_tail = 0;
                hmc_response d_type = hmc_response.MD_RD_RS;
                zero_packet(ref packet);
                for (int z = 0; z < hmc.num_links; z++)
                {

                    int res = hmc.hmcsim_recv(cub, (uint)z, ref packet);

                    if (res == Macros.HMC_STALL)
                    {
                        //stall_sig++;
                    }
                    else {
                        /* successfully received a packet */
                        Console.WriteLine("SUCCESS : RECEIVED A SUCCESSFUL PACKET RESPONSE");
                        hmc.hmcsim_decode_memresponse(
                            packet,
                            ref d_response_head,
                            ref d_response_tail,
                            ref d_type,
                            ref d_length,
                            ref d_tag,
                            ref d_rtn_tag,
                            ref d_src_link,
                            ref d_rrp,
                            ref d_frp,
                            ref d_seq,
                            ref d_dinv,
                            ref d_errstat,
                            ref d_rtc,
                            ref d_crc);
                        Console.WriteLine("RECV tag={0}; rtn_tag={1}", d_tag, d_rtn_tag);



                        var item = callback.FindIndex(s => s.Item1 == d_tag);
                        if (item < 0)
                        {
                            //error
                            Console.WriteLine("");

                        }

                        if (d_type == hmc_response.RD_RS)
                        {
                            foreach (var id in callback[item].Item2)
                            {
                                proc[id].read_callback(callback[item].Item3, callback[item].Item4);
                            }
                        }
                        else
                        {
                            if (d_type == hmc_response.WR_RS)
                            {
                                foreach (var id in callback[item].Item2)
                                {
                                    proc[id].write_callback(callback[item].Item3, callback[item].Item4);
                                }
                            }
                            else
                            {
                                //error
                                Environment.Exit(0);
                            }
                        }
                        if (Coherence.consistency == Consistency.SpinLock)
                        {
                            if (callback[item].Item5)
                            {
                                Coherence.spin_lock.relese_lock(callback[item].Item4);
                            }
                        }
                        callback.RemoveAt(item);
                        // all_recv++;
                    }

                    /*
                    * zero the packet
                    *
                    */
                    zero_packet(ref packet);
                }
                hmc.hmcsim_clock();
            }
        }

        public void zero_packet(ref UInt64[] packet)
        {
            int i = 0;

            /*
            * zero the packet
            *
            */
            for (i = 0; i < Macros.HMC_MAX_UQ_PACKET; i++)
            {
                packet[i] = 0x00L;
            }


            return;
        }

        public override int get_lock_index(ulong addr)
        {
            //  var_ addr = ((head >> 24) & 0x1FFFFFFFF);

            //   /* -- block size */
            //hmc.   hmcsim_util_get_max_blocksize(dev, ref bsize);

            //   /* -- get the bank */
            //   hmc.hmcsim_util_decode_bank(dev, bsize, addr, ref bank);
            return 1;
        }
    }
}
