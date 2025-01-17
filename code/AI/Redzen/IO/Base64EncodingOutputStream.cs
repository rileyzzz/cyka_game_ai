﻿//// This file is part of the Redzen code library; Copyright Colin D. Green.
//// See LICENSE.txt for details.
//using System.Text;

//namespace Redzen.IO;

///// <summary>
///// An output stream that accepts calls to write binary data into the stream, encodes the data into base64 characters,
///// encodes those characters using ASCII single byte encoding (i.e. compatible with ASCII and UTF-8), and writes the encoded
///// characters into an inner output stream.
///// </summary>
///// <remarks>
///// An encoding must be supplied at construction time and only ASCII and UTF-8 are accepted, this is because this class implements
///// its own lightweight single byte character encoding for improved performance when working with UTF-8 and ASCII. I.e. the encoding
///// on the constructor makes it clear what encodings this class supports, but the provided Encoding object is not actually used to
///// perform the character encoding.
///// </remarks>
//public sealed class Base64EncodingOutputStream : Stream
//{
//    #region Consts / Statics

//    const int __utf8CodePage = 65001;
//    const int __asciiCodePage = 20127;
//    const byte __paddingChar = 0x3d; // '=' padding char; ASCII encoding.

//    // The base64 encoding table. This is the below table of characters expressed as their ASCII single byte
//    // encodings, which happen to have the same code point in unicode, hence ASCII and UTF-8 encode to the same
//    // byte sequences when dealing with base64 characters only.
//    //
//    // 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
//    // 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
//    // 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
//    // 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
//    static readonly byte[] __base64Table =
//    {
//        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
//        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
//        0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76,
//        0x77, 0x78, 0x79, 0x7A, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x2B, 0x2F
//    };

//    #endregion

//    #region Instance Fields

//    // Inner output stream.
//    readonly Stream _innerStream;
//    readonly bool _leaveOpen;
//    bool _isOpen = true;

//    // Buffered bytes, either zero, one or two bytes. Contains left-over bytes when not enough bytes are available for a 3 byte base64 block.
//    readonly byte[] _buf = new byte[2];
//    int _bufCount;

//    #endregion

//    #region Constructor

//    /// <summary>
//    /// Construct a new instance.
//    /// </summary>
//    /// <param name="innerOutputStream">The inner output stream to write base64 encoded (and character encoded) bytes into.</param>
//    /// <param name="encoding">The character encoding to use for the base64 characters (only base64/ASCII compatible encodings are supported).</param>
//    /// <param name="leaveOpen">Indicates if the inner stream is left open upon disposal of this base64 stream object.</param>
//    public Base64EncodingOutputStream(
//        Stream innerOutputStream, Encoding encoding, bool leaveOpen = true)
//    {
//        ArgumentNullException.ThrowIfNull(innerOutputStream);
//        ArgumentNullException.ThrowIfNull(encoding);

//        if(!innerOutputStream.CanWrite)
//            throw new ArgumentException("Inner stream cannot be written to.", nameof(innerOutputStream));

//        // Note. This class implements its own character encoding, which is compatible with any encoding
//        // that represents the base64 characters as single bytes as described in __base64Table.
//        if(!(encoding.CodePage == __utf8CodePage || encoding.CodePage == __asciiCodePage))
//            throw new ArgumentException("This class supports UTF-8 and ASCII text encodings only.", nameof(encoding));

//        _innerStream = innerOutputStream;
//        _leaveOpen = leaveOpen;
//    }

//    #endregion

//    #region Properties

//    /// <inheritdoc/>
//    public override bool CanRead => false;

//    /// <inheritdoc/>
//    public override bool CanSeek => false;

//    /// <inheritdoc/>
//    public override bool CanWrite => _isOpen;

//    /// <inheritdoc/>
//    public override long Length => throw new NotImplementedException();

//    /// <inheritdoc/>
//    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//    #endregion

//    #region Public Methods

//    /// <inheritdoc/>
//    public override void Write(ReadOnlySpan<byte> buffer)
//    {
//        ObjectDisposedException.ThrowIf(!_isOpen, this);

//        // Fast exit test.
//        if(buffer.Length == 0)
//            return;

//        // Alloc temp storage on the stack for a single base64 block of 3 input bytes, and the corresponding four base64
//        // encoded characters (UTF8 single byte encoded).
//        Span<byte> inBytes = stackalloc byte[3];
//        Span<byte> outChars = stackalloc byte[4];

//        // Calc how many bytes are ready to be encoded.
//        int total = _bufCount + buffer.Length;

//        // If not enough bytes for a block then store them in _buf and exit.
//        if(total < 3)
//        {
//            for(int i=0; i < buffer.Length; i++)
//                _buf[_bufCount++] = buffer[i];
//            return;
//        }

//        int offset = 0;
//        int count = buffer.Length;

//        // If there are buffered bytes then use them in the first block.
//        if(_bufCount != 0)
//        {
//            // Form a 3 byte block to encode.
//            switch(_bufCount)
//            {
//                case 1:
//                    // Form the block.
//                    inBytes[0] = _buf[0];
//                    inBytes[1] = buffer[0];
//                    inBytes[2] = buffer[1];

//                    // Update buffer offset and count.
//                    offset += 2;
//                    count -= 2;
//                    break;
//                case 2:
//                    // Form the block.
//                    inBytes[0] = _buf[0];
//                    inBytes[1] = _buf[1];
//                    inBytes[2] = buffer[0];

//                    // Update buffer offset and count.
//                    offset++;
//                    count--;
//                    break;
//            }

//            // Encode the block.
//            EncodeBlock(inBytes, outChars);

//            // Write the encoded block to the inner stream.
//            _innerStream.Write(outChars);

//            // Reset the buffered byte count.
//            _bufCount = 0;
//        }

//        // Loop over 3 byte blocks in the input buffer.
//        int idx = 0;
//        for(; idx < count-2; idx += 3)
//        {
//            // Get a span over the block bytes.
//            var blockSpan = buffer.Slice(offset + idx, 3);

//            // Encode the block.
//            EncodeBlock(blockSpan, outChars);

//            // Write the encoded block to the inner stream.
//            _innerStream.Write(outChars);
//        }

//        // Store left-over bytes if not enough for a 3 byte block.
//        int remainCount = count - idx;
//        switch(remainCount)
//        {
//            case 1:
//                _buf[0] = buffer[offset + idx];
//                _bufCount = 1;
//                break;

//            case 2:
//                _buf[0] = buffer[offset + idx];
//                _buf[1] = buffer[offset + idx + 1];
//                _bufCount = 2;
//                break;
//        }
//    }

//    /// <inheritdoc/>
//    public override void Write(byte[] buffer, int offset, int count)
//    {
//        ArgumentNullException.ThrowIfNull(buffer);

//        Write(buffer.AsSpan(offset, count));
//    }

//    /// <inheritdoc/>
//    public override void Flush()
//    {
//        // Note. Any buffered bytes in _buf are there because we don't have enough bytes to write a base64 block, unless
//        // we know for sure we are at the end of the data stream, which is not the case here. Hence we just request the
//        // inner stream to flush any buffered data it has.
//        _innerStream.Flush();
//    }

//    /// <summary>
//    /// Not implemented.
//    /// </summary>
//    /// <param name="buffer">Buffer.</param>
//    /// <param name="offset">Offset.</param>
//    /// <param name="count">Count.</param>
//    /// <returns>Read count.</returns>
//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        throw new NotImplementedException();
//    }

//    /// <summary>
//    /// Not implemented.
//    /// </summary>
//    /// <param name="offset">Offset.</param>
//    /// <param name="origin">Origin.</param>
//    /// <returns>Position.</returns>
//    public override long Seek(long offset, SeekOrigin origin)
//    {
//        throw new NotImplementedException();
//    }

//    /// <summary>
//    /// Not implemented.
//    /// </summary>
//    /// <param name="value">Length.</param>
//    public override void SetLength(long value)
//    {
//        throw new NotImplementedException();
//    }

//    /// <inheritdoc/>
//    protected override void Dispose(bool disposing)
//    {
//        try
//        {
//            if(disposing)
//            {
//                FlushComplete();

//                if(!_leaveOpen)
//                    _innerStream.Close();
//            }
//        }
//        finally
//        {
//            _isOpen = false;
//            base.Dispose(disposing);
//        }
//    }

//    #endregion

//    #region Private Methods

//    private void FlushComplete()
//    {
//        // Fast exit test.
//        if(_bufCount == 0)
//            return;

//        // Alloc temp storage on the stack for a single base64 block of 3 input bytes, and the corresponding four
//        // base64 encoded characters (UTF8 single byte encoded).
//        // Note. SkipLocalsInit is enabled on the assembly, therefore these stacks are not guaranteed to contain
//        // zeros, hence the explicit setting of zero value in the switch statement below.
//        Span<byte> inBytes = stackalloc byte[3];
//        Span<byte> outChars = stackalloc byte[4];

//        // Encode a full block, but replace the trailing zeros in the encoding output with the padding character, to indicate
//        // how much of the block is actual data and how much is just padding.
//        switch(_bufCount)
//        {
//            case 1:
//                inBytes[0] = _buf[0];
//                inBytes[1] = 0;
//                inBytes[2] = 0;
//                EncodeBlock(inBytes, outChars);
//                outChars[2] = __paddingChar;
//                outChars[3] = __paddingChar;
//                break;

//            case 2:
//                inBytes[0] = _buf[0];
//                inBytes[1] = _buf[1];
//                inBytes[2] = 0;
//                EncodeBlock(inBytes, outChars);
//                outChars[3] = __paddingChar;
//                break;
//        }

//        // Write the encoded block to the inner stream.
//        _innerStream.Write(outChars);
//    }

//    #endregion

//    #region Private Static Methods

//    /// <summary>
//    /// Encode a block of 3 input bytes to 4 output characters (encoded to single bytes).
//    /// </summary>
//    /// <param name="inBytes">The block of 3 bytes to encode.</param>
//    /// <param name="outChars">A span to write the base64 encoded bytes into (must be length 4).</param>
//    private static void EncodeBlock(ReadOnlySpan<byte> inBytes, Span<byte> outChars)
//    {
//        outChars[0] = __base64Table[(inBytes[0] & 0xfc) >> 2];
//        outChars[1] = __base64Table[((inBytes[0] & 0x03) << 4) | ((inBytes[1] & 0xf0) >> 4)];
//        outChars[2] = __base64Table[((inBytes[1] & 0x0f) << 2) | ((inBytes[2] & 0xc0) >> 6)];
//        outChars[3] = __base64Table[inBytes[2] & 0x3f];
//    }

//    #endregion
//}
