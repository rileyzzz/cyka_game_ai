// This file is part of the Redzen code library; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
using Redzen.Random;

namespace Redzen.Numerics.Distributions;

/// <summary>
/// Static factory methods for creating instances of ISampler{T} and IStatelessSampler{T} for sampling from Gaussian distributions.
/// </summary>
public static class GaussianDistributionSamplerFactory
{
    #region Public Static Methods [ISampler Factory Methods]

    /// <summary>
    /// Create a sampler for the standard Gaussian distribution.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>()
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource();
        return CreateSampler<double>( 0.0, 1.0, rng );
    }

    /// <summary>
    /// Create a sampler for the standard Gaussian distribution.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="seed">Random source seed.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(ulong seed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource(seed);
        return CreateSampler<double>( 0.0, 1.0, rng);
    }

    /// <summary>
    /// Create a sampler for the standard Gaussian distribution.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(IRandomSource rng)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        return CreateSampler<double>( 0.0, 1.0, rng );
    }

    /// <summary>
    /// Create a sampler for the Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double mean, double stdDev)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource();
        return CreateSampler<double>( mean, stdDev, rng);
    }

    /// <summary>
    /// Create a sampler for the Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <param name="seed">Random source seed.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double mean, double stdDev, ulong seed)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        IRandomSource rng = RandomDefaults.CreateRandomSource(seed);
        return CreateSampler<double>(mean, stdDev, rng);
    }

    /// <summary>
    /// Create a sampler for the Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <param name="rng">Random source.</param>
    /// <returns>A new instance of <see cref="ISampler{T}"/>.</returns>
    public static ISampler<double> CreateSampler<T>(double mean, double stdDev, IRandomSource rng)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        if(typeof(T) == typeof(double))
        {
            return (ISampler<double>)new Double.ZigguratGaussianSampler(double.CreateChecked(mean), stdDev, rng);
        }
        else if(typeof(T) == typeof(float))
        {
            return (ISampler<double>)new Float.ZigguratGaussianSampler(float.CreateChecked(mean), (float)(stdDev), rng);
        }
        else
        {
            throw new ArgumentException("Unsupported type argument");
        }
    }

    #endregion

    #region Public Static Methods [IStatelessSampler Factory Methods]

    /// <summary>
    /// Create a stateless sampler for the standard Gaussian distribution.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <returns>A new instance of <see cref="IStatelessSampler{T}"/>.</returns>
    public static IStatelessSampler<double> CreateStatelessSampler<T>()
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        return CreateStatelessSampler<double>(0.0, 1.0);
    }

    /// <summary>
    /// Create a stateless sampler for the Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <typeparam name="T">Data type of the samples.</typeparam>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <returns>A new instance of <see cref="IStatelessSampler{T}"/>.</returns>
    public static IStatelessSampler<double> CreateStatelessSampler<T>(double mean, double stdDev)
        // where T : struct, IBinaryFloatingPointIeee754<T>
    {
        if(typeof(T) == typeof(double))
        {
            return (IStatelessSampler<double>)new Double.ZigguratGaussianStatelessSampler(double.CreateChecked(mean), double.CreateChecked(stdDev));
        }
        else if(typeof(T) == typeof(float))
        {
            return (IStatelessSampler<double>)new Float.ZigguratGaussianStatelessSampler(float.CreateChecked(mean), float.CreateChecked(stdDev));
        }
        else
        {
            throw new ArgumentException("Unsupported type argument");
        }
    }

    #endregion
}
