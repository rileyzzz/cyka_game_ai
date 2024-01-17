﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Numerics;
//// using System.Runtime.InteropServices;
using Redzen.Numerics;
using Redzen.Numerics.Distributions;
using SharpNeat.Neat.Reproduction.Asexual;
using SharpNeat.Neat.Reproduction.Asexual.WeightMutation;

namespace SharpNeat.Neat;

/// <summary>
/// Factory class for creating new instances of <see cref="NeatPopulationFactory{T}"/>.
/// </summary>
/// <typeparam name="T">Connection weight data type.</typeparam>
public class NeatPopulationFactory<T>
    where T : struct, IBinaryFloatingPointIeee754<T>
{
    readonly MetaNeatGenome<T> _metaNeatGenome;
    readonly INeatGenomeBuilder<T> _genomeBuilder;
    readonly double _connectionsProportion;
    readonly DirectedConnection[] _connectionDefArr;

    readonly IRandomSource _rng;
    readonly Int32Sequence _genomeIdSeq;
    readonly Int32Sequence _innovationIdSeq;
    readonly IStatelessSampler<T> _connWeightDist;

    #region Constructor

    private NeatPopulationFactory(
        MetaNeatGenome<T> metaNeatGenome,
        double connectionsProportion,
        IRandomSource rng)
    {
        _metaNeatGenome = metaNeatGenome ?? throw new ArgumentNullException(nameof(metaNeatGenome));
        _genomeBuilder = NeatGenomeBuilderFactory<T>.Create(metaNeatGenome);
        _connectionsProportion = connectionsProportion;

        // Define the set of all possible connections between the input and output nodes (fully interconnected).
        int inputCount = metaNeatGenome.InputNodeCount;
        int outputCount = metaNeatGenome.OutputNodeCount;
        _connectionDefArr = new DirectedConnection[inputCount * outputCount];

        // Notes.
        // Nodes are assigned innovation IDs. By convention the input nodes are assigned IDs first starting at zero, then the output nodes.
        // Thus, because all of the evolved networks have a fixed number of inputs and outputs, the IDs of these nodes are always fixed.
        int firstOutputNodeId = inputCount;

        for(int srcId=0, i=0; srcId < inputCount; srcId++)
        {
            for(int tgtIdx=0; tgtIdx < outputCount; tgtIdx++)
                _connectionDefArr[i++] = new DirectedConnection(srcId, firstOutputNodeId + tgtIdx);
        }

        // Init RNG and ID sequences.
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _genomeIdSeq = new Int32Sequence();
        int nextInnovationId = inputCount + outputCount;
        _innovationIdSeq = new Int32Sequence(nextInnovationId);

		// Init random connection weight source.
		// TODO: Consider using gaussian samples here. One of the big leaps in backpropagation learning was related to avoiding large connection weights in the initial random weights.
        _connWeightDist = (IStatelessSampler<T>)(object)UniformDistributionSamplerFactory.CreateStatelessSampler<double>(
            _metaNeatGenome.ConnectionWeightScale, true);
    }

    #endregion

    #region Private Methods

    private NeatPopulation<T> CreatePopulation(int size)
    {
        var genomeList = CreateGenomeList(size);
        return new NeatPopulation<T>(
            _metaNeatGenome,
            _genomeBuilder,
            size,
            genomeList,
            _genomeIdSeq,
            _innovationIdSeq);
    }

    /// <summary>
    /// Creates a list of randomly initialised genomes.
    /// </summary>
    /// <param name="count">The number of genomes to create.</param>
    private List<NeatGenome<T>> CreateGenomeList(int count)
    {
        List<NeatGenome<T>> genomeList = new(count);
        for(int i=0; i < count; i++)
        {
            NeatGenome<T> genome = CreateGenome();
            genomeList.Add(genome);
        }
        return genomeList;
    }

    /// <summary>
    /// Creates a single randomly initialised genome.
    /// </summary>
    private NeatGenome<T> CreateGenome()
    {
        // Determine how many connections to create in the new genome, as a proportion of all possible connections
        // between the input and output nodes.
        int connectionCount = (int)NumericsUtils.StochasticRound(_connectionDefArr.Length * _connectionsProportion, _rng);

        // Ensure there is at least one connection.
        connectionCount = Math.Max(1, connectionCount);

        // Select a random subset of all possible connections between the input and output nodes.
        int[] sampleArr = new int[connectionCount];
        DiscreteDistributionUtils.SampleUniformWithoutReplacement(
            _connectionDefArr.Length, sampleArr, _rng);

        // Sort the samples.
        // Note. This results in the neural net connections being sorted by sourceID then targetID.
        Array.Sort(sampleArr);

        // Create the connection gene arrays and populate them.
        ConnectionGenes<T> connGenes = new(connectionCount);
        DirectedConnection[] connArr = connGenes._connArr;
        T[] weightArr = connGenes._weightArr;

        for(int i=0; i < sampleArr.Length; i++)
        {
            DirectedConnection cdef = _connectionDefArr[sampleArr[i]];

            connArr[i] = new DirectedConnection(
                cdef.SourceId,
                cdef.TargetId);

            weightArr[i] = _connWeightDist.Sample(_rng);
        }

        // Create a new genome with a new ID, birth generation of zero.
        int id = _genomeIdSeq.Next();
        return _genomeBuilder.Create(id, 0, connGenes);
    }

    #endregion

    #region Public Static Factory Method

    /// <summary>
    /// Create a new <see cref="NeatPopulation{T}"/> with randomly initialised genomes.
    /// Genomes are randomly initialised by giving each a random subset of all possible connections between the
    /// input and output layer.
    /// </summary>
    /// <param name="metaNeatGenome">Genome metadata, conveys e.g. the number of input and output nodes that each
    /// genome should have.</param>
    /// <param name="connectionsProportion">The proportion of possible connections between the input and output
    /// layers, to create in each new genome.</param>
    /// <param name="popSize">Population size.</param>
    /// <param name="rng">Random source (optional).</param>
    /// <returns>A new instance of <see cref="NeatPopulation{T}"/>.</returns>
    public static NeatPopulation<T> CreatePopulation(
        MetaNeatGenome<T> metaNeatGenome,
        double connectionsProportion,
        int popSize,
        IRandomSource? rng = null)
    {
        var factory = new NeatPopulationFactory<T>(
            metaNeatGenome,
            connectionsProportion,
            rng ?? RandomDefaults.CreateRandomSource());

        return factory.CreatePopulation(popSize);
    }

    /// <summary>
    /// Create a new <see cref="NeatPopulation{T}"/> based on a single seed genome.
    /// </summary>
    /// <param name="metaNeatGenome">Genome metadata, conveys e.g. the number of input and output nodes that each
    /// genome should have.</param>
    /// <param name="popSize">Population size.</param>
    /// <param name="seedGenome">The seed genome.</param>
    /// <param name="reproductionAsexualSettings">Asexual reproduction settings.</param>
    /// <param name="weightMutationScheme">Connection weight mutation scheme.</param>
    /// <param name="rng">Random source (optional).</param>
    /// <returns>A new instance of <see cref="NeatPopulation{T}"/>.</returns>
    public static NeatPopulation<T> CreatePopulation(
        MetaNeatGenome<T> metaNeatGenome,
        int popSize,
        NeatGenome<T> seedGenome,
        NeatReproductionAsexualSettings reproductionAsexualSettings,
        WeightMutationScheme<T> weightMutationScheme,
        IRandomSource? rng = null)
    {
        // Create a genome list with the single seed genome (for starters).
        List<NeatGenome<T>> genomeList = new(popSize)
        {
            seedGenome
        };

        var genomeBuilder = NeatGenomeBuilderFactory<T>.Create(metaNeatGenome);
        NeatPopulation<T> neatPop = new(metaNeatGenome, genomeBuilder, popSize, genomeList);

        // Create additional genomes by spawning offspring from the seed genome.
        NeatReproductionAsexual<T> reproductionAsexual = new(
            metaNeatGenome,
            genomeBuilder,
            neatPop.GenomeIdSeq,
            neatPop.InnovationIdSeq,
            new Int32Sequence(),
            neatPop.AddedNodeBuffer,
            reproductionAsexualSettings,
            weightMutationScheme);

        rng ??= RandomDefaults.CreateRandomSource();

        for(int i=1; i < popSize; i++)
        {
            NeatGenome<T> childGenome = reproductionAsexual.CreateChildGenome(seedGenome, rng);
            genomeList.Add(childGenome);
        }

        return neatPop;
    }

    /// <summary>
    /// Create a new <see cref="NeatPopulation{T}"/> based on one of more seed genomes.
    /// </summary>
    /// <param name="metaNeatGenome">Genome metadata, conveys e.g. the number of input and output nodes that each
    /// genome should have.</param>
    /// <param name="popSize">Population size.</param>
    /// <param name="seedGenomes">The seed genomes.</param>
    /// <param name="reproductionAsexualSettings">Asexual reproduction settings.</param>
    /// <param name="weightMutationScheme">Connection weight mutation scheme.</param>
    /// <param name="rng">Random source (optional).</param>
    /// <returns>A new instance of <see cref="NeatPopulation{T}"/>.</returns>
    public static NeatPopulation<T> CreatePopulation(
        MetaNeatGenome<T> metaNeatGenome,
        int popSize,
        List<NeatGenome<T>> seedGenomes,
        NeatReproductionAsexualSettings reproductionAsexualSettings,
        WeightMutationScheme<T> weightMutationScheme,
        IRandomSource? rng = null)
    {
        // If the number of seed genomes is greater then or equal to popSizem, then we simply use the provided
        // seed genomes directly, and truncate seedGenomes to the required size.
        if(seedGenomes.Count >= popSize)
        {
            return new NeatPopulation<T>(
                metaNeatGenome,
                NeatGenomeBuilderFactory<T>.Create(metaNeatGenome),
                popSize,
                seedGenomes.Take(popSize).ToList());
        }

        // Create a genome list with the seed genomes (for starters).
        List<NeatGenome<T>> genomeList = new(popSize);
        genomeList.AddRange(seedGenomes);

        var genomeBuilder = NeatGenomeBuilderFactory<T>.Create(metaNeatGenome);
        NeatPopulation<T> neatPop = new(metaNeatGenome, genomeBuilder, popSize, genomeList);

        // Create additional genomes by spawning offspring from the seed genomes.
        NeatReproductionAsexual<T> reproductionAsexual = new(
            metaNeatGenome,
            genomeBuilder,
            neatPop.GenomeIdSeq,
            neatPop.InnovationIdSeq,
            new Int32Sequence(),
            neatPop.AddedNodeBuffer,
            reproductionAsexualSettings,
            weightMutationScheme);

        // Span<NeatGenome<T>> seedSpan = CollectionsMarshal.AsSpan(seedGenomes);
        rng ??= RandomDefaults.CreateRandomSource();

        int seedCount = genomeList.Count;
        for (int i = genomeList.Count; i < popSize; i++)
        {
            NeatGenome<T> seedGenome = seedGenomes[i % seedCount];
            NeatGenome<T> childGenome = reproductionAsexual.CreateChildGenome(seedGenome, rng);
            genomeList.Add(childGenome);
        }

        return neatPop;
    }

    #endregion
}
