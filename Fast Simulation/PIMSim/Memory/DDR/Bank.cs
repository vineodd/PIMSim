using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using PIMSim.Configs;
using PIMSim.Statistics;

namespace PIMSim.Memory.DDR
{
    public class Bank
    {
        private List<DataStruct> rowEntries;
        private Stream dramsim_log;
        public BankState currentState;

        public Bank(Stream dramsim_log_)
        {
            dramsim_log = dramsim_log_;

        }
        public void read(ref BusPacket busPacket)
        {
            DataStruct rowHeadNode = rowEntries[(int)busPacket.column];
            DataStruct foundNode = null;
            if ((foundNode = searchForRow((int)busPacket.row, rowHeadNode)) == null)
            {

                // the row hasn't been written before, so it isn't in the list
                UInt64 garbage = (Config.dram_config. BL * (Config.dram_config.JEDEC_DATA_BUS_BITS / 8));
                busPacket.data = garbage;
            }
            else
            {
                // found it
                busPacket.data = foundNode.data;
            }
            //the return packet should be a data packet, not a read packet
            busPacket.busPacketType = BusPacketType.DATA;
        }
        public void write(ref BusPacket busPacket)
        {
            //TODO: move all the error checking to BusPacket so once we have a bus packet,
            //			we know the fields are all legal

            if (busPacket.column >= Config.dram_config.NUM_COLS)
            {
                if(Config.DEBUG_MEMORY)DEBUG.WriteLine("== Error - Bus Packet column " + busPacket.column + " out of bounds");
                Environment.Exit(-1);
            }
            // head of the list we need to search
            DataStruct rowHeadNode = rowEntries[(int)busPacket.column];
            DataStruct foundNode = null;

            if ((foundNode = searchForRow((int)busPacket.row, rowHeadNode)) == null)
            {
                //not found
                DataStruct newRowNode = new DataStruct();

                //insert at the head for speed
                //TODO: Optimize this data structure for speedier lookups?
                newRowNode.row = (int)busPacket.row;
                newRowNode.data = busPacket.data;
            
                rowEntries[(int)busPacket.column].AddFirst(newRowNode);
            }
            else
            {
                // found it, just plaster in the new data
                foundNode.data = busPacket.data;
                if (Config.dram_config.DEBUG_BANKS)
                {
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine(" -- Bank " + busPacket.bank + " writing to physical address 0x" + busPacket.physicalAddress.ToString("x") + ":");
                    busPacket.printData();
                    if(Config.DEBUG_MEMORY)DEBUG.WriteLine("");
                }
            }
        }
        public DataStruct searchForRow(int row, DataStruct head)
        {

            for (int i = 0; i < head.Count; i++)
            {
                if (head.row == row)
                {
                    //found it
                    return head;
                }

            }
            //if we get here, didn't find it
            return null;
        }
    }
    public class DataStruct : LinkedList<DataStruct>
    {
        public int row;
        public UInt64 data;
    }
}
