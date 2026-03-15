using System.Runtime.CompilerServices;
using System.Text;

namespace QuickJSharp;

internal static unsafe class JSUtils
{
    private static readonly UTF8Encoding UTF8 = new(false);

    public static string GetString(byte* ptr)
    {
        if (ptr == null) return null!;
        int len = 0;
        while (ptr[len] != 0) len++;
        return UTF8.GetString(ptr, len);
    }

    /// <summary>
    /// Gets a string from a UTF8 buffer of a specific length.
    /// </summary>
    public static string GetString(byte* ptr, int length)
    {
        if (ptr == null) return null!;
        return UTF8.GetString(ptr, length);
    }

    /// <summary>
    /// Encodes a string as null-terminated UTF8 into a buffer.
    /// Returns the number of bytes written (excluding null terminator).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetUtf8(string str, byte* buffer, int bufferLength)
    {
        int written;
        fixed (char* pStr = str)
        {
            written = UTF8.GetBytes(pStr, str.Length, buffer, bufferLength - 1);
        }
        buffer[written] = 0;
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxByteCount(int charCount) => UTF8.GetMaxByteCount(charCount) + 1;
}

