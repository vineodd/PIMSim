using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SimplePIM.Memory;
using SimplePIM.Procs;
using SimplePIM.Configs;

namespace SimplePIM.Memory.DDR
{
    public class DDRMem : MemObject
    {
        public TransactionReceiver transactionReceiver;
        public string traceFileName = "k6_aoe_02_short.trc";
        public string pwdString = "";
        public uint megsOfMemory = 2048;
        public bool useClockCycle = true;
        public Dictionary<string, string> paramOverrides = null;
        public UInt64 numCycles = 1000;
        public MultiChannelMemorySystem memorySystem;
        public Callback_t read_cb;
        public Callback_t write_cb;
        public Queue<MemRequest> TransationQueue;
        public UInt64 clockCycle = 0;
        public List<Proc> proc;
        public List<Mctrl> mctrl;
        TransactionType transType = TransactionType.RETURN_DATA;
        UInt64 data = 0;
        Transaction trans = null;
        bool pendingTrans = false;
        UInt64 addr = 0;
  
        public override void attach_mctrl(ref Mctrl mctrl_)
        {
            mctrl .Add( mctrl_);
        }
        public DDRMem(ref List<Proc> proc_,int pid_)
        {
            this.pid = pid_;
            mctrl = new List<Mctrl>();
            proc = proc_;
            transactionReceiver = new TransactionReceiver(ref proc);
           
            memorySystem = new MultiChannelMemorySystem(Config.dram_config. systemIniFilename, pwdString, traceFileName, megsOfMemory,paramOverrides);
            memorySystem.setCPUClockSpeed(0);
            if (Config.dram_config.RETURN_TRANSACTIONS)
            {
                // transactionReceiver=new TransactionReceiver();
                /* create and register our callback functions */
                read_cb = new Callback_t(transactionReceiver.read_complete);
                // new Callback<TransactionReceiver, void, unsigned, uint64_t, uint64_t>(&transactionReceiver, &TransactionReceiver::read_complete);
                write_cb = new Callback_t(transactionReceiver.write_complete);//
                                                                              //  new Callback<TransactionReceiver, void, unsigned, uint64_t, uint64_t>(&transactionReceiver, &TransactionReceiver::write_complete);
                memorySystem.RegisterCallbacks(read_cb, write_cb, null);
            }
            TransationQueue = new Queue<MemRequest>();

        }
        public override bool addTransation(MemRequest req_)
        {
            //if()
            this.TransationQueue.Enqueue(req_);
            return false;
        }
        public void alignTransactionAddress(ref Transaction trans)
        {
            // zero out the low order bits which correspond to the size of a transaction

            int throwAwayBits = (int)Config.dram_config.THROW_AWAY_BITS;

            trans.address >>= throwAwayBits;
            trans.address <<= throwAwayBits;
        }
        public override void Step()
        {
            cycle++;
            MemRequest req_ = new MemRequest();
            foreach (var m in mctrl)
            {
                while (m.get_req(this.pid, ref req_))
                {
                    TransationQueue.Enqueue(req_);
                }
            }

            if (!pendingTrans)
            {
                if (TransationQueue.Count() > 0)
                {
                    MemRequest req = TransationQueue.Peek();
                    switch (req.memtype)
                    {
                        case MemReqType.READ:
                            transType = TransactionType.DATA_READ;
                            break;
                        case MemReqType.WRITE:
                            transType = TransactionType.DATA_WRITE;
                            break;
                        case MemReqType.RETURN_DATA:
                            transType = TransactionType.RETURN_DATA;
                            break;
                        default:
                            transType = TransactionType.RETURN_DATA;
                            break;


                    }
                    addr = req.address;
                    data = req.data;
                    clockCycle = req.cycle;
                    TransationQueue.Dequeue();
                    if (transType != TransactionType.DATA_READ && transType != TransactionType.DATA_WRITE)
                        return;
                    trans = new Transaction(transType, addr, data,req.block_addr,req.pid,req.pim);
                    Console.WriteLine("ADD transaction: addr=" + addr.ToString("X"));


                    alignTransactionAddress(ref trans);
                    if (cycle >= clockCycle)
                    {
                        if (!memorySystem.addTransaction(ref trans))
                        {
                            pendingTrans = true;
                        }
                        else
                        {
                            if (Config.dram_config.RETURN_TRANSACTIONS)
                                transactionReceiver.add_pending(trans, cycle);
                            //#endif
                            // the memory system accepted our request so now it takes ownership of it
                            trans = null;
                        }
                    }
                    else
                    {
                        pendingTrans = true;
                    }
                }


            }
            else
            {
                if (pendingTrans && cycle >= clockCycle)
                {
                    pendingTrans = !memorySystem.addTransaction(ref trans);
                    if (!pendingTrans)
                    {
                        //#ifdef RETURN_TRANSACTIONS
                        transactionReceiver.add_pending(trans, cycle);
                        //#endif
                        trans = null;
                    }
                }
            }

            memorySystem.update();

        }

        public override int get_lock_index(ulong addr)
        {
            Transaction trans = new Transaction();
            trans.address = addr;
            alignTransactionAddress(ref trans);
            uint channelNumber =memorySystem. findChannelNumber(trans.address);
            int ch = 0, ra = 0, ba = 0, ro=0, co = 0;
            memorySystem.channels[(int)channelNumber].memoryController.addressMapping(addr, ref ch, ref ra, ref ba, ref ro, ref co);
            return ba;
        }
    }
}
