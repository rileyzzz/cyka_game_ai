﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
using Redzen.Numerics.Distributions;
using static SharpNeat.Neat.Reproduction.Asexual.Strategy.AddConnectionUtils;

namespace SharpNeat.Neat.Reproduction.Asexual.Strategy;

/// <summary>
/// A NEAT genome asexual reproduction strategy based on adding a single connection.
/// </summary>
/// <typeparam name="T">Connection weight data type.</typeparam>
/// <remarks>
/// Offspring genomes are created by taking a clone of a single parent genome and adding a single connection,
/// if possible.
/// </remarks>
public sealed class AddCyclicConnectionStrategy<T> : IAsexualReproductionStrategy<T>
    where T : struct, IBinaryFloatingPointIeee754<T>
{
    readonly MetaNeatGenome<T> _metaNeatGenome;
    readonly INeatGenomeBuilder<T> _genomeBuilder;
    readonly Int32Sequence _genomeIdSeq;
    readonly Int32Sequence _generationSeq;

    readonly IStatelessSampler<T> _weightSamplerA;
    readonly IStatelessSampler<T> _weightSamplerB;

    #region Constructor

    /// <summary>
    /// Construct a new instance.
    /// </summary>
    /// <param name="metaNeatGenome">NEAT genome metadata.</param>
    /// <param name="genomeBuilder">NeatGenome builder.</param>
    /// <param name="genomeIdSeq">Genome ID sequence; for obtaining new genome IDs.</param>
    /// <param name="generationSeq">Generation sequence; for obtaining the current generation number.</param>
    public AddCyclicConnectionStrategy(
        MetaNeatGenome<T> metaNeatGenome,
        INeatGenomeBuilder<T> genomeBuilder,
        Int32Sequence genomeIdSeq,
        Int32Sequence generationSeq)
    {
        _metaNeatGenome = metaNeatGenome;
        _genomeBuilder = genomeBuilder;
        _genomeIdSeq = genomeIdSeq;
        _generationSeq = generationSeq;

        _weightSamplerA = (IStatelessSampler<T>)(object)UniformDistributionSamplerFactory.CreateStatelessSampler<T>(
            metaNeatGenome.ConnectionWeightScale, true);

        _weightSamplerB = (IStatelessSampler<T>)(object)UniformDistributionSamplerFactory.CreateStatelessSampler<T>(
			(metaNeatGenome.ConnectionWeightScale * 0.01), true);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public NeatGenome<T>? CreateChildGenome(NeatGenome<T> parent, IRandomSource rng)
    {
        // Attempt to find a new connection that we can add to the genome.
        if(!TryGetConnection(parent, rng, out DirectedConnection directedConn, out int insertIdx))
        {
            // Failed to find a new connection.
            return null;
        }

        // Determine the connection weight.
        // 50% of the time use weights very close to zero.
        // Note. this recreates the strategy used in SharpNEAT 2.x.
        // ENHANCEMENT: Reconsider the distribution of new weights and if there are better approaches (distributions) we could use.
        T weight = rng.NextBool() ? _weightSamplerB.Sample(rng) : _weightSamplerA.Sample(rng);

        // Create a new connection gene array that consists of the parent connection genes plus the new gene
        // inserted at the correct (sorted) position.
        var parentConnArr = parent.ConnectionGenes._connArr;
        var parentWeightArr = parent.ConnectionGenes._weightArr;
        int parentLen = parentConnArr.Length;

        // Create the child genome's ConnectionGenes object.
        int childLen = parentLen + 1;
        var connGenes = new ConnectionGenes<T>(childLen);
        var connArr = connGenes._connArr;
        var weightArr = connGenes._weightArr;

        // Copy genes up to insertIdx.
        Array.Copy(parentConnArr, connArr, insertIdx);
        Array.Copy(parentWeightArr, weightArr, insertIdx);

        // Copy the new genome into its insertion point.
        connArr[insertIdx] = new DirectedConnection(
            directedConn.SourceId,
            directedConn.TargetId);

        weightArr[insertIdx] = weight;

        // Copy remaining genes (if any).
        Array.Copy(parentConnArr, insertIdx, connArr, insertIdx+1, parentLen-insertIdx);
        Array.Copy(parentWeightArr, insertIdx, weightArr, insertIdx+1, parentLen-insertIdx);

        // Create and return a new genome.
        // Note. The set of hidden node IDs remains unchanged from the parent, therefore we are able to re-use
        // both parent.HiddenNodeIdArray and NodeIndexByIdMap.
        return _genomeBuilder.Create(
            _genomeIdSeq.Next(),
            _generationSeq.Peek,
            connGenes,
            parent.HiddenNodeIdArray,
            parent.NodeIndexByIdMap);
    }

    #endregion

    #region Private Methods

    private bool TryGetConnection(
        NeatGenome<T> parent,
        IRandomSource rng,
        out DirectedConnection conn,
        out int insertIdx)
    {
        // Make several attempts to find a new connection, if not successful then give up.
        for(int attempts=0; attempts < 5; attempts++)
        {
            if(TryGetConnectionInner(parent, rng, out conn, out insertIdx))
                return true;
        }

        conn = default;
        insertIdx = default;
        return false;
    }

    private bool TryGetConnectionInner(
        NeatGenome<T> parent,
        IRandomSource rng,
        out DirectedConnection conn,
        out int insertIdx)
    {
        int inputCount = _metaNeatGenome.InputNodeCount;
        int outputCount = _metaNeatGenome.OutputNodeCount;
        int hiddenCount = parent.HiddenNodeIdArray.Length;

        // Select a source node at random.
        // Note. this can be any node (input, output or hidden).
        int totalNodeCount = parent.MetaNeatGenome.InputOutputNodeCount + hiddenCount;
        int srcId = GetNodeIdFromIndex(parent, rng.Next(totalNodeCount));

        // Select a target node at random.
        // Note. This cannot be an input node (so must be a hidden or output node).
        int outputHiddenCount = outputCount + hiddenCount;
        int tgtId = GetNodeIdFromIndex(parent, inputCount + rng.Next(outputHiddenCount));

        // Test if the chosen connection already exists.
        // Note. Connection genes are always sorted by sourceId then targetId, so we can use a binary search to
        // find an existing connection in O(log(n)) time.
        conn = new DirectedConnection(srcId, tgtId);

        if((insertIdx = Array.BinarySearch(parent.ConnectionGenes._connArr, conn)) < 0)
        {
            // The proposed new connection does not already exist, therefore we can use it.
            // Get the position in parent.ConnectionGeneArray that the new connection should be inserted at (to maintain sort order).
            insertIdx = ~insertIdx;
            return true;
        }

        conn = default;
        insertIdx = default;
        return false;
    }

    #endregion
}
