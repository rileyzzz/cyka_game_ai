﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Diagnostics;
using SharpNeat.NeuralNets.Double;

namespace SharpNeat.Neat.Genome.Double;

/// <summary>
/// For decoding instances of <see cref="NeatGenome{Double}"/> to <see cref="IBlackBox{Double}"/>, specifically
/// cyclic neural network instances implemented by either <see cref="NeuralNetCyclic"/>.
/// </summary>
public sealed class NeatGenomeDecoderCyclic : IGenomeDecoder<NeatGenome<double>,IBlackBox<double>>
{
    /// <summary>
    /// Decodes a NEAT genome into a working neural network.
    /// </summary>
    /// <param name="genome">The genome to decode.</param>
    /// <returns>An <see cref="IBlackBox{T}"/>.</returns>
    public IBlackBox<double> Decode(NeatGenome<double> genome)
    {
        // Note. In principle an acyclic net can be decoded to a cyclic network (but not the other way around), but standard sharpneat behaviour is not to support this.
        Sandbox.Diagnostics.Assert.True(!genome.MetaNeatGenome.IsAcyclic);

        // Create a working neural net.
        return new NeuralNetCyclicSafe(
                genome.DirectedGraph,
                genome.ConnectionGenes._weightArr,
                genome.MetaNeatGenome.ActivationFn.Fn,
                genome.MetaNeatGenome.CyclesPerActivation);
    }
}
