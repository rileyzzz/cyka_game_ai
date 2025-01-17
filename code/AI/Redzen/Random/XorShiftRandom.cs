//// This file is part of the Redzen code library; Copyright Colin D. Green.
//// See LICENSE.txt for details.
//using System.Numerics;

//namespace Redzen.Random;

///// <summary>
///// A fast random number generator for .NET
///// Colin Green, January 2005
/////
///// Key points:
/////  1) Based on a simple and fast xor-shift pseudo-random number generator (PRNG) specified in:
/////  Marsaglia, George. (2003). Xorshift RNGs.
/////  http://www.jstatsoft.org/v08/i14/paper
/////
/////  This particular implementation of xorshift has a period of 2^128-1. See the above paper to see
/////  how this can be easily extended if you need a longer period. At the time of writing I could find no
/////  information on the period of System.Random for comparison.
/////
/////  2) Faster than System.Random. Up to 8x faster, depending on which methods are called.
/////
/////  3) Direct replacement for System.Random. This class implements all of the methods that System.Random
/////  does plus some additional methods. The like named methods are functionally equivalent.
/////
/////  4) Allows fast re-initialisation with a seed, unlike System.Random which accepts a seed at construction
/////  time which then executes a relatively expensive initialisation routine. This provides a significant speed
/////  improvement if you need to reset the pseudo-random number sequence many times, e.g. if you want to
/////  re-generate the same sequence of random numbers many times. An alternative might be to cache random numbers
/////  in an array, but that approach is limited by memory capacity and the fact that you may also want a large
/////  number of different sequences cached. Each sequence can be represented by a single seed value (int) when
/////  using this class.
///// </summary>
//[Obsolete("Superseded by Xoshiro256StarStar (comparable performance, but passes more statistical tests and has a longer period)")]
//public sealed class XorShiftRandom : IRandomSource
//{
//    // Constants.
//    const double INCR_DOUBLE = 1.0 / (1UL << 32);
//    const float INCR_FLOAT = 1f / (1U << 24);
//    const float INCR_HALF = 1f / (1U << 11);

//    // RNG state.
//    uint _x, _y, _z, _w;

//    #region Constructors

//    /// <summary>
//    /// Initialises a new instance with a seed from the default seed source.
//    /// </summary>
//    public XorShiftRandom()
//    {
//        Reinitialise(RandomDefaults.GetSeed());
//    }

//    /// <summary>
//    /// Initialises a new instance with the provided seed.
//    /// </summary>
//    /// <param name="seed">Seed value.</param>
//    public XorShiftRandom(ulong seed)
//    {
//        Reinitialise(seed);
//    }

//    #endregion

//    #region Public Methods [Re-initialisation]

//    /// <inheritdoc/>
//    public void Reinitialise(ulong seed)
//    {
//        // Notes.
//        // The first random sample will be very strongly correlated to the value of _x we set here;
//        // such a correlation is undesirable, therefore we significantly weaken it by hashing the
//        // seed's bits using the splitmix64 PRNG.
//        //
//        // It is required that at least one of the state variables be non-zero;
//        // use of splitmix64 satisfies this requirement because it is an equidistributed generator,
//        // thus if it outputs a zero it will next produce a zero after a further 2^64 outputs.

//        // Use the splitmix64 RNG to hash the seed.
//        ulong t = Splitmix64Rng.Next(ref seed);
//        _x = (uint)t;
//        _y = (uint)(t >> 32);

//        t = Splitmix64Rng.Next(ref seed);
//        _z = (uint)t;
//        _w = (uint)(t >> 32);
//    }

//    #endregion

//    #region Public Methods [System.Random functionally equivalent methods]

//    /// <inheritdoc/>
//    public int Next()
//    {
//        // Perform rejection sampling to handle the special case where the value int.MaxValue is generated;
//        // this value is outside the range of permitted values for this method.
//        // Rejection sampling ensures we produce an unbiased sample.
//        uint rtn;
//        do
//        {
//            rtn = NextInner() & 0x7fff_ffff;
//        }
//        while(rtn == 0x7fff_ffff);

//        return (int)rtn;
//    }

//    /// <inheritdoc/>
//    public int Next(int maxValue)
//    {
//        if(maxValue < 1)
//            throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, "maxValue must be > 0");

//        return NextInner(maxValue);
//    }

//    /// <inheritdoc/>
//    public int Next(int minValue, int maxValue)
//    {
//        if(minValue >= maxValue)
//            throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, "maxValue must be > minValue");

//        long range = (long)maxValue - minValue;
//        if(range <= int.MaxValue)
//            return NextInner((int)range) + minValue;

//        // Call NextInner(long); i.e. the range is greater than int.MaxValue.
//        return (int)(NextInner(range) + minValue);
//    }

//    /// <inheritdoc/>
//    public double NextDouble()
//    {
//        return NextDoubleInner();
//    }

//    /// <inheritdoc/>
//    public double NextDoubleHighRes()
//    {
//        // Notes.
//        // An alternative sampling method from:
//        //
//        //    2014, Taylor R Campbell
//        //
//        //    Uniform random floats:  How to generate a double-precision
//        //    floating-point number in [0, 1] uniformly at random given a uniform
//        //    random source of bits.
//        //
//        //    https://mumble.net/~campbell/tmp/random_real.c
//        //
//        // The basic idea is that we generate a string of binary digits and use them to construct a
//        // base two number of the form:
//        //
//        //    0.{digits}
//        //
//        // The digits are generated in blocks of 64 bits. If all 64 bits in a block are zero then a
//        // running exponent value is reduced by 64 and another 64 bits are generated. This process is
//        // repeated until a block with non-zero bits is produced, or the exponent value falls below -1074.
//        //
//        // The final step is to create the IEE754 double precision variable from a 64 bit significand
//        // (the most recent and thus significant 64 bits), and the running exponent.
//        //
//        // This scheme is capable of generating all possible values in the interval [0,1) that can be
//        // represented by a double precision float, and without bias. There are a little under 2^62
//        // possible discrete values, and this compares to the 2^53 possible values than can be generated by
//        // NextDouble(), however the scheme used in this method is much slower, so is likely of interest
//        // in specialist scenarios.
//        int exponent = -64;
//        ulong significand;
//        int shift;

//        // Read zeros into the exponent until we hit a one; the rest
//        // will go into the significand.
//        while((significand = NextULongInner()) == 0)
//        {
//            exponent -= 64;

//            // If the exponent falls below -1074 = emin + 1 - p,
//            // the exponent of the smallest subnormal, we are
//            // guaranteed the result will be rounded to zero.  This
//            // case is so unlikely it will happen in realistic
//            // terms only if random64 is broken.
//            if(exponent < -1074)
//                return 0;
//        }

//        // There is a 1 somewhere in significand, not necessarily in
//        // the most significant position.  If there are leading zeros,
//        // shift them into the exponent and refill the less-significant
//        // bits of the significand.  Can't predict one way or another
//        // whether there are leading zeros: there's a fifty-fifty
//        // chance, if random64 is uniformly distributed.
//        shift = BitOperations.LeadingZeroCount(significand);
//        if(shift != 0)
//        {
//            exponent -= shift;
//            significand <<= shift;
//            significand |= NextULongInner() >> (64 - shift);
//        }

//        // Set the sticky bit, since there is almost surely another 1
//        // in the bit stream.  Otherwise, we might round what looks
//        // like a tie to even when, almost surely, were we to look
//        // further in the bit stream, there would be a 1 breaking the
//        // tie.
//        significand |= 1;

//        // Finally, convert to double (rounding) and scale by
//        // 2^exponent.
//        return (double)significand * Math.Pow(2, exponent);
//    }

//    /// <inheritdoc/>
//    public unsafe void NextBytes(Span<byte> span)
//    {
//        // For improved performance the below loop operates on these stack allocated copies of the heap variables.
//        // Notes. doing this means that these heavily used variables are located near to other local/stack variables,
//        // thus they will very likely be cached in the same CPU cache line.
//        uint x=_x, y=_y, z=_z, w=_w;

//        uint t;
//        int i=0;

//        // Get a pointer to the start of the span.
//        fixed(byte* pSpan = span)
//        {
//            // A pointer to 32 bit size segments of the span.
//            uint* pUInt = (uint*)pSpan;

//            // Create and store new random bytes in groups of four.
//            for(int bound = span.Length / 4; i < bound; i++)
//            {
//                // Generate 32 random bits and assign to the segment that pUInt is currently pointing to.
//                t = x ^ (x << 11);

//                x = y;
//                y = z;
//                z = w;

//                pUInt[i] = w = (w^(w>>19))^(t^(t>>8));
//            }
//        }

//        // Convert back to one based indexing instead of groups of four bytes.
//        i *= 4;

//        // Fill any remaining bytes in the span that occur when its length is not a multiple of four.
//        if(i < span.Length)
//        {
//            // Generate a further 32 random bits, and update PRNG state.
//            t = x ^ (x << 11);

//            x = y;
//            y = z;
//            z = w;

//            w = (w^(w>>19))^(t^(t>>8));

//            // Allocate one byte at a time until we reach the end of the span.
//            while(i < span.Length)
//            {
//                span[i++] = (byte)w;
//                w >>= 8;
//            }
//        }

//        // Update the state variables on the heap.
//        _x = x;
//        _y = y;
//        _z = z;
//        _w = w;
//    }

//    #endregion

//    #region Public Methods [Methods not present on System.Random]

//    /// <inheritdoc/>
//    public uint NextUInt()
//    {
//        return NextInner();
//    }

//    /// <inheritdoc/>
//    public int NextInt()
//    {
//        // Generate 32 random bits and shift right to leave the most significant 31 bits.
//        // Bit 32 is the sign bit so must be zero to avoid negative results.
//        // Note. Shift right is used instead of a mask because the high significant bits
//        // exhibit higher quality randomness compared to the lower bits.
//        return (int)(NextInner() >> 1);
//    }

//    /// <inheritdoc/>
//    public ulong NextULong()
//    {
//        return NextULongInner();
//    }

//    /// <inheritdoc/>
//    public double NextDoubleNonZero()
//    {
//        // Here we generate a random double in the interval [0, 1 - (1 / 2^32)], and add INCR_DOUBLE
//        // to produce a value in the interval [1 / 2^32, 1]
//        return NextDoubleInner() + INCR_DOUBLE;
//    }

//    /// <inheritdoc/>
//    public float NextFloat()
//    {
//        // Here we generate a random integer between 0 and 2^24-1 (i.e., 24 binary 1s) and multiply by the fractional
//        // unit value 1.0 / 2^24, thus the resulting random value has a min value of 0.0, and a max value of
//        // 1.0 - (1.0 / 2^24).
//        return (NextInner() >> 8) * INCR_FLOAT;
//    }

//    /// <inheritdoc/>
//    public float NextFloatNonZero()
//    {
//        // Here we generate a random float in the interval [0, 1 - (1 / 2^24)], and add INCR_FLOAT
//        // to produce a value in the interval [(1 / 2^24), 1]
//        return NextFloat() + INCR_FLOAT;
//    }

//    /// <inheritdoc/>
//    public Half NextHalf()
//    {
//        // Here we generate a random integer between 0 and 2^11-1 (i.e. 11 binary 1s) and multiply
//        // by the fractional unit value 1.0 / 2^11, thus the result has a max value of
//        // 1.0 - (1.0 / 2^11). Or 0.999511718 in decimal.
//        return (Half)((NextInner() >> 21) * INCR_HALF);
//    }

//    /// <inheritdoc/>
//    public Half NextHalfNonZero()
//    {
//        // Here we generate a random float in the interval [0, 1 - (1 / 2^24)], and add INCR_FLOAT
//        // to produce a value in the interval [1 / 2^24, 1]
//        return (Half)(((NextULongInner() >> 21) * INCR_HALF) + INCR_HALF);
//    }

//    /// <summary>
//    /// Returns a random value sampled from the uniform distribution with interval [0, 1),
//    /// i.e., inclusive of 0.0 and exclusive of 1.0.
//    /// </summary>
//    /// <typeparam name="T">The numeric data type.</typeparam>
//    /// <returns>A new random sample, of type <typeparamref name="T"/>.</returns>
//    public T NextUnitInterval<T>()
//        where T : struct, IBinaryFloatingPointIeee754<T>
//    {
//        if(typeof(T) == typeof(double))
//        {
//            return T.CreateTruncating(NextDoubleInner());
//        }
//        else if(typeof(T) == typeof(float))
//        {
//            return T.CreateTruncating(NextFloat());
//        }
//        else
//        {
//            throw new ArgumentException("Unsupported type argument");
//        }
//    }

//    /// <summary>
//    /// Returns a random value sampled from the uniform distribution with interval (0, 1],
//    /// i.e., exclusive of 0.0, and inclusive of 1.0.
//    /// </summary>
//    /// <typeparam name="T">The numeric data type.</typeparam>
//    /// <returns>A new random sample, of type <typeparamref name="T"/>.</returns>
//    public T NextUnitIntervalNonZero<T>()
//        where T : struct, IBinaryFloatingPointIeee754<T>
//    {
//        if(typeof(T) == typeof(double))
//        {
//            return T.CreateTruncating(NextDoubleNonZero());
//        }
//        else if(typeof(T) == typeof(float))
//        {
//            return T.CreateTruncating(NextFloatNonZero());
//        }
//        else
//        {
//            throw new ArgumentException("Unsupported type argument");
//        }
//    }

//    /// <inheritdoc/>
//    public bool NextBool()
//    {
//        // Generate 32 random bits and return the most significant bit, discarding the rest.
//        // This is slower than the approach of generating and caching 32 bits for future calls, but
//        // (A) gives good quality randomness, and (B) is still very fast.
//        return (NextInner() & 0x8000) == 0;
//    }

//    /// <inheritdoc/>
//    public byte NextByte()
//    {
//        // Note. Here we shift right to use the 8 most significant bits because these exhibit higher quality
//        // randomness than the lower bits.
//        return (byte)(NextULongInner() >> 24);
//    }

//    #endregion

//    #region Private Methods

//    private int NextInner(int maxValue)
//    {
//        // Handle special case of a single sample value.
//        if(maxValue == 1)
//            return 0;

//        // Notes.
//        // Here we sample an integer value within the interval [0, maxValue). Rejection sampling is used in
//        // order to produce unbiased samples. An alternative approach is:
//        //
//        //  return (int)(NextDoubleInner() * maxValue);
//        //
//        // I.e. generate a double precision float in the interval [0,1) and multiply by maxValue. However the
//        // use of floating point arithmetic will introduce bias therefore this method is not used.
//        //
//        // The rejection sampling method used here operates as follows:
//        //
//        //  1) Calculate N such that  2^(N-1) < maxValue <= 2^N, i.e. N is the minimum number of bits required
//        //     to represent maxValue states.
//        //  2) Generate an N bit random sample.
//        //  3) Reject samples that are >= maxValue, and goto (2) to resample.
//        //
//        // Repeat until a valid sample is generated.

//        // Log2Ceiling(numberOfStates) gives the number of bits required to represent maxValue states.
//        int bitCount = MathUtils.Log2Ceiling((uint)maxValue);

//        // Rejection sampling loop.
//        // Note. The expected number of samples per generated value is approx. 1.3862,
//        // i.e. the number of loops, on average, assuming a random and uniformly distributed maxValue.
//        int x;
//        do
//        {
//            x = (int)(NextULongInner() >> (64 - bitCount));
//        }
//        while(x >= maxValue);

//        return x;
//    }

//    private long NextInner(long maxValue)
//    {
//        // Handle special case of a single sample value.
//        if(maxValue == 1)
//            return 0;

//        // See comments on NextInner(int).

//        // Log2Ceiling(numberOfStates) gives the number of bits required to represent maxValue states.
//        int bitCount = MathUtils.Log2Ceiling((ulong)maxValue);

//        // Rejection sampling loop.
//        long x;
//        do
//        {
//            x = (long)(NextULongInner() >> (64 - bitCount));
//        }
//        while(x >= maxValue);

//        return x;
//    }

//    private double NextDoubleInner()
//    {
//        // Notes.
//        // Here we generate a random integer in the interval [0, 2^32-1]  (i.e., the max value is 32 binary 1s),
//        // and multiply by the fractional value 1.0 / 2^32, thus the resulting random has a min value of 0.0 and a max
//        // value of 1.0 - (1.0 / 2^32).
//        //
//        // Use of 32 bits was a historical choice based on that fact that the underlying XorShift PRNG produces 32 bits of randomness
//        // per invocation/cycle. This approach is maintained here for backwards compatibility, however, this class is deprecated in
//        // favour of RandomSourceBase, which uses 53 bits of entropy per double precision float instead of 32.
//        return NextInner() * INCR_DOUBLE;
//    }

//    private uint NextInner()
//    {
//        // Generate 32 bits.
//        uint t = _x ^ (_x << 11);

//        _x = _y;
//        _y = _z;
//        _z = _w;

//        return _w = (_w^(_w>>19)) ^ (t^(t>>8));
//    }

//    private ulong NextULongInner()
//    {
//        // Generate 32 bits.
//        uint t = _x ^ (_x << 11);
//        _x = _y;
//        _y = _z;
//        _z = _w;
//        ulong acc = _w = (_w^(_w>>19)) ^ (t^(t>>8));

//        // Generate a further 32 bits.
//        t = _x ^ (_x << 11);
//        _x = _y;
//        _y = _z;
//        _z = _w;
//        return acc + (((ulong)(_w = (_w^(_w>>19)) ^ (t^(t>>8)))) << 32);
//    }

//    #endregion
//}
