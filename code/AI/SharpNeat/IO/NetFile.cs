// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
//using System.Text;
using SharpNeat.IO.Models;

namespace SharpNeat.IO;

/// <summary>
/// Static methods for loading and saving instances of <see cref="NetFileModel"/> from and to a file.
/// </summary>
public static class NetFile
{
    // static readonly System.Text.Encoding __utf8Encoding = new System.Text.UTF8Encoding(false, true);

    /// <summary>
    /// Load a <see cref="NetFileModel"/> instance from a 'net' format file.
    /// </summary>
    /// <param name="filepath">The name and path of the file to load.</param>
    /// <returns>A new instance of <see cref="NetFileModel"/>.</returns>
    public static NetFileModel Load(string filepath)
    {
        ArgumentNullException.ThrowIfNull(filepath);
		using var fs = Sandbox.FileSystem.Data.OpenRead( filepath );
		using var sr = new StreamReader(fs);
        NetFileModel netFileModel = NetFileReader.Read(sr);
        return netFileModel;
    }

    /// <summary>
    /// Load a <see cref="NetFileModel"/> instance from a 'net' format file.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A new instance of <see cref="NetFileModel"/>.</returns>
    public static NetFileModel Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var sr = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, true);
        NetFileModel netFileModel = NetFileReader.Read(sr);
        return netFileModel;
    }

    /// <summary>
    /// Save a <see cref="NetFileModel"/> instance to file using 'net' file format.
    /// </summary>
    /// <param name="model">The <see cref="NetFileModel"/> to save.</param>
    /// <param name="filepath">The name and path of the file to save to.</param>
    public static void Save(
        NetFileModel model, string filepath)
    {
		using var fs = Sandbox.FileSystem.Data.OpenWrite( filepath );
        //using var sw = new StreamWriter(filepath, false, System.Text.Encoding.UTF8 );
        using var sw = new StringWriter( /*, System.Text.Encoding.UTF8*/ );
        NetFileWriter.Write(model, sw);
		fs.Write( System.Text.Encoding.UTF8.GetBytes( sw.ToString() ) );
	}

    /// <summary>
    /// Save a <see cref="NetFileModel"/> instance to a stream using 'net' file format.
    /// </summary>
    /// <param name="model">The <see cref="NetFileModel"/> to save.</param>
    /// <param name="stream">The stream to save to.</param>
    public static void Save(
        NetFileModel model, Stream stream)
    {
		//using var sw = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, true);
		using var sw = new StringWriter( /*, System.Text.Encoding.UTF8*/ );
		NetFileWriter.Write(model, sw);
		stream.Write( System.Text.Encoding.UTF8.GetBytes( sw.ToString() ) );
	}
}
