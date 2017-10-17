using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Procs;
using PIMSim.Configs;
using PIMSim.PIM;
using PIMSim.Statistics;


namespace PIMSim.Memory.DDR
{

    public class Transaction
    {
        public Transaction() { }
        public TransactionType transactionType;
        public UInt64 address;
        public UInt64 data;
        public UInt64 timeAdded;
        public UInt64 timeReturned;

        public CallBackInfo callback = new CallBackInfo();
        
        // friend ostream &operator<<(ostream &os, const Transaction &t);
        //functions
        public Transaction(TransactionType transType,UInt64 addr, UInt64 dat,CallBackInfo call_)// UInt64 addr, UInt64 dat, UInt64 block, int pid_,bool pim_)
        {
            callback = call_;
            data = dat;
           // block_addr = block;
            transactionType = transType;
            address = call_.address;
            address = addr;

            //pid = pid_;
            //pim = pim_;
        }
        public Transaction(Transaction t)
        {
   
            transactionType = t.transactionType;
            callback = t.callback;
            //  address = t.address;
               data = t.data;
            address = t.address;
            timeAdded = t.timeAdded;
            timeReturned = t.timeReturned;
          //  pid = t.pid;
        }

        public BusPacketType getBusPacketType()
        {
            switch (transactionType)
            {
                case TransactionType.DATA_READ:
                    if (Config.dram_config. rowBufferPolicy == RowBufferPolicy. ClosePage)
                    {
                        return BusPacketType.READ_P;
                    }
                    else if (Config.dram_config.rowBufferPolicy == RowBufferPolicy.OpenPage)
                    {
                        return BusPacketType.READ;
                    }
                    else
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR: Unknown row buffer policy");
                        Environment.Exit(1);
                    }
                    break;
                case TransactionType.DATA_WRITE:
                    if (Config.dram_config.rowBufferPolicy == RowBufferPolicy.ClosePage)
                    {
                        return BusPacketType.WRITE_P;
                    }
                    else if (Config.dram_config.rowBufferPolicy == RowBufferPolicy.OpenPage)
                    {
                        return BusPacketType.WRITE;
                    }
                    else
                    {
                        if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR: Unknown row buffer policy");
                        Environment.Exit(1);
                    }
                    break;
                default:
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR: This transaction type doesn't have a corresponding bus packet type");
                    Environment.Exit(1);
                    break;
            }
            Environment.Exit(1);
            return BusPacketType.DATA;
        }
    }
    public enum TransactionType
    {
        DATA_READ,
        DATA_WRITE,
        RETURN_DATA

    }

    public class TransactionReceiver
    {
        public SortedDictionary<UInt64, List<UInt64>> pendingReadRequests = new SortedDictionary<ulong, List<ulong>>();
        public SortedDictionary<UInt64, List<UInt64>> pendingWriteRequests = new SortedDictionary<ulong, List<ulong>>();


        public void add_pending(Transaction t, UInt64 cycle)
        {
            // C++ lists are ordered, so the list will always push to the back and
            // remove at the front to ensure ordering
            if (t.transactionType == TransactionType.DATA_READ)
            {
                //pendingReadRequests[t.address].Add(cycle);
                
                    if (pendingReadRequests.ContainsKey(t.address))
                    {
                        pendingReadRequests[t.address].Add(cycle);
                    }
                    else
                    {
                        pendingReadRequests.Add(t.address, new List<ulong>());
                        pendingReadRequests[t.address].Add(cycle);
                    }
                
            }
            else if (t.transactionType == TransactionType.DATA_WRITE)
            {
                //pendingWriteRequests[t.address].Add(cycle);
                
                    if (pendingWriteRequests.ContainsKey(t.address))
                    {
                        pendingWriteRequests[t.address].Add(cycle);
                    }
                    else
                    {
                        pendingWriteRequests.Add(t.address, new List<ulong>());
                        pendingWriteRequests[t.address].Add(cycle);
                    }
                
            }
            else
            {

                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR  This should never happen");

                Environment.Exit(1);
            }
        }
        public int find(SortedDictionary<UInt64, List<UInt64>> dir, UInt64 add)
        {
            int i = 0;
            foreach (var item in dir)
            {
                if (item.Key == add)
                {
                    return i;
                }
                i++;
            }
            return i;
        }

        // public void read_complete(uint id, UInt64 address,UInt64 block_addr, UInt64 done_cycle,bool pim_)
        public void read_complete(uint id, UInt64 addr, UInt64 done_cycle, CallBackInfo callback)
        {

            int it = find(pendingReadRequests, addr);
            if (it == pendingReadRequests.Count())
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR: Cant find a pending read for this one");
                Environment.Exit(1);
            }
            else
            {
                if (pendingReadRequests.ElementAt(it).Value.Count() == 0)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR:Nothing here, either");
                    Environment.Exit(1);
                }
            }

            UInt64 added_cycle = pendingReadRequests[addr].First();
            UInt64 latency = done_cycle - added_cycle;

            //   for (int i = 0; i < proc.Count(); i++)
            if (callback.load)
            {
                foreach (var pimunit in (callback.getsource() as List<ComputationalUnit>))
                {
                    pimunit.read_callback(callback);
                }
                goto endr;
            }
            if (!callback.pim)
            {
                foreach (var proc in (callback.getsource() as List<Proc>))
                {
                    proc.read_callback(callback);
                }
                goto endr;
            }
            else
            {
                if (PIMConfigs.unit_type == PIM_Unit_Type.Processors)
                {
                    foreach (var pimproc in (callback.getsource() as List<ComputationalUnit>))
                    {
                        (pimproc as PIMProc).read_callback(callback);
                    }
                    goto endr;

                }

                ///CU
                foreach (var pimunit in (callback.getsource() as List<ComputationalUnit>))
                {
                    pimunit.read_callback(callback);
                }
            }



            endr:

            //if (Coherence.consistency == Consistency.SpinLock)
            //{
            //    if (callback.pim)
            //    {
            //        Coherence.spin_lock.relese_lock(callback.address);
            //    }
            //}

            pendingReadRequests[addr].RemoveAt(0);
            if (pendingReadRequests[addr].Count() == 0)
                pendingReadRequests.Remove(addr);
            if(Config.DEBUG_MEMORY)DEBUG.WriteLine("Read Callback:  0x" + addr.ToString("X") + "Block_addr=0x" + callback.block_addr + "  latency=" + latency + "cycles (" + done_cycle + "->" + added_cycle + ")");


        }
       public void write_complete(uint id, UInt64 addr, UInt64 done_cycle, CallBackInfo callback)//UInt64 address, UInt64 block_addr, UInt64 done_cycle,bool pim_)
        {

            int it = find(pendingWriteRequests, addr);
            if (it == pendingWriteRequests.Count())
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR  : Cant find a pending read for this one");
                Environment.Exit(1);
            }
            else
            {
                if (pendingWriteRequests.ElementAt(it).Value.Count() == 0)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("ERROR  : Nothing here, either");
                    Environment.Exit(1);
                }
            }

            UInt64 added_cycle = pendingWriteRequests[addr].First();
            UInt64 latency = done_cycle - added_cycle;

            //   for (int i = 0; i < proc.Count(); i++)

            if (callback.store)
            {
                foreach (var pimunit in (callback.getsource() as List<ComputationalUnit>))
                {
                    pimunit.write_callback(callback);
                }
                goto endw;
            }
            // proc[(int)id].write_callback(callback.block_addr, callback.address);
            if (callback.flush)
            {
                Coherence.flush_queue.Remove(callback.block_addr);
                DEBUG.WriteLine("-- Flushed data : [" + callback.block_addr + "] [" + callback.address + "]");
                goto endw;
            }
            else
            {
                if (!callback.pim)
                {
                    foreach (var proc in (callback.getsource() as List<Proc>))
                    {
                        proc.write_callback(callback);
                    }
                    goto endw;
                }
                else
                {
                    if (PIMConfigs.unit_type == PIM_Unit_Type.Processors)
                    {
                        foreach (var pimproc in (callback.getsource() as List<ComputationalUnit>))
                        {
                            (pimproc as PIMProc).write_callback(callback);
                        }

                        goto endw;
                    }
                    else
                    {
                        foreach (var pimunit in (callback.getsource() as List<ComputationalUnit>))
                        {
                            pimunit.write_callback(callback);
                        }

                    }
                }
            }

            //if (Coherence. consistency == Consistency.SpinLock)
            // {
            //     if (callback.pim)
            //     {
            //         Coherence.spin_lock.relese_lock(callback.address);
            //     }
            // }

            endw:
            pendingWriteRequests[addr].RemoveAt(0);
            if (pendingWriteRequests[addr].Count() == 0)
                pendingWriteRequests.Remove(addr);
            if(Config.DEBUG_MEMORY)DEBUG.WriteLine("Write Callback: 0x" + addr.ToString("X") + " latency=" + latency + "cycles (" + done_cycle + "->" + added_cycle + ")");
        }
    }
}
