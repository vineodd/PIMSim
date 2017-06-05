using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.Procs;
using SimplePIM.Configs;
using SimplePIM.PIM;

namespace SimplePIM.Memory.DDR
{

    public class Transaction
    {
        public Transaction() { }
        public TransactionType transactionType;
        public UInt64 address;
        public UInt64 data;
        public UInt64 timeAdded;
        public UInt64 timeReturned;
        public UInt64 block_addr;
        public int pid;
        public bool pim;
        
        // friend ostream &operator<<(ostream &os, const Transaction &t);
        //functions
        public Transaction(TransactionType transType, UInt64 addr, UInt64 dat, UInt64 block, int pid_,bool pim_)
        {

            block_addr = block;
            transactionType = transType;
            address = addr;
            data = dat;
            pid = pid_;
            pim = pim_;
        }
        public Transaction(Transaction t)
        {
   
            transactionType = t.transactionType;
            address = t.address;
            data = 0;
            timeAdded = t.timeAdded;
            timeReturned = t.timeReturned;
            pid = t.pid;
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
                        Console.WriteLine("ERROR: Unknown row buffer policy");
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
                        Console.WriteLine("ERROR: Unknown row buffer policy");
                        Environment.Exit(1);
                    }
                    break;
                default:
                    Console.WriteLine("ERROR: This transaction type doesn't have a corresponding bus packet type");
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
        public List<Proc> proc;
        public TransactionReceiver(ref List<Proc> proc_)
        {
            proc = proc_;

        }
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

                Console.WriteLine("ERROR  This should never happen");

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
        public void read_complete(uint id, CallBackInfo callback)
        {

            int it = find(pendingReadRequests, callback.address);
            if (it == pendingReadRequests.Count())
            {
                Console.WriteLine("ERROR: Cant find a pending read for this one");
                Environment.Exit(1);
            }
            else
            {
                if (pendingReadRequests.ElementAt(it).Value.Count() == 0)
                {
                    Console.WriteLine("ERROR:Nothing here, either");
                    Environment.Exit(1);
                }
            }

            UInt64 added_cycle = pendingReadRequests[callback.address].First();
            UInt64 latency = callback.done_cycle - added_cycle;

            //   for (int i = 0; i < proc.Count(); i++)
            {
                if (!callback.pim)
                {
                    (callback.getsource as Proc).read_callback(callback.block_addr, callback.address);
                }
                else
                {
                    if (Config.pim_config.unit_type == PIM_Unit_Type.Processors)
                    {
                        (callback.getsource as PIMProc).read_callback(callback.block_addr, callback.address);
                    }
                    else
                    {
                        (callback.getsource as ComputationalUnit).read_callback(callback.address);
                    }
                }
            }
            

            if (Coherence.consistency == Consistency.SpinLock)
            {
                if (callback.pim)
                {
                    Coherence.spin_lock.relese_lock(callback.address);
                }
            }

            pendingReadRequests[callback.address].RemoveAt(0);
            if (pendingReadRequests[callback.address].Count() == 0)
                pendingReadRequests.Remove(callback.address);
            Console.WriteLine("Read Callback:  0x" + callback.address.ToString("X") + "Block_addr=0x"+ callback.block_addr +"  latency=" + latency + "cycles (" + callback.done_cycle + "->" + added_cycle + ")");

            
        }
       public void write_complete(uint id, UInt64 address, UInt64 block_addr, UInt64 done_cycle,bool pim_)
        {

            int it = find(pendingWriteRequests, address);
            if (it == pendingWriteRequests.Count())
            {
                Console.WriteLine("ERROR  : Cant find a pending read for this one");
                Environment.Exit(1);
            }
            else
            {
                if (pendingWriteRequests.ElementAt(it).Value.Count() == 0)
                {
                    Console.WriteLine("ERROR  : Nothing here, either");
                    Environment.Exit(1);
                }
            }

            UInt64 added_cycle = pendingWriteRequests[address].First();
            UInt64 latency = done_cycle - added_cycle;

            //   for (int i = 0; i < proc.Count(); i++)

            proc[(int)id].write_callback(block_addr,address);
           if (Coherence. consistency == Consistency.SpinLock)
            {
                if (pim_)
                {
                    Coherence.spin_lock.relese_lock(address);
                }
            }
           
          
            pendingWriteRequests[address].RemoveAt(0);
            if (pendingWriteRequests[address].Count() == 0)
                pendingWriteRequests.Remove(address);
            Console.WriteLine("Write Callback: 0x" + address.ToString("X") + " latency=" + latency + "cycles (" + done_cycle + "->" + added_cycle + ")");
        }
    }
}
