// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Diagnostics;
using SharpNeat.Graphs.Acyclic;
using SharpNeat.NeuralNets.Double;

namespace SharpNeat.Neat.Genome.Double;

/// <summary>
/// For decoding instances of <see cref="NeatGenome{Double}"/> to <see cref="IBlackBox{Double}"/>, specifically
/// acyclic neural network instances implemented by <see cref="NeuralNetAcyclic"/>.
/// </summary>
public sealed class NeatGenomeDecoderAcyclic : IGenomeDecoder<NeatGenome<double>,IBlackBox<double>>
{
    /// <summary>
    /// Decodes a NEAT genome into a working neural network.
    /// </summary>
    /// <param name="genome">The genome to decode.</param>
    /// <returns>An <see cref="IBlackBox{T}"/>.</returns>
    public IBlackBox<double> Decode(
        NeatGenome<double> genome)
    {
        Sandbox.Diagnostics.Assert.True(genome?.MetaNeatGenome?.IsAcyclic == true);
        Sandbox.Diagnostics.Assert.True(genome?.ConnectionGenes is not null);
        Sandbox.Diagnostics.Assert.True(genome.ConnectionGenes.Length == genome?.ConnectionIndexMap?.Length);
        Sandbox.Diagnostics.Assert.True(genome.DirectedGraph is DirectedGraphAcyclic);

        // Get a neural net weight array.
        double[] neuralNetWeightArr = genome.GetDigraphWeightArray();

        // Create a working neural net.
        return new NeuralNetAcyclicSafe(
                (DirectedGraphAcyclic)genome.DirectedGraph,
                neuralNetWeightArr,
                genome.MetaNeatGenome.ActivationFn.Fn);
    }
}
