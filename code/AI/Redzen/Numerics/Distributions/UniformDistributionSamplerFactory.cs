// This file is part of the Redzen code library; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
using Redzen.Random;

namespace Redzen.Numerics.Distributions;

/// <summary>
/// Static factory methods for creating instances of ISampler{T} and IStatelessSampler{T} for sampling from uniform distributions.
/// </summary>
public static class UniformDistributionSamplerFactory
{
    #region Public Static Methods [ISampler Factory Methods]

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,1).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>()
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource();
        return CreateSampler<double>( 1.0, false, rng );
    }

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,1).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="seed">Random source seed.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(ulong seed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource(seed);
        return CreateSampler<double>( 1.0, false, rng );
    }

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,1).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="rng">Random source.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(IRandomSource rng)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        return CreateSampler<double>( 1.0, false, rng);
    }

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,max) or (-max,max).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="max">Maximum value (exclusive).</param>
    /// <param name="signed">If true the distribution has interval (-max,max); otherwise [0,max).</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double max, bool signed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource();
        return CreateSampler<double>( max, signed, rng);
    }

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,max) or (-max,max).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="max">Maximum value (exclusive).</param>
    /// <param name="signed">If true the distribution has interval (-max,max); otherwise [0,max).</param>
    /// <param name="seed">Random source seed.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double max, bool signed, ulong seed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource(seed);
        return CreateSampler<double>( max, signed, rng);
    }

    /// <summary>
    /// Create a sampler for the uniform distribution with interval [0,max) or (-max,max).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="max">Maximum value (exclusive).</param>
    /// <param name="signed">If true the distribution has interval (-max,max); otherwise [0,max).</param>
    /// <param name="rng">Random source.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double max, bool signed, IRandomSource rng)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        if(typeof(T) == typeof(double))
        {
            return (ISampler<double>)new Double.UniformDistributionSampler(max, signed, rng);
        }
        else if(typeof(T) == typeof(float))
        {
            return (ISampler<double>)new Float.UniformDistributionSampler((float)max, signed, rng);
        }
        else
        {
            throw new ArgumentException("Unsupported type argument");
        }
    }

    #endregion

    #region Public Static Methods [IStatelessSampler Factory Methods]

    /// <summary>
    /// Create a stateless sampler for the uniform distribution with interval [0,1).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <returns>A new instance of <see cref="IStatelessSampler{T}"/>.</returns>
    public static IStatelessSampler<double> CreateStatelessSampler<T>()
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        return CreateStatelessSampler<double>(1.0, false);
    }

    /// <summary>
    /// Create a stateless sampler for the uniform distribution with interval [0,max) or (-max,max).
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="max">Maximum value (exclusive).</param>
    /// <param name="signed">If true the distribution has interval (-max,max); otherwise [0,max).</param>
    /// <returns>A new instance of <see cref="IStatelessSampler{T}"/>.</returns>
    public static IStatelessSampler<double> CreateStatelessSampler<T>( double max, bool signed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        if(typeof(T) == typeof(double))
        {
            return (IStatelessSampler<double>)new Double.UniformDistributionStatelessSampler(max, signed);
        }
        else if(typeof(T) == typeof(float))
        {
            return (IStatelessSampler<double>)new Float.UniformDistributionStatelessSampler((float)max, signed);
        }
        else
        {
            throw new ArgumentException("Unsupported type argument");
        }
    }

    #endregion
}
