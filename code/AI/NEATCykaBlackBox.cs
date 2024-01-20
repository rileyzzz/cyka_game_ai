using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sandbox;
using SharpNeat;
using Sandbox.Diagnostics;

public struct CykaBall
{
	public float Size;
	// Relative position.
	// X is normalized [-1, 1].
	// Y is [0, 1] from the top (so the AI model knows to avoid negative numbers).
	public Vector2 Pos;

	public const int NumProps = 3;
}

public struct CykaInputState
{
	public const int NumBasicInputs = 3;
	// For ease of training, only consider the top ~18 balls.
	public const int NumTrackedBalls = 18;
	public const int NumInputs = NumBasicInputs + (CykaBall.NumProps * NumTrackedBalls);

	public float NextSize;
	public float TimeSinceLastDrop;
	public CykaBall[] Balls = new CykaBall[NumTrackedBalls];


	public CykaInputState()
	{
	}

	public void Pack( Span<double> v )
	{
		v.Clear();
		Assert.True( v.Length == NumInputs );

		// Bias.
		v[0] = 1.0;

		v[1] = (double)NextSize;
		v[2] = (double)TimeSinceLastDrop;
		for (int i = 0; i < NumTrackedBalls; i++ )
		{
			v[NumBasicInputs + i * CykaBall.NumProps + 0] = (double)Balls[i].Size;
			v[NumBasicInputs + i * CykaBall.NumProps + 1] = (double)Balls[i].Pos.x;
			v[NumBasicInputs + i * CykaBall.NumProps + 2] = (double)Balls[i].Pos.y;
		}
	}
}

public struct CykaOutputState
{
	public const int NumOutputs = 2;

	public float DropCondition;
	public float DropPosition;

	public static CykaOutputState Unpack( Span<double> data )
	{
		Assert.True( data.Length == NumOutputs );

		CykaOutputState state = new CykaOutputState();
		//state.DropCondition = (float)data[0];
		//state.DropPosition = (float)data[1];

		state.DropPosition = (float)data[0];
		state.DropCondition = (float)data[1];

		return state;
	}
}

public class CykaBlackBoxEvaluator : IPhenomeEvaluator<IBlackBox<double>>
{
	NEATManager Manager;

	public CykaBlackBoxEvaluator( NEATManager manager )
	{
		Manager = manager;
	}

	// Fitness is simply the current score.
	async Task<FitnessInfo> IPhenomeEvaluator<IBlackBox<double>>.Evaluate( IBlackBox<double> phenome )
	{
		var scene = Manager.AllocateScene( phenome );

		// Evaluate a single iteration of the model.
		while ( scene.CanRun() )
		{
			scene.GetInputs( out CykaInputState inputState );
			inputState.Pack( phenome.Inputs.AsSpan() );

			scene.SetDebugInputState( phenome.Inputs.AsSpan() );
			phenome.Activate();
			scene.SetDebugOutputState( phenome.Outputs.AsSpan() );


			var outputState = CykaOutputState.Unpack( phenome.Outputs.AsSpan() );
			var ball = scene.TryDropBall( outputState );

			

			// Log.Info($"ball {ball}");
			// await NEATScene.WaitForBallToSettle( ball );
			// Log.Info("ran scene!");
			await GameTask.DelaySeconds( 0.05f );
		}

		Log.Info( "simulation ended." );

		long score = scene.GetFitnessScore();

		scene.EndSimulation();
		// Manager.RemoveScene( scene );
		return new FitnessInfo( (double)score );
	}
}
