﻿//// This file is part of the Redzen code library; Copyright Colin D. Green.
//// See LICENSE.txt for details.
//namespace Redzen.IO;

//#pragma warning disable CA2201 // Do not raise reserved exception types

///// <summary>
///// A byte array backed by a file on disk.
/////
///// The byte array has fixed length and random accessible as per a normal byte array, but is backed by a
///// file rather than memory.
///// </summary>
//public sealed class FileByteArray : IDisposable
//{
//    readonly string _filePath;
//    readonly FileStream _fileStream;
//    readonly int _length;

//    #region Constructors

//    /// <summary>
//    /// Open an existing byte array file.
//    /// </summary>
//    /// <param name="filePath">The full path to the file.</param>
//    public FileByteArray(string filePath)
//    {
//        // Open file.
//        _filePath = filePath;
//        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

//        // Read file/array length (in bytes).
//        _length = (int)_fileStream.Length;
//    }

//    /// <summary>
//    /// Create a new byte array file with the specified length (in bytes).
//    /// </summary>
//    /// <param name="filePath">The full path to the file.</param>
//    /// <param name="length">The length of the byte array.</param>
//    public FileByteArray(string filePath, int length)
//    {
//        // Open file.
//        _filePath = filePath;
//        _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

//        // Init file (write zeros).
//        if(length <= 16_384)
//        {
//            // For small arrays we can initialise the whole array in one operation.
//            byte[] buf = new byte[length];
//            _fileStream.Write(buf, 0, length);
//        }
//        else
//        {
//            // For larger arrays we avoid allocating a large buffer array (which would likely be allocated on
//            // the Large Object Heap), and write the file in 80KiB (80 kibibyte) blocks instead.
//            byte[] buf = new byte[16_384];

//            int remainingBytes = length;
//            while(remainingBytes > buf.Length)
//            {
//                _fileStream.Write(buf, 0, length);
//                remainingBytes -= buf.Length;
//            }

//            if(remainingBytes > 0)
//                _fileStream.Write(buf, 0, remainingBytes);
//        }

//        _length = length;
//    }

//    #endregion

//    #region Properties / Indexer

//    /// <summary>
//    /// Gets the full file path of the file that is backing the byte array.
//    /// </summary>
//    public string FilePath => _filePath;

//    /// <summary>
//    /// Gets the length of the array.
//    /// </summary>
//    /// <value>The length of the array.</value>
//    public int Length => _length;

//    /// <summary>
//    /// Gets the byte at the specified index within the array.
//    /// </summary>
//    /// <param name="idx">The index of the byte to retrieve from the array.</param>
//    /// <returns>The byte at the specified index.</returns>
//    public byte this[int idx]
//    {
//        get
//        {
//            if(idx >= _length)
//                throw new IndexOutOfRangeException();

//            _fileStream.Seek(idx, SeekOrigin.Begin);
//            return (byte)_fileStream.ReadByte();
//        }

//        set
//        {
//            if(idx >= _length)
//                throw new IndexOutOfRangeException();

//            _fileStream.Seek(idx, SeekOrigin.Begin);
//            _fileStream.WriteByte(value);
//        }
//    }

//    #endregion

//    #region IDisposable

//    /// <inheritdoc/>
//    public void Dispose()
//    {
//        _fileStream.Close();
//    }

//    #endregion
//}
