﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
namespace SharpNeat.Graphs;

/// <summary>
/// An <see cref="IComparer{T}"/> for comparing instances of <see cref="WeightedDirectedConnection{T}"/>.
/// </summary>
/// <typeparam name="T">Connection weight numeric data type.</typeparam>
public sealed class WeightedDirectedConnectionComparer<T> : IComparer<WeightedDirectedConnection<T>>
    where T : struct
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly WeightedDirectedConnectionComparer<T> Default = new();

    /// <summary>
    /// Compares two instances of <see cref="WeightedDirectedConnection{T}"/> and returns a value indicating
    /// whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
    public int Compare(
        WeightedDirectedConnection<T> x,
        WeightedDirectedConnection<T> y)
    {
        // Compare source IDs.
        if(x.SourceId < y.SourceId)
            return -1;

        if(x.SourceId > y.SourceId)
            return 1;

        // Source IDs are equal; compare target IDs.
        if(x.TargetId < y.TargetId)
            return -1;

        if(x.TargetId > y.TargetId)
            return 1;

        return 0;
    }
}
