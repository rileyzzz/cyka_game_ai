// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;

namespace SharpNeat.NeuralNets.Double.ActivationFunctions;

/// <summary>
/// The ArcTan function (inverse tangent function).
/// </summary>
public sealed class ArcTan : IActivationFunction<double>
{
    /// <inheritdoc/>
    public void Fn(ref double x)
    {
        x = Math.Atan(x);
    }

    /// <inheritdoc/>
    public void Fn(ref double x, ref double y)
    {
        y = Math.Atan(x);
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
