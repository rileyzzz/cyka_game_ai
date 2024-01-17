﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
namespace SharpNeat.Neat.Reproduction.Sexual.Strategy;

/// <summary>
/// Represents a NEAT genome sexual reproduction strategy.
/// </summary>
/// <typeparam name="T">Connection weight data type.</typeparam>
public interface ISexualReproductionStrategy<T>
    where T : struct
{
    /// <summary>
    /// Create a new child genome based on the genetic content of two parent genome.
    /// </summary>
    /// <param name="parent1">Parent 1.</param>
    /// <param name="parent2">Parent 2.</param>
    /// <param name="rng">Random source.</param>
    /// <returns>A new child genome.</returns>
    NeatGenome<T> CreateGenome(NeatGenome<T> parent1, NeatGenome<T> parent2, IRandomSource rng);
}
