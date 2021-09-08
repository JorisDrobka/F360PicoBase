using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;



public static class BinaryUtil
{

    public static string EncodeToString(byte[] array)
    {
        var enc = new System.Text.UTF8Encoding();
        return enc.GetString(array);
    }

    public static byte[] DecodeFromString(string s)
    {
        var enc = new System.Text.UTF8Encoding();
        return enc.GetBytes(s);
    }

    //-----------------------------------------------------------------------------------------------------------------

    //  single byte operations

    public static bool GetBit(this byte self, int index)
    {
        return ((self >> index) & 1) != 0;
    }
    public static byte SetBit(this byte self, int index, bool value)
    {
        byte mask = (byte)(1 << index);
        return (byte)(value ? (self | mask) : (self & ~mask));
    }
    public static byte ToggleBit(this byte self, int index)
    {
        byte mask = (byte)(1 << Mathf.Clamp(index, 0, 7));
        self ^= mask;
        return self;
    }
    public static bool[] ToBoolArray(this byte self, int start=0, int length=8)
    {
        start = Mathf.Clamp(start, 0, 6);
        length = Mathf.Clamp(length, 1, 8-start);
        bool[] result = new bool[length];
        for(int i = start; i < length; i++) {
            result[i] = self.GetBit(i);
        }
        return result;
    }
    public static bool[] ToBoolArray(this byte self, bool[] buffer, int start=0, int length=8)
    {
        start = Mathf.Clamp(start, 0, 6);
        length = Mathf.Clamp(length, 1, 8-start);
        if(buffer == null || buffer.Length != length) {
            buffer = new bool[length];
        }
        for(int i = start; i < length; i++) {
            buffer[i] = self.GetBit(i);
        }
        return buffer;
    }

    //-----------------------------------------------------------------------------------------------------------------

    //  byte array operations

    public static byte[] SetBit(byte[] self, int index, bool value)
    {
        int byteIndex = index / 8;
        int bitIndex = index % 8;
        byte mask = (byte)(1 << bitIndex);

        self[byteIndex] = (byte)(value ? (self[byteIndex] | mask) : (self[byteIndex] & ~mask));
        return self;
    }

    public static byte[] ToggleBit(byte[] self, int index)
    {
        int byteIndex = index / 8;
        int bitIndex = index % 8;
        byte mask = (byte)(1 << bitIndex);

        self[byteIndex] ^= mask;
        return self;
    }

    public static bool GetBit(byte[] self, int index)
    {
        int byteIndex = index / 8;
        int bitIndex = index % 8;
        byte mask = (byte)(1 << bitIndex);

        return (self[byteIndex] & mask) != 0;
    }

    
    public static byte ConstructByte(params bool[] bits)
    {
        bool[] bitsFinal = bits;
        if(bits.Length > 8)
        {
            throw new ArgumentException("ConstructByte():: illegal number of bits");
        }
        else if(bits.Length < 8)
        {
            bitsFinal = new bool[8];
            for(int i = 0; i < bits.Length; i++) {
                bitsFinal[i] = bits[i];
            }
        }
        BitArray bArray = new BitArray(bitsFinal);
        return ConvertToByte(bArray);
    }
    public static byte ConstructByteInv(params bool[] bits)
    {
        bool[] bitsFinal;
        if(bits.Length > 8)
        {
            throw new ArgumentException("ConstructByte():: illegal number of bits");
        }
        else if(bits.Length < 8)
        {
            bitsFinal = new bool[8];
            for(int i = 0; i < bits.Length; i++) {
                bitsFinal[8-1-i] = bits[i];
            }
        }
        else
        {
            bitsFinal = new bool[8];
            for(int i = 0; i < bits.Length; i++)
            {
                bitsFinal[8-1-i] = bits[i];
            }
        }
        BitArray bArray = new BitArray(bitsFinal);
        return ConvertToByte(bArray);
    }

    static byte ConvertToByte(BitArray bits)
    {
        if (bits.Count != 8)
        {
            throw new ArgumentException("illegal number of bits");
        }

        byte b = 0;
        if (bits.Get(7)) b++;
        if (bits.Get(6)) b += 2;
        if (bits.Get(5)) b += 4;
        if (bits.Get(4)) b += 8;
        if (bits.Get(3)) b += 16;
        if (bits.Get(2)) b += 32;
        if (bits.Get(1)) b += 64;
        if (bits.Get(0)) b += 128;
        return b;
    }

    //-----------------------------------------------------------------------------------------------------------------

    public static string PrintByte(this byte self, int start=0, int length=8)
    {
        var flags = self.ToBoolArray(start, length);
        return __flagsToString(flags).ToString();
    }
    public static string PrintByte(this byte self, bool[] buffer, int start=0, int length=8)
    {
        var flags = self.ToBoolArray(buffer, start, length);
        return __flagsToString(flags).ToString();
    }
    
    public static string PrintByteSequence(this byte[] self, int start=0, int length=-1)
    {
        start = Mathf.Max(0, start);
        int end = length > 0 
                    ? Mathf.Clamp(start+length+1, start, self.Length) 
                    : self.Length;

        var b = new System.Text.StringBuilder();
        var buffer = new bool[8];
        for(int i = start; i < end; i++) 
        {
            buffer = self[i].ToBoolArray(buffer);
            b.Append("{" + i.ToString() + "} " +__flagsToString(buffer).ToString());
            b.Append("\n");
        }
        return b.ToString();
    }


    static System.Text.StringBuilder __flagsToString(bool[] flags)
    {
        var b = new System.Text.StringBuilder("[");
        for(int i = 0; i < flags.Length; i++) {
            if(i > 0) b.Append(", ");
            b.Append(flags[i] ? "1" : "0");
        }
        b.Append("]");
        return b;
    }
        
}



//-----------------------------------------------------------------------------------------------------------------
//
//  FILE OPS
//
//-----------------------------------------------------------------------------------------------------------------

public static class BinaryFileOps
{

     //  FileOperations (taken from: https://www.codeproject.com/Articles/17716/Insert-Text-into-Existing-Files-in-C-Without-Temp)

    public const int KB = 1024;
    public const int MB = 1024 * 1024;
    public const int GB = 1024 * 1024 * 1024;


    public static int GetTotalSystemMemory()
    {
        return SystemInfo.systemMemorySize;
    }
    

    //-----------------------------------------------------------------------------------------------------------------

    //  from http://quazistax.blogspot.com/2010/03/insert-delete-space-at-any-place-in.html

    [ThreadStatic]
    private static byte[] fileBuf = new byte[128 * 1024];

    private static void SimpleCopyFilePart(FileStream f, long from, long to, int length)
    {
        f.Position = from;
        f.Read(fileBuf, 0, length);
        f.Position = to;
        f.Write(fileBuf, 0, length);
    }

    public static void CopyFilePart(string file, long fromPos, long toPos, long length)
    {
        using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            CopyFilePart(f, fromPos, toPos, length);
    }

    public static void CopyFilePart(FileStream f, long fromPos, long toPos, long length)
    {
        lock (fileBuf)
        {
            int bufSize = fileBuf.Length;
            if (toPos > fromPos)
            {
                if (toPos + length > f.Length)
                    throw new ArgumentOutOfRangeException("toPos + length", "Destination range is out of file.");

                long i_to = toPos + length - bufSize;
                long i_from = fromPos + length - bufSize;
                for (long i = length / (long)bufSize; i > 0; --i, i_from -= bufSize, i_to -= bufSize)
                    SimpleCopyFilePart(f, i_from, i_to, bufSize);

                int leftover = (int)(length % (long)bufSize);
                if (leftover > 0)
                    SimpleCopyFilePart(f, fromPos, toPos, leftover);

            }
            else
            {
                if (fromPos + length > f.Length)
                    throw new ArgumentOutOfRangeException("fromPos + length", "Source range is out of file.");

                long i_to = toPos;
                long i_from = fromPos;
                for (long i = length / (long)bufSize; i > 0; --i, i_from += bufSize, i_to += bufSize)
                    SimpleCopyFilePart(f, i_from, i_to, bufSize);

                int leftover = (int)(length % (long)bufSize);
                if (leftover > 0)
                    SimpleCopyFilePart(f, i_from, i_to, leftover);
            }
        }
    }

    public static void DeleteFilePart(string file, long startPos, long length)
    {
        using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            DeleteFilePart(f, startPos, length);

    }

    public static void DeleteFilePart(FileStream f, long startPos, long length)
    {
        if (startPos + length > f.Length)
            throw new ArgumentOutOfRangeException("startPos + length", "Remove range is out of file.");

        long endPos = startPos + length;
        CopyFilePart(f, endPos, startPos, f.Length - endPos);
        f.SetLength(f.Length - length);
    }

    public static void InsertFilePart(string file, long startPos, long length)
    {
        using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            InsertFilePart(f, startPos, length);
    }

    public static void InsertFilePart(FileStream f, long startPos, long length)
    {
        if (startPos > f.Length)
            throw new ArgumentOutOfRangeException("startPos", "Insertion position is out of file.");

        long endPos = startPos + length;
        f.SetLength(f.Length + length);
        CopyFilePart(f, startPos, endPos, f.Length - endPos);
    }

    public static void FillFilePart(string file, long startPos, long length, byte fillByte)
    {
        using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Write))
            FillFilePart(f, startPos, length, fillByte);
    }

    public static void FillFilePart(FileStream f, long startPos, long length, byte fillByte)
    {
        if (startPos + length > f.Length)
            throw new ArgumentOutOfRangeException("startPos + length", "Fill range is out of file.");

        f.Position = startPos;
        for (long i = 0; i < length; ++i)
            f.WriteByte(fillByte);
    }


    //-----------------------------------------------------------------------------------------------------------------

    public static byte[] ReadBytes(string filename, long position, int len)
    {
        using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            BinaryReader rd = new BinaryReader(fs);
            rd.BaseStream.Seek(position, SeekOrigin.Begin);
            var result = rd.ReadBytes(len);
            rd.Close(); fs.Close();
            return result;
        }
    }
    public static byte[] ReadBytes(string filename, long position, int len, byte[] buffer)
    {   
        if(buffer == null || buffer.Length != len)
        {
            buffer = new byte[len];
        }
        using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            BinaryReader rd = new BinaryReader(fs);
            rd.BaseStream.Seek(position, SeekOrigin.Begin);
            rd.Read(buffer, 0, len);
            rd.Close(); fs.Close();
            return buffer;
        }
    }
    public static byte[] ReadNextBytes(BinaryReader reader, long position, int len)
    {
        reader.BaseStream.Seek(position, SeekOrigin.Begin);
        return reader.ReadBytes(len);
    }
    public static byte[] ReadNextBytes(BinaryReader reader, long position, int len, byte[] buffer)
    {
        if(buffer == null || buffer.Length != len)
        {
            buffer = new byte[len];
        }
        reader.BaseStream.Seek(position, SeekOrigin.Begin);
        reader.Read(buffer, 0, len);
        return buffer;
    }

    /// <summary>
    /// Writes bytes[] into <paramref name="file"/> at [position], overwriting existing contents
    /// </summary>
    /// <param name="bytes">Array of bytes to write into <paramref name="file"/></param>
    /// <param name="filename">Target file to be written to
    /// <param name="position">Position at which to begin writing
    public static void WriteBytes(byte[] bytes, string filename, long position)
    {
        using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            BinaryWriter bw = new BinaryWriter(fs);
            bw.BaseStream.Seek(position, SeekOrigin.Begin);
            bw.Write(bytes);
            bw.Close(); fs.Close();
        }
    }
    public static void WriteBytes(FileStream fs, byte[] bytes, long position, BinaryWriter bw=null)
    {
        if(fs != null && fs.CanWrite)
        {
            bool closeAfterWrite = bw == null;
            if(bw == null) bw = new BinaryWriter(fs);
            bw.BaseStream.Seek(position, SeekOrigin.Begin);
            bw.Write(bytes);
            if(closeAfterWrite)
                bw.Close();
        }
    }

    /// <summary>
    /// Wrapper for FileStream.SetLength().
    /// </summary>
    /// <remarks>
    /// When lengthening a file, this method appends null characters to it which 
    /// does NOT leave it in an XML-parseable state. After all your transpositions, 
    /// ALWAYS come back and truncate the file unless you've overwritten the 
    /// appended space with valid characters.
    /// </remarks>
    /// <param name="filename">Name of file to resize</param>
    /// <param name="len">New size of file</param>
    public static void SetFileLen(string filename, long len)
    {
        using (FileStream fsw = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            fsw.SetLength(len);
            fsw.Close();
        }
    }

    public static void SetFileLen(FileStream fs, long len)
    {
        if(fs != null && fs.CanWrite)
        {
            fs.SetLength(len);
        }
    }

    /// <summary>
    /// Writes bytes[] into file referred to by <paramref name="filename"/> at <paramref name="position"/>, 
    /// increasing length of file by length of inserted bytes.
    /// </summary>
    /// <param name="bytes">Array of bytes to insert into file</param>
    /// <param name="filename">Target file to receive <paramref name="bytes[]"/></param>
    /// <param name="position">Position in file at which to insert <paramref name="bytes[]"/></param>
    public static void InsertBytes(byte[] bytes, string filename, long position, bool setLength=true)
    {
        InsertBytes(bytes, filename, position, 32 * KB, setLength);

    }

    /// <summary>
    /// Identical to the other InsertBytes() overload, except it adds the bfrSz argument
    /// to force a specific read/write buffer size when Transpose or transposeReverse is 
    /// called. Also, removes the redundant length comparison logic in the other overload, 
    /// since Transpose() now supports that internally:
    /// </summary>
    public static void InsertBytes(byte[] bytes, string filename, long position, int bfrSz, bool setLength=true)
    {
        // Length of file before insert:
        long fileLen = new FileInfo(filename).Length;
        if(setLength)
        {
            // Extend the target file to accomodate bytes[]:
            SetFileLen(filename, fileLen + bytes.Length);
        }
        // Move the bytes after our insert position to make room for 
        // the bytes we're inserting, in one fell swoop:
        Transpose(filename, position, position + bytes.Length, fileLen - position, bfrSz);
        // Then insert the desired bytes and we're done:
        WriteBytes(bytes, filename, position);
    }
    public static void InsertBytes(FileStream fsw, byte[] bytes, long position, BinaryWriter writer=null)
    {
        InsertBytes(fsw, bytes, position, 32 * KB, writer);
    }
    public static void InsertBytes(FileStream fsw, byte[] bytes, long position, int bfrSz, BinaryWriter writer=null)
    {
        long fileLen = fsw.Length;
        Transpose(fsw, position, position + bytes.Length, fileLen - position, bfrSz, writer);
        WriteBytes(fsw, bytes, position, writer);
    }


    /// <summary>
    /// Inserts bytes into a file while avoiding any external memory or disk buffers;
    /// when needed, the target file provides its own temp space:
    /// </summary>
    /// <param name="bytes">Bytes to be inserted</param>
    /// <param name="filename">Target file</param>
    /// <param name="position">Insertion position</param>
    public static void InsertBytesUsingEOFTemp(byte[] bytes, string filename, long position)
    {
        long fileLen = new FileInfo(filename).Length;
        long suffixLen = fileLen - position;
        long suffixTempPosition;
        long tempLen;
        // Is the Inserted text inter or shorter than the right segment?

        long compare = suffixLen.CompareTo(bytes.Length);
        // If we're shifting the RH segment right by its own length or more, 
        // then we have it easy; shift it exactly enough to accomodate the
        // inserted bytes...
        if (compare < 0)
        {
            suffixTempPosition = position + bytes.Length;
            tempLen = (suffixTempPosition + suffixLen);
            SetFileLen(filename, tempLen);
            Transpose(filename, position, suffixTempPosition, suffixLen);
            WriteBytes(bytes, filename, position);
        }
        // Otherwise, if we're shifting the RH segment right by less than 
        // its own length, we'll encounter a write/read collision, so
        // we would need to preserve the RH segment by buffering [1]:
        else
        {
            suffixTempPosition = fileLen;
            tempLen = (fileLen + suffixLen);
            SetFileLen(filename, tempLen);
            Transpose(filename, position, suffixTempPosition, suffixLen);
            WriteBytes(bytes, filename, position);
            Transpose(filename, suffixTempPosition, position + bytes.Length, suffixLen);
            SetFileLen(filename, fileLen + bytes.Length);
        }
        // [1] See InsertBytes() and transposeReverse() for a more efficient approach;
    }

    /// <summary>
    /// Within <paramref name="filename"/>, moves a range of <paramref name="Len"/> bytes 
    /// starting at <paramref name="SourcePos"/> to <paramref name="DestPos"/>.
    /// </summary>
    /// <param name="filename">The target file</param>
    /// <param name="SourcePos">The starting position of the byte range to move</param>
    /// <param name="DestPos">The destination position of the byte range</param>
    /// <param name="Len">The number of bytes to move</param>
    public static void Transpose(string filename, long SourcePos, long DestPos, long Len)
    {
        // 32KB is consistently among the most efficient buffer sizes:
        Transpose(filename, SourcePos, DestPos, Len, 32 * KB);
    }

    /// <summary>
    /// Identical to Transpose(), but allows the caller to specify a read/write buffer
    /// size if transposeReverse is called:
    /// </summary>
    public static void Transpose(string filename, long SourcePos, long DestPos, long Len, int bfrSz)
    {
        if (DestPos > SourcePos && Len > (DestPos - SourcePos))
        {
            // Delegate work to transposeReverse, telling it to use a
            // specified read/write buffer size:
            transposeForward(filename, SourcePos, DestPos, Len, bfrSz);
        }
        else
        {
            __forceTranspose(filename, SourcePos, DestPos, Len);
        }
    }
    public static void Transpose(FileStream fsw, long SourcePos, long DestPos, long Len, BinaryWriter writer=null)
    {
        // 32KB is consistently among the most efficient buffer sizes:
        Transpose(fsw, SourcePos, DestPos, Len, 32 * KB, writer);
    }
    public static void Transpose(FileStream fsw, long SourcePos, long DestPos, long Len, int bfrSz, BinaryWriter writer=null)
    {
        if (DestPos > SourcePos && Len > (DestPos - SourcePos))
        {
            // Delegate work to transposeReverse, telling it to use a
            // specified read/write buffer size:
            transposeForward2(fsw, SourcePos, DestPos, Len, bfrSz);
        }
        else
        {
            __forceTranspose2(fsw, SourcePos, DestPos, Len, writer);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    private static void __forceTranspose(string filename, long SourcePos, long DestPos, long Len)
    {
        using (FileStream fsw = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            using (FileStream fsr = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fsr))
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        sr.BaseStream.Position = SourcePos;
                        bw.BaseStream.Seek(DestPos, SeekOrigin.Begin);
                        //bw.Seek(int.Parse(DestPos.ToString()), SeekOrigin.Begin);
                        for (long i = 0; i < Len; i++)
                        {
                            bw.Write((byte)sr.Read());
                        }
                        bw.Close();
                        sr.Close();
                    }
                }
            }
        }
    }
    private static void __forceTranspose2(FileStream fsw, long SourcePos, long DestPos, long Len, BinaryWriter writer=null)
    {
        using (FileStream fsr = new FileStream(fsw.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fsr))
            {
                if(writer != null)
                {
                    __transposeForcedWrite(sr, writer, SourcePos, DestPos, Len);
                    writer.Flush();
                }
                else
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        __transposeForcedWrite(sr, bw, SourcePos, DestPos, Len);
                        bw.Close();
                    }
                }
                sr.Close();
            }
        }
    }
    private static void __transposeForcedWrite(StreamReader sr, BinaryWriter bw, long SourcePos, long DestPos, long Len)
    {
        sr.BaseStream.Position = SourcePos;
        bw.BaseStream.Seek(DestPos, SeekOrigin.Begin);
        //bw.Seek(int.Parse(DestPos.ToString()), SeekOrigin.Begin);
        for (long i = 0; i < Len; i++)
        {
            bw.Write((byte)sr.Read());
        }
    }

    private static void transposeForward(string filename, long SourcePos, long DestPos, long Length, int bfrSz)
    {
        long distance = DestPos - SourcePos;
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException
                ("DestPos", "DestPos is less than SourcePos, and this method can only copy byte ranges to the right.\r\n" +
                "Use the public Transpose() method to copy a byte range to the left of itself.");
        }
        long readPos = SourcePos;// +Length;
        long writePos = DestPos;// +Length;
        bfrSz = bfrSz < 1 ? 32 * KB :
            (int)Math.Min(bfrSz, Length);
        // more than 40% of available memory poses a high risk of
        // OutOfMemoryExceptions when allocating 2x buffer, and
        // saps performance anyway:
        bfrSz=(int)Math.Min(bfrSz, (GetTotalSystemMemory() * .4));

        long numReads = Length / bfrSz;
        byte[] buff = new byte[bfrSz];
        byte[] buff2 = new byte[bfrSz];
        int remainingBytes = (int)Length % bfrSz;
        using (FileStream fsw = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            using (FileStream fsr = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fsr))
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
                        bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
                        // prime Buffer B:
                        sr.BaseStream.Read(buff2, 0, bfrSz);
                        for (long i = 1; i < numReads; i++)
                        {
                            buff2.CopyTo(buff,0);
                            sr.BaseStream.Read(buff2, 0, bfrSz);
                            bw.Write(buff, 0, bfrSz);                                
                                                            
                        }
                        buff2.CopyTo(buff,0);
                        if (remainingBytes > 0)
                        {
                            buff2 = new byte[remainingBytes];
                            sr.BaseStream.Read(buff2, 0, remainingBytes);
                            bw.Write(buff, 0, bfrSz);
                            bfrSz = remainingBytes;
                            buff = new byte[bfrSz];
                            buff2.CopyTo(buff,0);
                        }
                        bw.Write(buff, 0, bfrSz);
                        bw.Close();
                        sr.Close();
                        buff = null;
                        buff2 = null;
                    }
                }
            }
        }
        GC.Collect();
    }
    private static void transposeForward2(FileStream fsw, long SourcePos, long DestPos, long Length, int bfrSz, BinaryWriter writer=null)
    {
        long distance = DestPos - SourcePos;
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException
                ("DestPos", "DestPos is less than SourcePos, and this method can only copy byte ranges to the right.\r\n" +
                "Use the public Transpose() method to copy a byte range to the left of itself.");
        }
        long readPos = SourcePos;// +Length;
        long writePos = DestPos;// +Length;
        bfrSz = bfrSz < 1 ? 32 * KB :
            (int)Math.Min(bfrSz, Length);
        // more than 40% of available memory poses a high risk of
        // OutOfMemoryExceptions when allocating 2x buffer, and
        // saps performance anyway:
        bfrSz=(int)Math.Min(bfrSz, (GetTotalSystemMemory() * .4));

        long numReads = Length / bfrSz;
        byte[] buff = new byte[bfrSz];
        byte[] buff2 = new byte[bfrSz];
        int remainingBytes = (int)Length % bfrSz;
        using (FileStream fsr = new FileStream(fsw.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fsr))
            {
                if(writer != null)
                {
                    __transposeForwardWrite(sr, writer, readPos, writePos, remainingBytes, numReads, bfrSz, ref buff, ref buff2);
                    writer.Flush();
                }
                else
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        __transposeForwardWrite(sr, bw, readPos, writePos, remainingBytes, numReads, bfrSz, ref buff, ref buff2);
                        bw.Close();
                    }
                }
                sr.Close();
            }
        }
    }
    private static void __transposeForwardWrite(StreamReader sr, BinaryWriter bw, long readPos, long writePos, int remainingBytes, long numReads, int bfrSz, ref byte[] buff, ref byte[] buff2)
    {
        sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
        bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
        // prime Buffer B:
        sr.BaseStream.Read(buff2, 0, bfrSz);
        for (long i = 1; i < numReads; i++)
        {
            buff2.CopyTo(buff,0);
            sr.BaseStream.Read(buff2, 0, bfrSz);
            bw.Write(buff, 0, bfrSz);                                
                                            
        }
        buff2.CopyTo(buff,0);
        if (remainingBytes > 0)
        {
            buff2 = new byte[remainingBytes];
            sr.BaseStream.Read(buff2, 0, remainingBytes);
            bw.Write(buff, 0, bfrSz);
            bfrSz = remainingBytes;
            buff = new byte[bfrSz];
            buff2.CopyTo(buff,0);
        }
        bw.Write(buff, 0, bfrSz);
        buff = null;
        buff2 = null;
    }

    private static void transposeReverse(string filename, long SourcePos, long DestPos, long Length, int bfrSz)
    {
        long distance = DestPos - SourcePos;
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException
                ("DestPos", "DestPos is less than SourcePos, and this method can only copy byte ranges to the right.\r\n" +
                "Use the public Transpose() method to copy a byte range to the left of itself.");
        }
        long readPos = SourcePos + Length;
        long writePos = DestPos + Length;
        bfrSz = bfrSz < 1 ? (int)Math.Min(GetTotalSystemMemory() * .9, Length) : (int)Math.Min(bfrSz, Length);

        long numReads = Length / bfrSz;
        byte[] buff = new byte[bfrSz];
        int remainingBytes = (int)Length % bfrSz;
        using (FileStream fsw = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            using (FileStream fsr = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fsr))
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
                        bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
                        for (long i = 0; i < numReads; i++)
                        {
                            readPos -= bfrSz;
                            writePos -= bfrSz;
                            sr.DiscardBufferedData();
                            sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
                            sr.BaseStream.Read(buff, 0, bfrSz);
                            bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
                            bw.Write(buff, 0, bfrSz);
                        }
                        if (remainingBytes > 0)
                        {
                            bfrSz = remainingBytes;
                            readPos -= bfrSz;
                            writePos -= bfrSz;
                            sr.DiscardBufferedData();
                            sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
                            sr.BaseStream.Read(buff, 0, bfrSz);
                            bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
                            bw.Write(buff, 0, bfrSz);
                        }
                        bw.Close();
                        sr.Close();
                        buff = null;
                    }
                }
            }
        }
        GC.Collect();
    }
    private static void transposeReverse2(FileStream fsw, long SourcePos, long DestPos, long Length, int bfrSz, BinaryWriter writer=null)
    {
        if(fsw == null || !fsw.CanWrite)
        {
            return;
        }

        long distance = DestPos - SourcePos;
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException
                ("DestPos", "DestPos is less than SourcePos, and this method can only copy byte ranges to the right.\r\n" +
                "Use the public Transpose() method to copy a byte range to the left of itself.");
        }
        long readPos = SourcePos + Length;
        long writePos = DestPos + Length;
        bfrSz = bfrSz < 1 ? (int)Math.Min(GetTotalSystemMemory() * .9, Length) : (int)Math.Min(bfrSz, Length);

        long numReads = Length / bfrSz;
        byte[] buff = new byte[bfrSz];
        int remainingBytes = (int)Length % bfrSz;
        using (FileStream fsr = new FileStream(fsw.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fsr))
            {
                if(writer != null)
                {
                    __transposeReverseWrite(sr, writer, readPos, writePos, numReads, bfrSz, remainingBytes, ref buff);
                }
                else
                {
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        __transposeReverseWrite(sr, bw, readPos, writePos, numReads, bfrSz, remainingBytes, ref buff);
                        bw.Close();
                    }
                }
                sr.Close();
            }
        }
    }
    static void __transposeReverseWrite(StreamReader sr, BinaryWriter bw, long readPos, long writePos, long numReads, int bfrSz, int remainingBytes, ref byte[] buff)
    {
        sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
        bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
        for (long i = 0; i < numReads; i++)
        {
            readPos -= bfrSz;
            writePos -= bfrSz;
            sr.DiscardBufferedData();
            sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
            sr.BaseStream.Read(buff, 0, bfrSz);
            bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
            bw.Write(buff, 0, bfrSz);
        }
        if (remainingBytes > 0)
        {
            bfrSz = remainingBytes;
            readPos -= bfrSz;
            writePos -= bfrSz;
            sr.DiscardBufferedData();
            sr.BaseStream.Seek(readPos, SeekOrigin.Begin);
            sr.BaseStream.Read(buff, 0, bfrSz);
            bw.BaseStream.Seek(writePos, SeekOrigin.Begin);
            bw.Write(buff, 0, bfrSz);
        }
        buff = null;
    }

    

    //-----------------------------------------------------------------------------------------------------------------

    //  Text/XML 

    /// <summary>
    /// Overwrites <paramref name="length"/> bytes in <paramref name="filename"/> 
    /// with spaces, beginning at <paramref name="start"/>.
    /// </summary>
    /// <param name="filename">The target file</param>
    /// <param name="start">The position at which to begin writing spaces</param>
    /// <param name="length">How many spaces to write</param>
    public static void WriteSpaces(string filename, int start, int length)
    {
        using (FileStream fs = new FileStream
            (filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Seek(start, SeekOrigin.Begin);
            for (int i = 0; i < length; i++)
            {
                bw.Write(" ");
            }
            bw.Flush(); bw.Close(); fs.Close();
        }
    }

    /// <summary>
    /// Grab the desired number of bytes from the beginning of a file;
    /// useful, e.g. for files too large to open in Notepad.
    /// </summary>
    /// <param name="filename">Target file</param>
    /// <param name="lines">Number of lines to grab</param>
    /// <returns>First n lines from the file</returns>
    public static string Head(string filename, int bytes)
    {
        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            char[] buffer = (char[])Array.CreateInstance(typeof(char), bytes); 
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(fs))
            {
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                sr.ReadBlock(buffer, 0, buffer.Length);
                return new string(buffer);
            }
        }
    }

    /// <summary>
    /// Grab the desired number of kilobytes from the end of a file;
    /// useful, e.g. for files too large to open in Notepad.
    /// </summary>
    /// <param name="filename">Target file</param>
    /// <param name="kb">Number of kilobytes to grab</param>
    /// <returns>Last kb bytes from the file</returns>
    public static string Tail(string filename, int bytes)
    {
        using (FileStream fs = new FileStream(filename,FileMode.Open,FileAccess.Read))
        {
            char[] buffer = (char[])Array.CreateInstance(typeof(char), bytes);
            string txt;
            using (StreamReader sr = new StreamReader(fs))
            {
                sr.BaseStream.Seek((-1024 * bytes), SeekOrigin.End);
                sr.ReadBlock(buffer, 0, buffer.Length);
                txt = new string(buffer);
                sr.Close(); fs.Close(); 
            }
            return txt;
        }
    }

    /// <summary>
    /// Returns the position of the first occurrence of <paramref name="FindWhat"/> 
    /// within <paramref name="InStream"/>, or -1 if <paramref name="FindWhat"/> is
    /// not found.
    /// </summary>
    /// <param name="FindWhat">The string being sought</param>
    /// <param name="InStream">The stream in which to search (must be readable & seekable)</param>
    /// <returns>The position of the first occurrence of <paramref name="FindWhat"/> 
    /// within <paramref name="InStream"/>, or -1 if <paramref name="FindWhat"/> is
    /// not found
    /// </returns>
    public static int Find(string FindWhat, Stream InStream)
    {
        // TODO: Investigate performance optimizations using a smart string-search
        // algorithm, like Boyer-Moore, Knuth-Morris-Pratt, etc. Automate choice of
        // brute force vs. smart algorithm; Optionally, run performance tests & save 
        // results to a configuration file indicating, e.g., where the tradeoff between
        // algorithms would be for a given length of FindWhat & file size.
        int streamPos = 0;
        int findPos;
        bool found = true;
        char findChar;
        StreamReader sr = new StreamReader(InStream);
        sr.BaseStream.Seek(0, SeekOrigin.Begin);
        // Outer loop for entire file stream reader...
        while (sr.Peek() >= 0)
        {
            findPos = 0;
            findChar = Convert.ToChar(FindWhat.Substring(findPos, 1));
            found = findChar == (char)sr.Read();
            // Per MSDN:
            // "StreamReader might buffer input such that the position of the
            //  underlying stream will not match the StreamReader position."
            //  Since sr.BaseStream.Position is not an accurate indicator
            //  for determining streamPos, we'll track it ourselves...
            streamPos += 1;
            findPos += 1;
            // Inner loop for comparing findwhat to candidate 
            //  when we hit a potential match...
            while (found)
            {
                while (findPos <= FindWhat.Length)
                {
                    findChar = Convert.ToChar(FindWhat.Substring(findPos, 1));
                    found = findChar == (char)sr.Read();
                    if (!found)
                        break;
                    streamPos += 1;
                    findPos += 1;
                    if (findPos == FindWhat.Length) return streamPos - findPos;
                }
            }
        }
        // No luck finding it?
        return -1;
    }

    /// <summary>
    /// Experimental; researching various approaches to quickly validating
    /// very large XML files, avoiding XmlDocument and XmlSchema instances
    /// </summary>
    /// <param name="filename">The XML file to be validated</param>
    /// <returns>True if valid, False if not. Capiche?</returns>
    public static bool IsValidXmlFile(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Open))
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationEventHandler += new ValidationEventHandler(_validationHandler);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema;
            XmlReader xr = XmlReader.Create(stream, settings);
            while (xr.Read())
            { }//do nothing, just read; if there's a validation error, it'll hit the callback.
            xr.Close(); stream.Close();
            return _validationErrorsCount == 0;
        }
    }

    private static int _validationErrorsCount;// = 0;
    private static void _validationHandler(object sender, ValidationEventArgs args)
    {
        if (args.Severity != XmlSeverityType.Warning &&
            args.Exception.Message.IndexOf
                ("An element or attribute information item has already been validated from the '' namespace")
                < 0)
        {
            Console.WriteLine(args.Exception.ToString());
            _validationErrorsCount++;
        }
    }
}