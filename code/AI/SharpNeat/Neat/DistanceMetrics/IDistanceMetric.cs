﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
namespace SharpNeat.Neat.DistanceMetrics;

/// <summary>
/// Represents a metric for measuring the distance between two genome positions in an encoding space, and thus,
/// in principle, the compatibility of the two genomes with respect to the probability of creating fit offspring.
/// </summary>
/// <typeparam name="T">Coordinate component data type.</typeparam>
public interface IDistanceMetric<T>
    where T : struct
{
    /// <summary>
    /// Calculates the distance between two positions.
    /// </summary>
    /// <param name="p1">Position one.</param>
    /// <param name="p2">Position two.</param>
    /// <returns>The distance between <paramref name="p1"/> and <paramref name="p2"/>.</returns>
    double CalcDistance(ConnectionGenes<T> p1, ConnectionGenes<T> p2);

    /// <summary>
    /// Tests if the distance between two positions is less than some threshold.
    /// </summary>
    /// <param name="p1">Position one.</param>
    /// <param name="p2">Position two.</param>
    /// <param name="threshold">Distance threshold.</param>
    /// <returns>
    /// True if the distance between <paramref name="p1"/> and <paramref name="p2"/> is less than
    /// <paramref name="threshold"/>.
    /// </returns>
    bool TestDistance(ConnectionGenes<T> p1, ConnectionGenes<T> p2, double threshold);

    /// <summary>
    /// Calculates the centroid for the given set of points.
    /// </summary>
    /// <param name="points">The set of points.</param>
    /// <returns>A new instance of <see cref="ConnectionGenes{T}"/>.</returns>
    /// <remarks>
    /// The centroid is a central position within a set of points that minimizes the sum of the squared distance
    /// between each of those points and the centroid. As such it can also be thought of as being representative
    /// of the set of points.
    ///
    /// The centroid calculation is dependent on the distance metric, hence this method is defined on
    /// <see cref="IDistanceMetric{T}"/>. For some distance metrics the centroid may not be a unique point, in
    /// those cases one of the possible centroids is returned.
    ///
    /// A centroid is used in k-means clustering to define the centre of a cluster.
    /// </remarks>
    ConnectionGenes<T> CalculateCentroid(ReadOnlySpan<ConnectionGenes<T>> points);
}
