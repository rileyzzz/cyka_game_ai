﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Buffers;
using System.Diagnostics;

namespace SharpNeat.Graphs;

/// <summary>
/// Represents the connections in a directed graph.
/// </summary>
public readonly struct ConnectionIds : IDisposable
{
    private readonly int _length;

    // Array of source and target connection IDs. The source IDs start at position zero; the target IDs start
    // after the last source ID. Allocating a single array for both sets of IDs allows to perform a single
    // call to ArrayPool.Rent() instead of two. The two sets of IDs are also guaranteed to be adjacent to each
    // other in RAM, and this improves performance in some scenarios, i.e., depending of CPU cache architecture
    // and distance between the a source and target ID for the same connection.
    private readonly int[] _idArr;

    /// <summary>
    /// Construct a new instance.
    /// </summary>
    /// <param name="length">The number of connections to allocate memory for.</param>
    public ConnectionIds(int length)
    {
        // Store the number of the connections, and rent an array that has length of at least 2 * length.
        _length = length;
        _idArr = ArrayPool<int>.Shared.Rent(length << 1);
    }

    /// <summary>
    /// Gets the number of connections.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Get a span over the connection source IDs.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/>"/>.</returns>
    public Span<int> GetSourceIdSpan() => _idArr.AsSpan(0, _length);

    /// <summary>
    /// Get a span over the connection target IDs.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/>"/>.</returns>
    public Span<int> GetTargetIdSpan() => _idArr.AsSpan(_length, _length);

    /// <summary>
    /// Get the source ID for the specified connection.
    /// </summary>
    /// <param name="connIdx">Connection index.</param>
    /// <returns>Source ID.</returns>
    public ref int GetSourceId(int connIdx)
    {
        Sandbox.Diagnostics.Assert.True(connIdx >= 0 && connIdx < _length);
        return ref _idArr[connIdx];
    }

    /// <summary>
    /// Get the target ID for the specified connection.
    /// </summary>
    /// <param name="connIdx">Connection index.</param>
    /// <returns>Source ID.</returns>
    public ref int GetTargetId(int connIdx)
    {
        Sandbox.Diagnostics.Assert.True(connIdx >= 0 && connIdx < _length);
        return ref _idArr[_length + connIdx];
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ArrayPool<int>.Shared.Return(_idArr);
    }
}
