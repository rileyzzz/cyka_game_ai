﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;

namespace SharpNeat.NeuralNets.Double.ActivationFunctions.Vectorized;

/// <summary>
/// Rectified linear activation unit (ReLU).
/// </summary>
public sealed class ReLU : IActivationFunction<double>
{
    /// <inheritdoc/>
    public void Fn(ref double x)
    {
        if(x < 0.0)
            x = 0.0;
    }

    /// <inheritdoc/>
    public void Fn(ref double x, ref double y)
    {
        y = x;

        if(x < 0.0)
            y = 0.0;
    }

    ///// <inheritdoc/>
    //public void Fn(Span<double> v)
    //{
    //    Fn(ref MemoryMarshal.GetReference(v), v.Length);
    //}

    ///// <inheritdoc/>
    //public void Fn(ReadOnlySpan<double> v, Span<double> w)
    //{
    //    // Obtain refs to the spans, and call on to the unsafe ref based overload.
    //    Fn(
    //        ref MemoryMarshal.GetReference(v),
    //        ref MemoryMarshal.GetReference(w),
    //        v.Length);
    //}

    ///// <inheritdoc/>
    //public void Fn(ref double vref, int len)
    //{
    //    // Calc span bounds.
    //    ref double vrefBound = ref Unsafe.Add(ref vref, len);
    //    ref double vrefBoundVec = ref Unsafe.Subtract(ref vrefBound, Vector<double>.Count - 1);

    //    // Loop SIMD vector sized segments.
    //    for(; Unsafe.IsAddressLessThan(ref vref, ref vrefBoundVec);
    //        vref = ref Unsafe.Add(ref vref, Vector<double>.Count))
    //    {
    //        // Load values into a vector.
    //        // The odd code pattern is taken from the Vector<T> constructor's source code.
    //        var vec = Unsafe.ReadUnaligned<Vector<double>>(
    //            ref Unsafe.As<double, byte>(ref vref));

    //        // Apply max(val, 0) to each element in the vector.
    //        vec = Vector.Max(vec, Vector<double>.Zero);

    //        // Store the result in the post-activations span.
    //        Unsafe.WriteUnaligned(
    //            ref Unsafe.As<double, byte>(ref vref),
    //            vec);
    //    }

    //    // Handle vectors with lengths not an exact multiple of vector width.
    //    for(; Unsafe.IsAddressLessThan(ref vref, ref vrefBound);
    //        vref = ref Unsafe.Add(ref vref, 1))
    //    {
    //        Fn(ref vref);
    //    }
    //}

    ///// <inheritdoc/>
    //public void Fn(ref double vref, ref double wref, int len)
    //{
    //    // Calc span bounds.
    //    ref double vrefBound = ref Unsafe.Add(ref vref, len);
    //    ref double vrefBoundVec = ref Unsafe.Subtract(ref vrefBound, Vector<double>.Count - 1);

    //    // Loop SIMD vector sized segments.
    //    for(; Unsafe.IsAddressLessThan(ref vref, ref vrefBoundVec);
    //        vref = ref Unsafe.Add(ref vref, Vector<double>.Count),
    //        wref = ref Unsafe.Add(ref wref, Vector<double>.Count))
    //    {
    //        // Load values into a vector.
    //        // The odd code pattern is taken from the Vector<T> constructor's source code.
    //        var vec = Unsafe.ReadUnaligned<Vector<double>>(
    //            ref Unsafe.As<double, byte>(ref vref));

    //        // Apply max(val, 0) to each element in the vector.
    //        vec = Vector.Max(vec, Vector<double>.Zero);

    //        // Store the result in the post-activations span.
    //        Unsafe.WriteUnaligned(
    //            ref Unsafe.As<double, byte>(ref wref),
    //            vec);
    //    }

    //    // Handle vectors with lengths not an exact multiple of vector width.
    //    for(; Unsafe.IsAddressLessThan(ref vref, ref vrefBound);
    //        vref = ref Unsafe.Add(ref vref, 1),
    //        wref = ref Unsafe.Add(ref wref, 1))
    //    {
    //        Fn(ref vref, ref wref);
    //    }
    //}
}
