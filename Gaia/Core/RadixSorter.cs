using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.Core
{
    public interface IRadix
    {
        ulong ToRadixID();
    }

    public static class RadixSorter
    {
        static void radix(int stride, IRadix[] source, IRadix[] dest)
        {
            int[] count = new int[256];
            int[] index = new int[256];
            for (int i = 0; i < source.Length; i++)
                count[((source[i].ToRadixID()) >> (stride * 8)) & 0xff]++;

            index[0] = 0;
            for (int i = 1; i < 256; i++) index[i] = index[i - 1] + count[i - 1];
            for (int i = 0; i < source.Length; i++) dest[index[((source[i].ToRadixID()) >> (stride * 8)) & 0xff]++] = source[i];
        }

        public static IRadix[] Sort(IRadix[] source, int sizeOfIDInBytes)
        {
            IRadix[] dest = new IRadix[source.Length];
            bool swap = false;
            for (int i = 0; i <= sizeOfIDInBytes; i++)
            {
                if (swap)
                    radix(i, dest, source);
                else
                    radix(i, source, dest);
                swap = !swap;
            }
            if (swap)
                return source;
            else
                return dest;
        }
    }
}
