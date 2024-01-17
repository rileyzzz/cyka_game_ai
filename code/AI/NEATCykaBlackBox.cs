using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sandbox;
using SharpNeat;

struct CykaBall
{
	public int Size;
	// Relative position.
	// X is normalized [-1, 1].
	// Y is [0, 1] from the top (so the AI model knows to avoid negative numbers).
	public Vector2 Pos;

	public const int NumProps = 3;
}

struct CykaInputState
{
	public const int NumTrackedBalls = 16;
	public const int NumInputs = 1 + (CykaBall.NumProps * NumTrackedBalls);

	public int NextSize;
	public CykaBall[] Balls = new CykaBall[NumTrackedBalls];


	public CykaInputState()
	{
	}
}

struct CykaOutputState
{
	public const int NumOutputs = 1;

	public float DropPosition;
}

public class CykaBlackBoxEvaluator : IPhenomeEvaluator<IBlackBox<double>>
{
	NEATModel Model;

	public CykaBlackBoxEvaluator(NEATModel model)
	{
		Model = model;
	}

	// Fitness is simply the current score.
	FitnessInfo IPhenomeEvaluator<IBlackBox<double>>.Evaluate( IBlackBox<double> phenome )
	{
		// Evaluate a single iteration of the model.

	}
}
