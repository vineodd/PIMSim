using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.General.Protocols
{
    public enum CMD
    {
        InvalidCmd,
        ReadReq,
        ReadResp,
        ReadRespWithInvalidate,
        WriteReq,
        WriteResp,
        WritebackDirty,
        WritebackClean,
        CleanEvict,
        SoftPFReq,
        HardPFReq,
        SoftPFResp,
        HardPFResp,
        WriteLineReq,
        UpgradeReq,
        SCUpgradeReq,           // Special "weak" upgrade for StoreCond
        UpgradeResp,
        SCUpgradeFailReq,       // Failed SCUpgradeReq in MSHR (never sent)
        UpgradeFailResp,        // Valid for SCUpgradeReq only
        ReadExReq,
        ReadExResp,
        ReadCleanReq,
        ReadSharedReq,
        LoadLockedReq,
        StoreCondReq,
        StoreCondFailReq,       // Failed StoreCondReq in MSHR (never sent)
        StoreCondResp,
        SwapReq,
        SwapResp,
        MessageReq,
        MessageResp,
        MemFenceReq,
        MemFenceResp,
        // Error responses
        // @TODO these should be classified as responses rather than
        // requests; coding them as requests initially for backwards
        // compatibility
        InvalidDestError,  // packet dest field invalid
        BadAddressError,   // memory address invalid
        FunctionalReadError, // unable to fulfill functional read
        FunctionalWriteError, // unable to fulfill functional write
                              // Fake simulator-only commands
        PrintReq,       // Print state matching address
        FlushReq,      //request for a cache flush
        InvalidateReq,   // request for address to be invalidated
        InvalidateResp,
        NUM_MEM_CMDS
    }

    public enum Attribute
    {
        IsRead,         //!< Data flows from responder to requester
        IsWrite,        //!< Data flows from requester to responder
        IsUpgrade,
        IsInvalidate,
        NeedsWritable,  //!< Requires writable copy to complete in-cache
        IsRequest,      //!< Issued by requester
        IsResponse,     //!< Issue by responder
        NeedsResponse,  //!< Requester needs response from target
        IsEviction,
        IsSWPrefetch,
        IsHWPrefetch,
        IsLlsc,         //!< Alpha/MIPS LL or SC access
        HasData,        //!< There is an associated payload
        IsError,        //!< Error response
        IsPrint,        //!< Print state matching address (for debugging)
        IsFlush,        //!< Flush the address from caches
        FromCache,      //!< Request originated from a caching agent
        NUM_COMMAND_ATTRIBUTES
    }



    public class Command
    {
        private string SET1(Attribute @att)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < (int)@att+1; i++)
            {
                if (i == (int)@att)
                    sb.Append("1");
                else
                    sb.Append("0");
            }
            return sb.ToString();
        }
        private string SET2(Attribute @att1, Attribute @att2)
        {
            return SET2(SET1(@att1), @att2);
        }
        private string SET2(string @att1, Attribute @att2)
        {
            var string1 = @att1;
            var string2 = SET1(@att2);
            StringBuilder sb = new StringBuilder();
            var max = Math.Max(@att1.Count(), (int)@att2 + 1);
            for (int i = 0; i < max; i++)
            {
                char a, b;
                if (i < @att1.Count())

                    a = @att1[i];
                else
                    a = '0';

                if (i == (int)@att2)
                    b = '1';
                else
                    b = '0';
                if (a == '0' && b == '0')
                    sb.Append("0");
                else
                    sb.Append("1");
            }
            return sb.ToString();
        }
        private string SET3(Attribute @att1, Attribute @att2, Attribute @att3)
        {
            return SET2(SET2(@att1, @att2), @att3);
        }
        private string SET4(Attribute @att1, Attribute @att2, Attribute @att3, Attribute @att4)
        {
            return SET2(SET3(@att1, @att2, @att3), @att4);
        }
        private string SET5(Attribute @att1, Attribute @att2, Attribute @att3, Attribute @att4, Attribute @att5)
        {
           return SET2(SET4(@att1, @att2, @att3, @att4), @att5);
        }
        private string SET6(Attribute @att1, Attribute @att2, Attribute @att3, Attribute @att4, Attribute @att5, Attribute @att6)
        {
            return SET2(SET5(@att1, @att2, @att3, @att4,@att5), @att6);
        }
        private string SET7(Attribute @att1, Attribute @att2, Attribute @att3, Attribute @att4, Attribute @att5, Attribute @att6, Attribute @att7)
        {
            return SET2(SET6(@att1, @att2, @att3, @att4, @att5,@att6), @att7);
        }

        public FlagBit _flag = new FlagBit((int)Attribute.NUM_COMMAND_ATTRIBUTES);

        public CMD _cmd;

        public Command(CMD cmd)
        {
            _cmd = cmd;
            MatchCMD(_cmd);
        }
        public Command(int cmd)
        {
            _cmd = (CMD)cmd;
            MatchCMD(_cmd);
        }

        public Command()
        {
            _cmd = CMD.InvalidCmd;
            MatchCMD(_cmd);
        }
        public bool isRead() { return testCmdAttrib(Attribute.IsRead); }
        public bool isWrite() { return testCmdAttrib(Attribute.IsWrite); }
        public bool isUpgrade() { return testCmdAttrib(Attribute.IsUpgrade); }
        public bool isRequest() { return testCmdAttrib(Attribute.IsRequest); }
        public bool isResponse() { return testCmdAttrib(Attribute.IsResponse); }
        public bool needsWritable() { return testCmdAttrib(Attribute.NeedsWritable); }
        public bool needsResponse() { return testCmdAttrib(Attribute.NeedsResponse); }
        public bool isInvalidate() { return testCmdAttrib(Attribute.IsInvalidate); }
        public bool isEviction() { return testCmdAttrib(Attribute.IsEviction); }
        public bool fromCache() { return testCmdAttrib(Attribute.FromCache); }

        /**
         * A writeback is an eviction that carries data.
         */
        public bool isWriteback()
        {
            return testCmdAttrib(Attribute.IsEviction) &&
             testCmdAttrib(Attribute.HasData);
        }

        /**
         * Check if this particular packet type carries payload data. Note
         * that this does not reflect if the data pointer of the packet is
         * valid or not.
         */
        public bool hasData() { return testCmdAttrib(Attribute.HasData); }
        public bool isLLSC() { return testCmdAttrib(Attribute.IsLlsc); }
        public bool isSWPrefetch() { return testCmdAttrib(Attribute.IsSWPrefetch); }
        public bool isHWPrefetch() { return testCmdAttrib(Attribute.IsHWPrefetch); }
        public bool isPrefetch()
        {
            return testCmdAttrib(Attribute.IsSWPrefetch) ||
             testCmdAttrib(Attribute.IsHWPrefetch);
        }
        public bool isError() { return testCmdAttrib(Attribute.IsError); }
        public bool isPrint() { return testCmdAttrib(Attribute.IsPrint); }
        public bool isFlush() { return testCmdAttrib(Attribute.IsFlush); }
        private bool testCmdAttrib(Attribute attrib)
        {
            return _flag[(int)attrib] != '0';
        }
        //public bool this[int index]
        //{
        //    get
        //    {
        //        return _flag[index] == '1';

        //    }
        //}
        private void MatchCMD(CMD cmd)
        {
            switch (cmd)
            {
                case CMD.InvalidCmd:
                    _flag.set("");
                    break;
                case CMD.ReadReq:
                    _flag.set(SET3(Attribute.IsRead, Attribute.IsRequest, Attribute.NeedsResponse));
                    break;
                case CMD.ReadResp:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsRequest, Attribute.HasData, Attribute.IsResponse));
                    break;
                case CMD.ReadRespWithInvalidate:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsResponse, Attribute.HasData, Attribute.IsInvalidate));
                    break;
                case CMD.WriteReq:
                    _flag.set(SET5(Attribute.IsWrite, Attribute.NeedsWritable, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.HasData));
                    break;
                case CMD.WriteResp:
                    _flag.set(SET2(Attribute.IsWrite, Attribute.IsResponse));
                    break;
                case CMD.WritebackDirty:
                    _flag.set(SET5(Attribute.IsWrite, Attribute.IsRequest, Attribute.IsEviction, Attribute.HasData, Attribute.FromCache));
                    break;
                case CMD.WritebackClean:
                    _flag.set(SET5(Attribute.IsWrite, Attribute.IsRequest, Attribute.IsEviction, Attribute.HasData, Attribute.FromCache));
                    break;
                case CMD.CleanEvict:
                    _flag.set(SET3(Attribute.IsRequest, Attribute.IsEviction, Attribute.FromCache));
                    break;
                case CMD.SoftPFReq:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsRequest, Attribute.IsSWPrefetch, Attribute.NeedsResponse));
                    break;
                case CMD.HardPFReq:
                    _flag.set(SET5(Attribute.IsRead, Attribute.IsRequest, Attribute.IsHWPrefetch, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.SoftPFResp:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsResponse, Attribute.IsSWPrefetch, Attribute.HasData));
                    break;
                case CMD.HardPFResp:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsResponse, Attribute.IsHWPrefetch, Attribute.HasData));
                    break;
                case CMD.WriteLineReq:
                    _flag.set(SET5(Attribute.IsWrite, Attribute.NeedsWritable, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.HasData));
                    break;
                case CMD.UpgradeReq:
                    _flag.set(SET6(Attribute.IsInvalidate, Attribute.NeedsWritable, Attribute.IsUpgrade, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.SCUpgradeReq:
                    _flag.set(SET7(Attribute.IsInvalidate, Attribute.NeedsWritable, Attribute.IsUpgrade, Attribute.IsLlsc, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.UpgradeResp:
                    _flag.set(SET2(Attribute.IsUpgrade, Attribute.IsResponse));
                    break;
                case CMD.SCUpgradeFailReq:
                    _flag.set(SET7(Attribute.IsRead, Attribute.NeedsWritable, Attribute.IsInvalidate, Attribute.IsLlsc, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.UpgradeFailResp:
                    _flag.set(SET3(Attribute.IsRead, Attribute.IsResponse, Attribute.HasData));
                    break;
                case CMD.ReadExReq:
                    _flag.set(SET6(Attribute.IsRead, Attribute.NeedsWritable, Attribute.IsInvalidate, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.ReadExResp:
                    _flag.set(SET3(Attribute.IsRead, Attribute.IsResponse, Attribute.HasData));
                    break;
                case CMD.ReadCleanReq:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.ReadSharedReq:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.LoadLockedReq:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsLlsc, Attribute.IsRequest, Attribute.NeedsResponse));
                    break;
                case CMD.StoreCondReq:
                    _flag.set(SET6(Attribute.IsWrite, Attribute.NeedsWritable, Attribute.IsLlsc, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.HasData));
                    break;
                case CMD.StoreCondFailReq:
                    _flag.set(SET6(Attribute.IsWrite, Attribute.NeedsWritable, Attribute.IsLlsc, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.HasData));
                    break;
                case CMD.StoreCondResp:
                    _flag.set(SET3(Attribute.IsWrite, Attribute.IsLlsc, Attribute.IsResponse));
                    break;
                case CMD.SwapReq:
                    _flag.set(SET6(Attribute.IsRead, Attribute.IsWrite, Attribute.NeedsWritable, Attribute.IsRequest, Attribute.HasData, Attribute.NeedsResponse));
                    break;
                case CMD.SwapResp:
                    _flag.set(SET4(Attribute.IsRead, Attribute.IsWrite, Attribute.IsResponse, Attribute.HasData));
                    break;
                case CMD.MessageReq:
                    _flag.set(SET4(Attribute.IsWrite, Attribute.IsRequest, Attribute.NeedsResponse, Attribute.HasData));
                    break;
                case CMD.MessageResp:
                    _flag.set(SET2(Attribute.IsWrite, Attribute.IsResponse));
                    break;
                case CMD.MemFenceReq:
                    _flag.set(SET2(Attribute.IsRequest, Attribute.NeedsResponse));
                    break;
                case CMD.MemFenceResp:
                    _flag.set(SET1(Attribute.IsResponse));
                    break;
                case CMD.InvalidDestError:
                    _flag.set(SET2(Attribute.IsResponse, Attribute.IsError));
                    break;
                case CMD.BadAddressError:
                    _flag.set(SET2(Attribute.IsResponse, Attribute.IsError));
                    break;
                case CMD.FunctionalReadError:
                    _flag.set(SET3(Attribute.IsRead, Attribute.IsResponse, Attribute.IsError));
                    break;
                case CMD.FunctionalWriteError:
                    _flag.set(SET3(Attribute.IsWrite, Attribute.IsResponse, Attribute.IsError));
                    break;
                case CMD.PrintReq:
                    _flag.set(SET2(Attribute.IsRequest, Attribute.IsPrint));
                    break;
                case CMD.FlushReq:
                    _flag.set(SET3(Attribute.IsRequest, Attribute.IsFlush, Attribute.NeedsWritable));
                    break;
                case CMD.InvalidateReq:
                    _flag.set(SET5(Attribute.IsInvalidate, Attribute.IsRequest, Attribute.NeedsWritable, Attribute.NeedsResponse, Attribute.FromCache));
                    break;
                case CMD.InvalidateResp:
                    _flag.set(SET2(Attribute.IsInvalidate, Attribute.IsResponse));
                    break;
                default:
                    _flag.set("");
                    break;

            }
        }


        public static bool operator ==(Command a, Command b) { return (a._cmd == b._cmd); }
        public static bool operator !=(Command a, Command b) { return (a._cmd != b._cmd); }


        public string toString() { return _cmd.ToString(); }
        public int toInt() { return (int)_cmd; }
    }
}
