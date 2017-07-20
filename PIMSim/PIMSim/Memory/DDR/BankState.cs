using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PIMSim.Memory.DDR
{
    public class BankState
    {
        public Stream dramsim_log;
        public  CurrentBankState currentBankState;
        public int openRowAddress;
        public UInt64 nextRead;
        public UInt64 nextWrite;
        public UInt64 nextActivate;
        public UInt64 nextPrecharge;
        public UInt64 nextPowerUp;
        public BusPacketType lastCommand;
        public int stateChangeCountdown;
        public BankState(Stream dramsim_log_)
        {
            dramsim_log = dramsim_log_;
            currentBankState = CurrentBankState.Idle;
            openRowAddress = 0;
            nextRead = 0;
            nextWrite = 0;
            nextActivate = 0;
            nextPrecharge = 0;
            nextPowerUp = 0;
            lastCommand = BusPacketType.READ;
            stateChangeCountdown = 0;
        }
        public void print()
        {

        }

    }
    public enum CurrentBankState
    {
        Idle,
        RowActive,
        Precharging,
        Refreshing,
        PowerDown
    };
}
