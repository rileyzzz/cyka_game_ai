﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Threading.Tasks;

namespace SharpNeat.EvolutionAlgorithm;

/// <summary>
/// Represents a generational evolution algorithm.
/// </summary>
public interface IEvolutionAlgorithm
{
    /// <summary>
    /// Gets the current evolution algorithm statistics.
    /// </summary>
    EvolutionAlgorithmStatistics Stats { get; }

    /// <summary>
    /// Initialise the evolutionary algorithm.
    /// </summary>
    Task Initialise();

    /// <summary>
    /// Perform one generation of the evolutionary algorithm.
    /// </summary>
    Task PerformOneGeneration();
}
