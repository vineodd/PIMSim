#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Procs;

#endregion

namespace PIMSim.General
{
    /// <summary>
    /// Processor Request Defination.
    /// </summary>
    public class ProcRequest : Request
    {
        #region Public Vaiables

        /// <summary>
        /// id of Processors
        /// </summary>
        public int pid;

        /// <summary>
        /// Cycles should be serve.
        /// </summary>
        public UInt64 cycle;

        /// <summary>
        /// Type.
        /// </summary>
        public RequestType type;

        //address
        public UInt64 actual_addr;
        public UInt64 block_addr;

        /// <summary>
        /// Ready bit.
        /// True : This Request is completely processed.
        /// </summary>
        public bool ready = false;

        /// <summary>
        /// Memory bit.
        /// True : This request contains memory operations.
        /// </summary>
        public bool if_mem;

        /// <summary>
        /// PC.
        /// </summary>
        public UInt64 pc;


        public List<int> stage_id = new List<int>();
        #endregion

        #region Public Methods

        /// <summary>
        /// Parse Instructions into ProcRequest
        /// </summary>
        /// <param name="ins_">Parsed Instructions</param>
        public void parse_ins(Instruction ins_)
        {
            pid = ins_.pid;
            switch (ins_.type)
            {
                case InstructionType.NOP:
                    type = RequestType.NOP;
                    break;
                case InstructionType.CALCULATION:
                    type = RequestType.CALCULATION;
                    break;
                case InstructionType.READ:
                    type = RequestType.READ;
                    break;
                case InstructionType.WRITE:
                    type = RequestType.WRITE;
                    break;
                default:
                    type = RequestType.NOP;
                    break;
            }
            pc = ins_.pc;
            if_mem = ins_.is_mem;
            cycle = ins_.cycle;
            actual_addr = ins_.address;
            block_addr = ins_.block_addr;
            
        }

        #endregion
    }

    /// <summary>
    /// Requests Type.
    /// <para>READ,WRITE : Memory operation.</para>
    /// <para>NOP : no operations.</para>
    /// <para>CALCULATION : ALU operation.</para>
    /// </summary>
    public enum RequestType
    {
        READ,
        WRITE,
        NOP,
        CALCULATION,
        FLUSH,
        LOAD,
        STORE
    }
}
