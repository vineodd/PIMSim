#region Reference
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIMSim.Configs;
#endregion

namespace PIMSim.Procs
{
    /// <summary>
    /// Mapping block addresses and actual addresses 
    /// </summary>
    public class PageConverter
    {
        #region  Private Variables

        private UInt64 page_size = 0;
        /// <summary>
        /// Used in random mode.
        /// </summary>
        private UInt64 stride = 0;

        /// <summary>
        /// Page table of existed pages.
        /// </summary>
        private Dictionary<UInt64, UInt64> page_table;

        /// <summary>
        /// frame
        /// </summary>
        private List<UInt64> frame;
        private UInt64 curr_fid;

        #endregion

        #region Public Methods

        /// <summary>
        /// Construction Function
        /// </summary>
        /// <param name="random">random mode</param>
        public PageConverter(bool random = false)
        {

            page_size = Config.page_size;
            if (!random)
            {
                stride = (UInt64)(Config.channel * Config.rank * Config.bank);

            }
            frame = new List<UInt64>();
            page_table = new Dictionary<UInt64, UInt64>();
        }

        /// <summary>
        /// Get corresponding mapping.
        /// </summary>
        /// <param name="addr_">actual address. </param>
        /// <returns>virtual address </returns>
        public UInt64 scan_page(UInt64 addr_)
        {
            UInt64 page = addr_ / page_size;
            UInt64 offset = addr_ % page_size;

            if (page_table.ContainsKey(page))
            {
                // HIT
                return page_table[page] * page_size + offset;
            }
            UInt64 index;
            index= page / stride;
            index *= stride;
            index += curr_fid;
            frame.Add(index);
            page_table.Add(page, index);

            curr_fid = (curr_fid + 1) % stride;

            return index * page_size + offset;

        }
        #endregion
    }
}
