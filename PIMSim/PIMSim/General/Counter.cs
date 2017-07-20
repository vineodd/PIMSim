#region Reference 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace PIMSim.General
{
    /// <summary>
    /// Implement of Counter.
    /// Non-blocked Semaphore implement.
    /// </summary>
    public class Counter
    {
        #region Private Variables
        /// <summary>
        /// Current value.
        /// </summary>
        private int count;

        /// <summary>
        /// Max counter value.
        /// </summary>
        private int max;

        #endregion

        #region Public Methods
        /// <summary>
        /// Construction Function
        /// </summary>
        /// <param name="start_">start value.</param>
        /// <param name="max_">max value.</param>
        public Counter(int start_,int max_)
        {
            if (start_ < 0)
                throw new ArgumentException();
            if (max_ < start_)
                throw new ArgumentException();
            count = start_;
            max = max_;
        }

        /// <summary>
        /// Waitting Signal.
        /// </summary>
        /// <returns></returns>
        public bool WaitOne()
        {
            if (count < 0)
                throw new ArgumentException();
            if (count == 0)
            {
                return false;
            }
            count--;
            if (count < 0)
                throw new ArgumentException();
            return true;
        }

        /// <summary>
        /// Reset Counter.
        /// </summary>
        /// <param name="start_">initial value.</param>
        public void Reset(int start_)
        {
            if (start_ < 0)
                throw new ArgumentException();
            if (max < start_)
                throw new ArgumentException();
            count = start_;
        }

        /// <summary>
        /// Reset Counter.
        /// </summary>
        public void Reset()
        {
            count = max;
        }

        /// <summary>
        /// Free signal.
        /// </summary>
        public void Release()
        {
            if (count + 1 > max)
                return;
            count++;
        }

        /// <summary>
        /// If zero. 
        /// </summary>
        public bool Zero => count == 0 && max != 0;

        #endregion
    }
}
