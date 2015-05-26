using System.Runtime.InteropServices;

namespace ETG.Orleans.Swmr
{
    internal class MurmurHash2
    {
        public static uint Hash(byte[] data)
        {
            return Hash(data, 0xc58f1a7b);
        }
        const uint M = 0x5bd1e995;
        const int R = 24;

        [StructLayout(LayoutKind.Explicit)]
        struct BytetoUInt32Converter
        {
            [FieldOffset(0)]
            public byte[] Bytes;

            [FieldOffset(0)]
            public readonly uint[] UInts;
        }

        public static uint Hash(byte[] data, uint seed)
        {
            var length = data.Length;
            if (length == 0)
                return 0;
            var h = seed ^ (uint)length;
            var currentIndex = 0;
            // array will be length of Bytes but contains Uints
            // therefore the currentIndex will jump with +1 while length will jump with +4
            var hackArray = new BytetoUInt32Converter { Bytes = data }.UInts;
            while (length >= 4)
            {
                var k = hackArray[currentIndex++];
                k *= M;
                k ^= k >> R;
                k *= M;

                h *= M;
                h ^= k;
                length -= 4;
            }
            currentIndex *= 4; // fix the length
            switch (length)
            {
                case 3:
                    h ^= (ushort)(data[currentIndex++] | data[currentIndex++] << 8);
                    h ^= (uint)data[currentIndex] << 16;
                    h *= M;
                    break;
                case 2:
                    h ^= (ushort)(data[currentIndex++] | data[currentIndex] << 8);
                    h *= M;
                    break;
                case 1:
                    h ^= data[currentIndex];
                    h *= M;
                    break;
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;

            return h;
        }
    }
}