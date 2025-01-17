﻿// This file is part of the Redzen code library; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Redzen.Random;

namespace Redzen.Numerics.Distributions.Float;

/// <summary>
/// A duplicate of Double.ZigguratGaussian that produces Gaussian sample in single precision floating point form.
///
/// For complete details see <see cref="Double.ZigguratGaussian"/>.
/// </summary>
public static class ZigguratGaussian
{
    #region Consts

    /// <summary>
    /// Number of blocks.
    /// </summary>
    const int __blockCount = 128;

    /// <summary>
    /// Right hand x coord of the base rectangle, thus also the left hand x coord of the tail
    /// (pre-determined/computed for 128 blocks).
    /// </summary>
    const double __R = 3.442619855899;
    const float __RF = (float)__R;

    /// <summary>
    /// Area of each rectangle (pre-determined/computed for 128 blocks).
    /// </summary>
    const double __A = 9.91256303526217e-3;

    /// <summary>
    /// Denominator for __INCR constant. This is the number of distinct values this class is capable
    /// of generating in the interval [0,1], i.e. (2^24)-1 distinct values.
    /// </summary>
    const uint __MAXINT = (1u << 24) - 1;

    /// <summary>
    /// Scale factor for converting a ULong with interval [0, 0x1f_ffff_ffff_ffff] to a float with interval [0,1].
    /// </summary>
    const float __INCR = 1f / __MAXINT;

    #endregion

    #region Static Fields

    // __x[i] and __y[i] describe the top-right position of rectangle i.
    static readonly float[] __x;
    static readonly float[] __y;

    // The proportion of each segment that is entirely within the distribution, expressed as uint where
    // a value of 0 indicates 0% and 2^24-1 (i.e. 24 binary 1s) 100%. Expressing this as an integer value
    // allows some floating point operations to be replaced with integer operations.
    static readonly uint[] __xComp;

    // Useful precomputed values.
    // Area A divided by the height of B0. Note. This is *not* the same as __x[i] because the area
    // of B0 is __A minus the area of the distribution tail.
    static readonly float __A_Div_Y0;

    #endregion

    #region Static Initialiser

    static ZigguratGaussian()
    {
        // Initialise rectangle position data.
        // __x[i] and __y[i] describe the top-right position of Box i.

        // Allocate temp storage. We add one to the length of x so that we have an entry at x[__blockCount], this avoids having
        // to do a special case test when sampling from the top box.
        double[] x = new double[__blockCount + 1];
        double[] y = new double[__blockCount];

        // Determine top right position of the base rectangle/box (the rectangle with the Gaussian tale attached).
        // We call this Box 0 or B0 for short.
        // Note. x[0] also describes the right-hand edge of B1. (See diagram).
        x[0] = __R;
        y[0] = GaussianPdfDenorm(__R);

        // The next box (B1) has a right hand X edge the same as B0.
        // Note. B1's height is the box area divided by its width, hence B1 has a smaller height than B0 because
        // B0's total area includes the attached distribution tail.
        x[1] = __R;
        y[1] = y[0] + (__A / x[1]);

        // Calc positions of all remaining rectangles.
        for(int i=2; i < __blockCount; i++)
        {
            x[i] = GaussianPdfDenormInv(y[i-1]);
            y[i] = y[i-1] + (__A / x[i]);
        }

        // For completeness we define the right-hand edge of a notional box 6 as being zero (a box with no area).
        x[__blockCount] = 0f;

        // Useful precomputed values.
        __A_Div_Y0 = (float)(__A / y[0]);
        __xComp = new uint[__blockCount];

        // Special case for base box. __xComp[0] stores the area of B0 as a proportion of __R
        // (recalling that all segments have area __A, but that the base segment is the combination of B0 and the distribution tail).
        // Thus __xComp[0] is the probability that a sample point is within the box part of the segment.
        __xComp[0] = (uint)(((__R * y[0]) / __A) * (double)__MAXINT);

        for(int i=1; i < __blockCount-1; i++)
        {
            __xComp[i] = (uint)((x[i+1] / x[i]) * (double)__MAXINT);
        }
        __xComp[__blockCount-1] = 0;  // Shown for completeness.

        // Allocate permanent single precision arrays, and cast/copy the temporary double precision values into them.
        __x = new float[x.Length];
        __y = new float[y.Length];

        for(int i=0; i < x.Length; i++)
        {
            __x[i] = (float)x[i];
        }

        for(int i=0; i < y.Length; i++)
        {
            __y[i] = (float)y[i];
        }

        // Sanity check. Test that the top edge of the topmost rectangle is at y=1.0.
        // Note. We expect there to be a tiny drift away from 1.0 due to the inexactness of floating
        // point arithmetic.
        Sandbox.Diagnostics.Assert.True(Math.Abs(1f - __y[__blockCount-1]) < 1e-10f);
    }

    #endregion

    #region Public Static Methods [Fast / Reference Based Methods]

    /// <summary>
    /// Gets a random value sampled from the standard Gaussian distribution, i.e., with mean of 0 and standard deviation of 1.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <param name="x">Reference to a variable to store the new sample value in.</param>
    public static void Sample(IRandomSource rng, out float x)
    {
        while(true)
        {
            // Generate 64 random bits.
            ulong u = rng.NextULong();

            // Note. 32 random bits are required and therefore the lowest 32 bits are discarded
            // (a typical characteristic of PRNGs is that the least significant bits exhibit lower
            // quality randomness than the higher bits).
            // Note. The bit index values in comments are zero based, i..e the first bit is bit 0.
            // Select a segment (7 bits, bits 32 to 38).
            int s = (int)((u >> 32) & 0x7f);

            // Select the sign bit (bit 39), and shift it to the sign bit position for IEEE754
            // single-precision floating-point format.
            // Previously, the sign bit handling used a conditional branch that optionally multiplied the
            // positive Gaussian sample by -1. However, that approach is considerably slower because modern
            // superscalar CPUs rely heavily on branch prediction, but the sign bit here is randomly generated
            // and therefore entirely unpredictable, i.e. the absolute worse case scenario for a branch
            // predictor!
            uint signBit = (uint)((u & 0x80_0000_0000UL) >> 8);

            // Get a uniform random value with interval [0, 2^24-1], or in hexadecimal [0, 0xff_ffff]
            // (i.e. a random 24 bit number) (bits 40 to 63).
            ulong u2 = u >> 40;

            // Special case for the base segment.
            if(s == 0)
            {
                if(u2 < __xComp[0])
                {
                    // Generated x is within R0.
                    x = u2 * __INCR * __A_Div_Y0;
                }
                else
                {
                    // Generated x is in the tail of the distribution.
                    SampleTail(rng, out x);
                }

                SetSignBit(ref x, ref signBit);
                return;
            }

            // All other segments.
            if(u2 < __xComp[s])
            {
                // Generated x is within the rectangle.
                x = u2 * __INCR * __x[s];
                SetSignBit(ref x, ref signBit);
                return;
            }

            // Generated x is outside of the rectangle.
            // Generate a random y coordinate and test if our (x,y) is within the distribution curve.
            // This execution path is relatively slow/expensive (makes a call to Math.Exp()) but is relatively rarely executed,
            // although more often than the 'tail' path (above).
            x = u2 * __INCR * __x[s];
            if(__y[s-1] + ((__y[s] - __y[s-1]) * rng.NextFloat()) < GaussianPdfDenormF(x))
            {
                SetSignBit(ref x, ref signBit);
                return;
            }
        }
    }

    /// <summary>
    /// Gets a random value sampled from the a Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <param name="x">Reference to a variable to store the new sample value in.</param>
    public static void Sample(IRandomSource rng, float mean, float stdDev, out float x)
    {
        Sample(rng, out x);

        x = (x * stdDev) + mean;
    }

    /// <summary>
    /// Fills a span with random samples from the standard Gaussian distribution, i.e., with mean of 0 and standard deviation of 1.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <param name="span">The span to fill with random samples.</param>
    public static void Sample(IRandomSource rng, Span<float> span)
    {
        for(int i = 0; i < span.Length; i++)
            Sample(rng, out span[i]);
    }

    /// <summary>
    /// Fills a span with random samples from a Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <param name="span">The span to fill with random samples.</param>
    public static void Sample(IRandomSource rng, float mean, float stdDev, Span<float> span)
    {
        for(int i=0; i < span.Length; i++)
        {
            Sample(rng, out float x);
            span[i] = (x * stdDev) + mean;
        }
    }

    #endregion

    #region Public Static Methods [Return-value Based Methods]

    /// <summary>
    /// Returns a random value sampled from the standard Gaussian distribution, i.e., with mean of 0 and standard deviation of 1.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <returns>A new random sample.</returns>
    public static float Sample(IRandomSource rng)
    {
        Sample(rng, out float x);
        return x;
    }

    /// <summary>
    /// Returns a random value sampled from the a Gaussian distribution with the specified mean and standard deviation.
    /// </summary>
    /// <param name="rng">Random source.</param>
    /// <param name="mean">Distribution mean.</param>
    /// <param name="stdDev">Distribution standard deviation.</param>
    /// <returns>A random sample.</returns>
    public static float Sample(IRandomSource rng, float mean, float stdDev)
    {
        Sample(rng, out float x);

        return (x * stdDev) + mean;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Sample from the distribution tail (defined as having x >= __R).
    /// </summary>
    private static void SampleTail(IRandomSource rng, out float x)
    {
        float y;
        do
        {
            // Note. we use NextFloatNonZero() because Log(0) returns -Infinity and will also tend to be a very slow execution path (when it occurs, which is rarely).
            x = -MathF.Log(rng.NextFloatNonZero()) / __RF;
            y = -MathF.Log(rng.NextFloatNonZero());
        }
        while(y+y < x*x);
        x += __RF;
    }

    /// <summary>
    /// Gaussian probability density function, denormalised, that is, y = e^-(x^2/2).
    /// </summary>
    private static double GaussianPdfDenorm(double x)
    {
        return Math.Exp(-(x * x / 2.0));
    }

    /// <summary>
    /// Gaussian probability density function, denormalised, that is, y = e^-(x^2/2).
    /// Returns a single-precision floating-point value.
    /// </summary>
    private static float GaussianPdfDenormF(float x)
    {
        return MathF.Exp(-(x * x * 0.5f));
    }

    /// <summary>
    /// Inverse function of GaussianPdfDenorm(x).
    /// </summary>
    private static double GaussianPdfDenormInv(double y)
    {
        // Operates over the y interval (0,1], which happens to be the y interval of the pdf,
        // with the exception that it does not include y=0, but we would never call with
        // y=0 so it doesn't matter. Note that a Gaussian effectively has a tail going
        // into infinity on the x-axis, hence asking what is x when y=0 is an invalid question
        // in the context of this class.
        return Math.Sqrt(-2.0 * Math.Log(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetSignBit(ref float x, ref uint signBit)
    {
		x = BitConverter.UInt32BitsToSingle( BitConverter.SingleToUInt32Bits( x ) | signBit );
        //Unsafe.As<float, uint>(ref x) |= signBit;
    }

    #endregion
}
