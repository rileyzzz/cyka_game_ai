﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
using Redzen.Numerics.Distributions;
using SharpNeat.Neat.Reproduction.Asexual.WeightMutation.Selection;

namespace SharpNeat.Neat.Reproduction.Asexual.WeightMutation;

/// <summary>
/// A connection weight mutation strategy that resets connection weights.
/// </summary>
/// <typeparam name="T">Connection weight data type.</typeparam>
public sealed class ResetWeightMutationStrategy<T> : IWeightMutationStrategy<T>
    where T : struct, IBinaryFloatingPointIeee754<T>
{
    readonly ISubsetSelectionStrategy _selectionStrategy;
    readonly IStatelessSampler<T> _weightSampler;

    /// <summary>
    /// Construct with the given selection strategy and weight sampler.
    /// </summary>
    /// <param name="selectionStrategy">Weight selection strategy.</param>
    /// <param name="weightSampler">Weight sampler.</param>
    public ResetWeightMutationStrategy(
        ISubsetSelectionStrategy selectionStrategy,
        IStatelessSampler<T> weightSampler)
    {
        _selectionStrategy = selectionStrategy;
        _weightSampler = weightSampler;
    }

    #region Public Methods

    /// <inheritdoc/>
    public void Invoke(T[] weightArr, IRandomSource rng)
    {
        // Select a subset of connection genes to mutate.
        int[] selectedIdxArr = _selectionStrategy.SelectSubset(weightArr.Length, rng);

        // Loop over the connection genes to be mutated, and mutate them.
        for(int i=0; i < selectedIdxArr.Length; i++)
            weightArr[selectedIdxArr[i]] = _weightSampler.Sample(rng);
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Create a weight mutation strategy that replaces a random subset of weights, with new weights sampled
    /// from a uniform distribution.
    /// </summary>
    /// <param name="selectionStrategy">Weight selection strategy.</param>
    /// <param name="weightScale">The uniform distribution scale.</param>
    /// <returns>A new instance of <see cref="ResetWeightMutationStrategy{T}"/>.</returns>
    public static ResetWeightMutationStrategy<T> CreateUniformResetStrategy(
        ISubsetSelectionStrategy selectionStrategy,
        double weightScale)
    {
        var sampler = (IStatelessSampler<T>)(object)UniformDistributionSamplerFactory.CreateStatelessSampler<T>(
            weightScale, true);

        return new ResetWeightMutationStrategy<T>(selectionStrategy, sampler);
    }

    // TODO: Consider Laplacian distribution.

    /// <summary>
    /// Create a weight mutation strategy that replaces a random subset of weights, with new weights sampled
    /// from a Gaussian distribution.
    /// </summary>
    /// <param name="selectionStrategy">Weight selection strategy.</param>
    /// <param name="stdDev">Gaussian standard deviation.</param>
    /// <returns>A new instance of <see cref="ResetWeightMutationStrategy{T}"/>.</returns>
    public static ResetWeightMutationStrategy<T> CreateGaussianResetStrategy(
        ISubsetSelectionStrategy selectionStrategy,
        double stdDev)
    {
        var sampler = GaussianDistributionSamplerFactory.CreateStatelessSampler<double>(
			0.0, stdDev);

        return new ResetWeightMutationStrategy<T>(selectionStrategy, (IStatelessSampler<T>)(object)sampler);
    }

    #endregion
}
