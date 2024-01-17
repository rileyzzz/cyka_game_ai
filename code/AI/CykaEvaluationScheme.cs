using System;
using Sandbox;
using SharpNeat;
using SharpNeat.Evaluation;

public class CykaEvaluationScheme : IBlackBoxEvaluationScheme<double>
{
	NEATModel Model;

	/// <inheritdoc/>
	public int InputCount => CykaInputState.NumInputs;

	/// <inheritdoc/>
	public int OutputCount => CykaOutputState.NumOutputs;

	/// <inheritdoc/>
	public bool IsDeterministic => false;

	/// <inheritdoc/>
	public IComparer<FitnessInfo> FitnessComparer => PrimaryFitnessInfoComparer.Singleton;

	/// <inheritdoc/>
	public FitnessInfo NullFitness => FitnessInfo.DefaultFitnessInfo;

	/// <inheritdoc/>
	public bool EvaluatorsHaveState => true;

	public CykaEvaluationScheme( NEATModel model )
	{
		Model = model;
	}

	/// <inheritdoc/>
	public IPhenomeEvaluator<IBlackBox<double>> CreateEvaluator()
	{
		return new CykaBlackBoxEvaluator( Model );
	}

	/// <inheritdoc/>
	public bool TestForStopCondition( FitnessInfo fitnessInfo )
	{
		return (fitnessInfo.PrimaryFitness >= 100.0);
	}
}
