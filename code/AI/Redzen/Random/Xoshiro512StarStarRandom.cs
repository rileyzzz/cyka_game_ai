﻿// A C# port of the xorshiro512** pseudo random number generator (PRNG).
// Original C source code was obtained from:
//    http://xoshiro.di.unimi.it/xoshiro512starstar.c
//
// See original headers below for more info.
//
// ===========================================================================
//
// Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
//
// To the extent possible under law, the author has dedicated all copyright
// and related and neighboring rights to this software to the public domain
// worldwide. This software is distributed without any warranty.

// See <http://creativecommons.org/publicdomain/zero/1.0/>. */
//
// --------
//
// This is xoshiro512** 1.0, an all-purpose, rock-solid generator. It has
// excellent (about 1ns) speed, an increased state (512 bits) that is
// large enough for any parallel application, and it passes all tests we
// are aware of.
//
// For generating just floating-point numbers, xoshiro512+ is even faster.
//
// The state must be seeded so that it is not everywhere zero. If you have
// a 64-bit seed, we suggest to seed a splitmix64 generator and use its
// output to fill s.
using System.Numerics;
//using System.Runtime.CompilerServices;
//// using System.Runtime.InteropServices;

namespace Redzen.Random;

/// <summary>
/// Xoshiro512** (xor, shift, rotate) pseudo-random number generator (PRNG).
/// </summary>
public sealed class Xoshiro512StarStarRandom : RandomSourceBase, IRandomSource
{
    // RNG state.
    ulong _s0;
    ulong _s1;
    ulong _s2;
    ulong _s3;
    ulong _s4;
    ulong _s5;
    ulong _s6;
    ulong _s7;

    #region Constructors

    /// <summary>
    /// Initialises a new instance with a seed from the default seed source.
    /// </summary>
    public Xoshiro512StarStarRandom()
    {
        Reinitialise(RandomDefaults.GetSeed());
    }

    /// <summary>
    /// Initialises a new instance with the provided seed.
    /// </summary>
    /// <param name="seed">Seed value.</param>
    public Xoshiro512StarStarRandom(ulong seed)
    {
        Reinitialise(seed);
    }

    #endregion

    #region Public Methods [Re-initialisation]

    /// <inheritdoc/>
    public void Reinitialise(ulong seed)
    {
        // Notes.
        // The first random sample will be very strongly correlated to the value we give to the
        // state variables here; such a correlation is undesirable, therefore we significantly
        // weaken it by hashing the seed's bits using the splitmix64 PRNG.
        //
        // It is required that at least one of the state variables be non-zero;
        // use of splitmix64 satisfies this requirement because it is an equidistributed generator,
        // thus if it outputs a zero it will next produce a zero after a further 2^64 outputs.

        // Use the splitmix64 RNG to hash the seed.
        _s0 = Splitmix64Rng.Next(ref seed);
        _s1 = Splitmix64Rng.Next(ref seed);
        _s2 = Splitmix64Rng.Next(ref seed);
        _s3 = Splitmix64Rng.Next(ref seed);
        _s4 = Splitmix64Rng.Next(ref seed);
        _s5 = Splitmix64Rng.Next(ref seed);
        _s6 = Splitmix64Rng.Next(ref seed);
        _s7 = Splitmix64Rng.Next(ref seed);
    }

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
    public override void NextBytes(Span<byte> span)
    {
        // For improved performance the below loop operates on these stack allocated copies of the heap variables.
        // Note. doing this means that these heavily used variables are located near to other local/stack variables,
        // thus they will very likely be cached in the same CPU cache line.
        ulong s0 = _s0;
        ulong s1 = _s1;
        ulong s2 = _s2;
        ulong s3 = _s3;
        ulong s4 = _s4;
        ulong s5 = _s5;
        ulong s6 = _s6;
        ulong s7 = _s7;

        // Allocate bytes in groups of 8 (64 bits at a time), for good performance.
        // Keep looping and updating buffer to point to the remaining/unset bytes, until buffer.Length is too small
        // to use this loop.
        while(span.Length >= sizeof(ulong))
        {
			// Get 64 random bits, and assign to buffer (at the slice it is currently pointing to).
			//Unsafe.WriteUnaligned(
			//    ref MemoryMarshal.GetReference(span),
			//    BitOperations.RotateLeft(s1 * 5, 7) * 9);
			var b = BitConverter.GetBytes( BitOperations.RotateLeft( s1 * 5, 7 ) * 9 );
			for ( int j = 0; j < b.Length; j++ ) span[j] = b[j];

			// Update PRNG state.
			ulong t = s1 << 11;
            s2 ^= s0;
            s5 ^= s1;
            s1 ^= s2;
            s7 ^= s3;
            s3 ^= s4;
            s4 ^= s5;
            s0 ^= s6;
            s6 ^= s7;
            s6 ^= t;
            s7 = BitOperations.RotateLeft(s7, 21);

            // Set buffer to the a slice over the remaining bytes.
            span = span.Slice(sizeof(ulong));
        }

        // Fill any remaining bytes in buffer (these occur when its length is not a multiple of eight).
        if(!span.IsEmpty)
        {
            // Get 64 random bits.
            ulong next = BitOperations.RotateLeft(s1 * 5, 7) * 9;
            var remainingBytes = BitConverter.GetBytes( next );

            for(int i=0; i < span.Length; i++)
            {
                span[i] = remainingBytes[i];
            }

            // Update PRNG state.
            ulong t = s1 << 11;
            s2 ^= s0;
            s5 ^= s1;
            s1 ^= s2;
            s7 ^= s3;
            s3 ^= s4;
            s4 ^= s5;
            s0 ^= s6;
            s6 ^= s7;
            s6 ^= t;
            s7 = BitOperations.RotateLeft(s7, 21);
        }

        // Update the state variables on the heap.
        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
        _s4 = s4;
        _s5 = s5;
        _s6 = s6;
        _s7 = s7;
    }

    /// <inheritdoc/>
    protected override ulong NextULongInner()
    {
        // Generate a new random sample.
        ulong result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;

        // Update PRNG state.
        ulong t = _s1 << 11;
        _s2 ^= _s0;
        _s5 ^= _s1;
        _s1 ^= _s2;
        _s7 ^= _s3;
        _s3 ^= _s4;
        _s4 ^= _s5;
        _s0 ^= _s6;
        _s6 ^= _s7;
        _s6 ^= t;
        _s7 = BitOperations.RotateLeft(_s7, 21);

        return result;
    }

    #endregion
}
