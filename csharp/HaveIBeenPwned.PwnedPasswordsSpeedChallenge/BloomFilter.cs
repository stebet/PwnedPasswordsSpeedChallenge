using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Stebet.BloomFilter;

public class BloomFilter
{
    int n;
    double p;
    long m;
    int k;
    byte[][] _filter;
    double ratio;
    private readonly Encoding _utf8 = Encoding.UTF8;
    
    public BloomFilter(int numItems, double falsePositiveRatio)
    {
        n = numItems;
        p = falsePositiveRatio;
        m = (long)Math.Ceiling((n * Math.Log(p)) / Math.Log(1 / Math.Pow(2, Math.Log(2))));
        long numBytes = m / 8;
        int numBlocks = (int)Math.Ceiling(numBytes / 32768.0);
        _filter = new byte[numBlocks][];
        for(int i = 0;i < numBlocks; i++)
        {
            _filter[i] = new byte[32768];
        }
        k = (int)Math.Round((m / n) * Math.Log(2));
        ratio = m / (double)uint.MaxValue;
    }

    public void AddItem(ReadOnlySpan<byte> item)
    {
        for(int i = 0; i < k; i++)
        {
            SetBit(Murmur.ComputeHash(item, (uint)i));
        }
    }

    public void AddItem(ReadOnlySpan<char> item)
    {
        int bytesRequired = _utf8.GetByteCount(item);
        byte[]? array = null;
        Span<byte> span = bytesRequired < 128 ? stackalloc byte[bytesRequired] : (array = ArrayPool<byte>.Shared.Rent(bytesRequired)).AsSpan(0, bytesRequired);
        _utf8.GetBytes(item, span);

        AddItem(span);

        if(array != null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    private void SetBit(uint v)
    {
        var index = GetIndex(v);
        _filter[index.Block][index.Byte] |= (byte)(1 << index.Bit);
    }

    private (int Block, int Byte, int Bit) GetIndex(uint v)
    {
        long index = (long)(v * ratio);
        long byteLongIndex = index / 8;
        long blockIndex = byteLongIndex / 32768;
        int byteIndex = (int)(byteLongIndex - blockIndex * 32768);
        return ((int)blockIndex, byteIndex, (int)(index - (blockIndex * 32768 * 8) - (byteIndex * 8)));
    }

    private bool CheckBit(uint v)
    {
        var index = GetIndex(v);
        if ((uint)_filter.Length >= index.Byte)
        {
            return (_filter[index.Block][index.Byte] & (1 << index.Bit)) > 0;
        }

        return false;
    }

    public bool CheckItem(ReadOnlySpan<byte> item)
    {
        for(int i = 0; i < k; i++)
        {
            if(!CheckBit(Murmur.ComputeHash(item, (uint)i)))
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckItem(ReadOnlySpan<char> item)
    {
        int bytesRequired = _utf8.GetByteCount(item);
        byte[]? array = null;
        Span<byte> span = bytesRequired < 128 ? stackalloc byte[bytesRequired] : (array = ArrayPool<byte>.Shared.Rent(bytesRequired)).AsSpan(0, bytesRequired);
        _utf8.GetBytes(item, span);

        var result = CheckItem(span);

        if (array != null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        return result;
    }
}
