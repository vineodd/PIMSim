using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.DDR
{
        public class ClockDomainCrosser
    {
        
		public ClockUpdateCB callback;
        public UInt64 clock1, clock2;
        public UInt64 counter1, counter2;
        public ClockDomainCrosser(ClockUpdateCB _callback)
        {
            callback = _callback;
            clock1 = 1UL;
            clock2 = 1UL;
            counter1 = 0UL;
            counter2 = 0UL;
        }
        public ClockDomainCrosser(UInt64 _clock1, UInt64 _clock2, ClockUpdateCB _callback)
        {
            callback = _callback;
            clock1 = _clock1;
            clock2 = _clock2;
            counter1 = 0;
            counter2 = 0;
        }
        public ClockDomainCrosser(double ratio, ClockUpdateCB _callback)
        {
            callback = _callback;
            counter1 = 0;
            counter2 = 0;
            // Compute numerator and denominator for ratio, then pass that to other constructor.
            double x = ratio;

            const int MAX_ITER = 15;
            int i;
            int[] ns = new int[MAX_ITER];


            int[] ds = new int[MAX_ITER];
            double[] zs = new double[MAX_ITER];
            
            ds[0] = 0;
            ds[1] = 1;
            zs[1] = x;
            ns[1] = (int)x;

            for (i = 1; i < MAX_ITER - 1; i++)
            {
                if ( Math.Abs(x - (double)ns[i] / (double)ds[i]) < 0.00005)
                {
                    //printf("ANSWER= %u/%d\n",ns[i],ds[i]);
                    break;
                }
                //TODO: or, if the answers are the same as the last iteration, stop 

                zs[i + 1] = 1.0f / (zs[i] - (int)Math.Floor(zs[i])); // 1/(fractional part of z_i)
                ds[i + 1] = ds[i] * (int)Math.Floor(zs[i + 1]) + ds[i - 1];
                double tmp = x * ds[i + 1];
                double tmp2 = tmp - (int)tmp;
                ns[i + 1] = tmp2 >= 0.5 ?(int)Math.Ceiling (tmp) : (int)Math.Floor(tmp); // ghetto implementation of a rounding function
                                                                  //printf("i=%lu, z=%20f n=%5u d=%5u\n",i,zs[i],ns[i],ds[i]);
            }

            //printf("APPROXIMATION= %u/%d\n",ns[i],ds[i]);
            this.clock1 = (ulong)ns[i];
            this.clock2 = (ulong)ds[i];

            //cout << "CTOR: callback address: " << (uint64_t)(this->callback) << "\t ratio="<<clock1<<"/"<<clock2<< endl;
        }
        public void update()
        {
            //short circuit case for 1:1 ratios
            if (clock1 == clock2 && callback!=null)
            {
                callback();
                return;
            }

            // Update counter 1.
            counter1 += clock1;

            while (counter2 < counter1)
            {
                counter2 += clock2;
                //cout << "CALLBACK: counter1= " << counter1 << "; counter2= " << counter2 << "; " << endl;
                //cout << "callback address: " << (uint64_t)callback << endl;
                if (callback!=null)
                {
                    //cout << "Callback() " << (uint64_t)callback<< "Counters: 1="<<counter1<<", 2="<<counter2 <<endl;
                    callback();
                }
            }

            if (counter1 == counter2)
            {
                counter1 = 0;
                counter2 = 0;
            }
        }
    }

    
}
