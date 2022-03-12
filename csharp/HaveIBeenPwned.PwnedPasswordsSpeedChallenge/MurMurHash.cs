using System.Runtime.CompilerServices;

namespace Stebet.BloomFilter;

public class Murmur
{
    private const MethodImplOptions FullOptimization = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

    [MethodImpl(FullOptimization)]
    private static uint Murmur32Scramble(uint k) => RotateLeft(k * 0xcc9e2d51, 15) * 0x1b873593;

    [MethodImpl(FullOptimization)]
    private static uint RotateLeft(uint x, int num) => (x << num) | (x >> 32-num);

    public static uint ComputeHash(ReadOnlySpan<byte> key, uint seed)
    {
        uint h = seed;
        uint tempVal = 0;
        int accumulator = 0;
        for(int i = 0; i < key.Length;i++)
        {
            tempVal = (tempVal << 8) | key[i];
            if(++accumulator == 4)
            {
                h ^= Murmur32Scramble(tempVal);
                h = RotateLeft(h, 13) * 5 + 0xe6546b64;
                tempVal = 0;
                accumulator = 0;
            }
        }

        if(accumulator > 0)
        {
            h ^= Murmur32Scramble(tempVal);
        }

        /* Finalize. */
        h ^= (uint)key.Length;
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}
