﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;

namespace SharpNeat.NeuralNets.Double.ActivationFunctions;

/// <summary>
/// Leaky rectified linear activation unit (ReLU).
/// Shifted on the x-axis so that x=0 gives y=0.5, in keeping with the logistic sigmoid.
/// </summary>
public sealed class LeakyReLUShifted : IActivationFunction<double>
{
    /// <inheritdoc/>
    public void Fn(ref double x)
    {
        const double a = 0.001;
        const double offset = 0.5;

        x += offset;

        if(x < 0.0)
            x *= a;
    }

    /// <inheritdoc/>
    public void Fn(ref double x, ref double y)
    {
        const double a = 0.001;
        const double offset = 0.5;

        y = x + offset;

        if(y < 0.0)
            y *= a;
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

    //    // Loop over span elements, invoking the scalar activation fn for each.
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

    //    // Loop over span elements, invoking the scalar activation fn for each.
    //    for(; Unsafe.IsAddressLessThan(ref vref, ref vrefBound);
    //        vref = ref Unsafe.Add(ref vref, 1),
    //        wref = ref Unsafe.Add(ref wref, 1))
    //    {
    //        Fn(ref vref, ref wref);
    //    }
    //}
}
