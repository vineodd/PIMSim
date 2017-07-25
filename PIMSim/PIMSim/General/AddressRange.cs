using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Addr = System.UInt64;


namespace PIMSim.General
{
    public class AddressRange
    {

        /// Private fields for the start and end of the range
        /// Both _start and _end are part of the range.
        private Addr _start;
        private Addr _end;

        /// The high bit of the slice that is used for interleaving
        Byte intlvHighBit;

        /// The high bit of the slice used to XOR hash the value we match
        /// against, set to 0 to disable.
        Byte xorHighBit;

        /// The number of bits used for interleaving, set to 0 to disable
        Byte intlvBits;

        /// The value to compare the slice addr[high:(high - bits + 1)]
        /// with.
        Byte intlvMatch;



        public AddressRange()

        {
            _start = 1;
            _end = 0;
            intlvHighBit = 0;
            xorHighBit = 0;
            intlvBits = 0;
            intlvMatch = 0;
        }

        AddressRange(Addr _start, Addr _end, Byte _intlv_high_bit,
                  Byte _xor_high_bit, Byte _intlv_bits,
                  Byte _intlv_match)

        {
            this._start = _start;
            this._end = _end;
            this.intlvHighBit = _intlv_high_bit;

            this.xorHighBit = _xor_high_bit;
            this.intlvBits = _intlv_bits;

            this.intlvMatch = _intlv_match;

            // sanity checks
            if (intlvBits > 0 && (intlvMatch >= (1 << intlvBits)))
                Debug.Fail(String.Format("Match value {0} does not fit in {1} interleaving bits\n", intlvMatch, intlvBits));

            // ignore the XOR bits if not interleaving
            if (intlvBits > 0 && xorHighBit > 0)
            {
                if (xorHighBit == intlvHighBit)
                {
                    Debug.Fail(String.Format("XOR and interleave high bit must be different\n"));
                }
                else if (xorHighBit > intlvHighBit)
                {
                    if ((xorHighBit - intlvHighBit) < intlvBits)
                        Debug.Fail(String.Format("XOR and interleave high bit must be at least \n {0} bits apart\n", intlvBits));
                }
                else {
                    if ((intlvHighBit - xorHighBit) < intlvBits)
                    {
                        Debug.Fail(String.Format("Interleave and XOR high bit must be at least \n {0} bits apart\n", intlvBits));
                    }
                }
            }
        }

        public AddressRange(Addr _start, Addr _end)

        {
            this._start = _start;
            this._end = _end;
            intlvHighBit = 0;
            xorHighBit = 0;

            intlvBits = 0;
            intlvMatch = 0;
        }

        /**
         * Create an address range by merging a collection of interleaved
         * ranges.
         *
         * @param ranges Interleaved ranges to be merged
         */
        public AddressRange(List<AddressRange> ranges)
        {
            this._start = 1;
            this._end = 0;
            intlvHighBit = 0;
            xorHighBit = 0;

            intlvBits = 0;
            intlvMatch = 0;

            if (!(ranges.Count <= 0))
            {
                // get the values from the first one and check the others
                _start = ranges[0]._start;
                _end = ranges[0]._end;
                intlvHighBit = ranges[0].intlvHighBit;
                xorHighBit = ranges[0].xorHighBit;
                intlvBits = ranges[0].intlvBits;

                if (ranges.Count() != (1 << intlvBits))
                    Debug.Fail(String.Format("Got {0} ranges spanning {1} interleaving bits\n", ranges.Count(), intlvBits));

                Byte match = 0;
                foreach (var r in ranges)
                {
                    if (!mergesWith(r))
                        Debug.Fail(String.Format("Can only merge ranges with the same start, end and interleaving bits\n"));

                    if (r.intlvMatch != match)
                        Debug.Fail(String.Format("Expected interleave match %d but got %d when merging\n", match, r.intlvMatch));
                    ++match;
                }

                // our range is complete and we can turn this into a
                // non-interleaved range
                intlvHighBit = 0;
                xorHighBit = 0;
                intlvBits = 0;
            }
        }

        /**
         * Determine if the range is interleaved or not.
         *
         * @return true if interleaved
         */
        public bool interleaved() { return intlvBits != 0; }

        /**
         * Determine if the range interleaving is hashed or not.
         */
        public bool hashed() { return interleaved() && xorHighBit != 0; }

        /**
         * Determing the interleaving granularity of the range.
         *
         * @return The size of the regions created by the interleaving bits
         */
        public UInt64 granularity()
        {
            return (ulong)1 << (intlvHighBit - intlvBits + 1);
        }

        /**
         * Determine the number of interleaved address stripes this range
         * is part of.
         *
         * @return The number of stripes spanned by the interleaving bits
         */
        public UInt32 stripes() { return (uint)1 << intlvBits; }

        /**
         * Get the size of the address range. For a case where
         * interleaving is used we make the simplifying assumption that
         * the size is a divisible by the size of the interleaving slice.
         */
        public Addr size()
        {
            return (_end - _start + 1) >> intlvBits;
        }

        /**
         * Determine if the range is valid.
         */
        public bool valid() { return _start <= _end; }

        /**
         * Get the start address of the range.
         */
        public Addr start() { return _start; }

        /**
         * Get the end address of the range.
         */
        public Addr end() { return _end; }

        /**
         * Get a string representation of the range. This could
         * alternatively be implemented as a operator, but at the moment
         * that seems like overkill.
         */
        public override string ToString()
        {
            if (interleaved())
            {
                if (hashed())
                {
                    return string.Format("[{0} : {1}], [{2} : {3}] XOR [{4} : {5}] = {6}",
                                    _start, _end,
                                    intlvHighBit, intlvHighBit - intlvBits + 1,
                                    xorHighBit, xorHighBit - intlvBits + 1,
                                    intlvMatch);
                }
                else {
                    return string.Format("[{0} : {1}], [[{2} : {3}] = {4}",
                                    _start, _end,
                                    intlvHighBit, intlvHighBit - intlvBits + 1,
                                    intlvMatch);
                }
            }
            else {
                return string.Format("[{0} : {1}]", _start, _end);
            }
        }

        /**
         * Determine if another range merges with the current one, i.e. if
         * they are part of the same contigous range and have the same
         * interleaving bits.
         *
         * @param r Range to evaluate merging with
         * @return true if the two ranges would merge
         */
        public bool mergesWith(AddressRange r)
        {
            return r._start == _start && r._end == _end &&
                r.intlvHighBit == intlvHighBit &&
                r.xorHighBit == xorHighBit &&
                r.intlvBits == intlvBits;
        }

        /**
         * Determine if another range intersects this one, i.e. if there
         * is an address that is both in this range and the other
         * range. No check is made to ensure either range is valid.
         *
         * @param r Range to intersect with
         * @return true if the intersection of the two ranges is not empty
         */
        public bool intersects(AddressRange r)
        {
            if (_start > r._end || _end < r._start)
                // start with the simple case of no overlap at all,
                // applicable even if we have interleaved ranges
                return false;
            else if (!interleaved() && !r.interleaved())
                // if neither range is interleaved, we are done
                return true;

            // now it gets complicated, focus on the cases we care about
            if (r.size() == 1)
                // keep it simple and check if the address is within
                // this range
                return contains(r.start());
            else if (mergesWith(r))
                // restrict the check to ranges that belong to the
                // same chunk
                return intlvMatch == r.intlvMatch;
            else {
                Debug.Fail(String.Format("Cannot test intersection of %s and %s\n", ToString(), r.ToString()));
                return false;
            }
        }

        /**
         * Determine if this range is a subset of another range, i.e. if
         * every address in this range is also in the other range. No
         * check is made to ensure either range is valid.
         *
         * @param r Range to compare with
         * @return true if the this range is a subset of the other one
         */
        public bool isSubset(AddressRange r)
        {
            if (interleaved())
                Debug.Fail(String.Format("Cannot test subset of interleaved range %s\n", ToString()));
            return _start >= r._start && _end <= r._end;
        }

        /**
         * Determine if the range contains an address.
         *
         * @param a Address to compare with
         * @return true if the address is in the range
         */
        public bool contains(Addr a)
        {
            // check if the address is in the range and if there is either
            // no interleaving, or with interleaving also if the selected
            // bits from the address match the interleaving value
            bool in_range = a >= _start && a <= _end;
            if (!interleaved())
            {
                return in_range;
            }
            else if (in_range)
            {
                if (!hashed())
                {
                    return bits(a, intlvHighBit, intlvHighBit - intlvBits + 1) ==
                        intlvMatch;
                }
                else {
                    return (bits(a, intlvHighBit, intlvHighBit - intlvBits + 1) ^
                            bits(a, xorHighBit, xorHighBit - intlvBits + 1)) ==
                        intlvMatch;
                }
            }
            return false;
        }
        public Addr bits(Addr val, int first, int last)
        {
            int nbits = first - last + 1;
            return (val >> last) & mask(nbits);
        }
        /**
         * Remove the interleaving bits from an input address.
         *
         * This function returns a new address that doesn't have the bits
         * that are use to determine which of the interleaved ranges it
         * belongs to.
         *
         * e.g., if the input address is:
         * -------------------------------
         * | prefix | intlvBits | suffix |
         * -------------------------------
         * this function will return:
         * -------------------------------
         * |         0 | prefix | suffix |
         * -------------------------------
         *
         * @param the input address
         * @return the address without the interleaved bits
         */
        public Addr removeIntlvBits(Addr a)
        {
            var intlv_low_bit = intlvHighBit - intlvBits + 1;
            return insertBits(a >> intlvBits, intlv_low_bit - 1, 0, a);
        }

        public Addr insertBits(Addr val, int first, int last, Addr bit_val)
        {
            Addr t_bit_val = bit_val;
            Addr bmask = mask(first - last + 1) << last;
            return ((t_bit_val << last) & bmask) | (val & ~bmask);
        }

        public Addr mask(int nbits)
        {
            return (nbits == 64) ? UInt64.MaxValue : (1UL << nbits) - 1;
        }
        public Addr mbits(Addr val, int first, int last)
        {
            return val & (mask(first + 1) & ~mask(last));
        }
        /**
         * Determine the offset of an address within the range.
         *
         * This function returns the offset of the given address from the
         * starting address discarding any bits that are used for
         * interleaving. This way we can convert the input address to a
         * new unique address in a continuous range that starts from 0.
         *
         * @param the input address
         * @return the flat offset in the address range
         */
        public Addr getOffset(Addr a)
        {
            bool in_range = a >= _start && a <= _end;
            if (!in_range)
            {
                return Addr.MaxValue;
            }
            if (interleaved())
            {
                return removeIntlvBits(a) - removeIntlvBits(_start);
            }
            else {
                return a - _start;
            }
        }

        /**
         * Less-than operator used to turn an STL map into a binary search
         * tree of non-overlapping address ranges.
         *
         * @param r Range to compare with
         * @return true if the start address is less than that of the other range
         */
        public static bool operator <(AddressRange a, AddressRange b)
        {
            if (a._start != b._start)
                return a._start < b._start;
            else
                // for now assume that the end is also the same, and that
                // we are looking at the same interleaving bits
                return a.intlvMatch < b.intlvMatch;
        }
        public static bool operator >(AddressRange a, AddressRange b)
        {
            if (a._start != b._start)
                return a._start > b._start;
            else
                // for now assume that the end is also the same, and that
                // we are looking at the same interleaving bits
                return a.intlvMatch > b.intlvMatch;
        }
        public static bool operator ==(AddressRange a, AddressRange b)
        {
            if (a._start != b._start) return false;
            if (a._end != b._end) return false;
            if (a.intlvBits != b.intlvBits) return false;
            if (a.intlvBits != 0)
            {
                if (a.intlvHighBit != b.intlvHighBit) return false;
                if (a.intlvMatch != b.intlvMatch) return false;
            }
            return true;
        }
        public static bool operator !=(AddressRange a, AddressRange b)
        {
            if (a._start != b._start) return true;
            if (a._end != b._end) return true;
            if (a.intlvBits != b.intlvBits) return true;
            if (a.intlvBits != 0)
            {
                if (a.intlvHighBit != b.intlvHighBit) return true;
                if (a.intlvMatch != b.intlvMatch) return true;
            }
            return false;
        }


    }
}
