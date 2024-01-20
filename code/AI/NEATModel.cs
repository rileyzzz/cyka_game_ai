using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sandbox;
using SharpNeat;
using Sandbox.Diagnostics;
using Sandbox.UI;
using SharpNeat.Experiments;
using SharpNeat.NeuralNets;
using SharpNeat.Experiments.ConfigModels;
using SharpNeat.IO;
using SharpNeat.Neat.Genome.Double;
using SharpNeat.Neat;
using SharpNeat.Neat.Genome.IO;
using Sandbox.Razor;
using SharpNeat.Neat.EvolutionAlgorithm;

public class NEATScenePanel : ScenePanel
{
	private NEATScene _Scene;
	private SceneOverlay Overlay;

	public new NEATScene Scene
	{
		get => _Scene;
		set
		{
			_Scene = value;
			base.World = _Scene.Scene.SceneWorld;
		}
	}

	public NEATScenePanel()
	{
		// setup camera
		Camera.Position = new Vector3( -834.644f, 0.0f, 374.784f );
		Camera.ZNear = 10;
		Camera.ZFar = 10000;

		Camera.Ortho = true;
		Camera.OrthoHeight = 964.403f;
		Camera.OrthoWidth = Camera.OrthoHeight;

		Overlay = AddChild<SceneOverlay>();
		// Overlay.Scene = Scene;
	}

	protected override void BuildRenderTree( RenderTreeBuilder tree )
	{
		base.BuildRenderTree( tree );
	}

	private float hoverAlpha = 0.0f;
	private float hoverSpeed = 0.0f;
	public void DrawInputOverlay( ref RenderState state )
	{
		var inputs = Scene.DebugInputs;
		var outputs = Scene.DebugOutputs;
		if ( inputs == null || outputs == null )
			return;

		bool hasHover = this.HasHovered;

		hoverAlpha = MathX.SmoothDamp( hoverAlpha, hasHover ? 1 : 0, ref hoverSpeed, 0.1f, Time.Delta );

		if (hoverAlpha > 0.01f)
		{
			Color col = new Color( 1, 1, 1, hoverAlpha );

			Graphics.DrawText( this.Box.Rect.TopLeft + new Vector2( -32.0f, 16.0f ), $"Next: {inputs[1].ToString( "0.00" )}", col );
			Graphics.DrawText( this.Box.Rect.TopLeft + new Vector2( -32.0f, 32.0f ), $"Time: {inputs[2].ToString( "0.00" )}", col );

			for ( int i = 3; i < inputs.Length; i += 3 )
			{
				double size = inputs[i + 0] * 0.5f + 0.5f;
				double x = inputs[i + 1] * 0.5f + 0.5f;
				double y = inputs[i + 2] * 0.5f + 0.5f;

				// fit over UI.
				x = MathX.Lerp( (float)(1 - x), 0.5f, 0.55f );
				y = MathX.Lerp( (float)(1 - y), 0.5f, 0.3f );

				var pos = this.Box.Rect.TopLeft + this.Box.Rect.Size * new Vector2( (float)x, (float)y );
				float drawSize = ((float)size * 11) * 8;

				var rect = new Rect( pos - (drawSize * 0.5f) * Vector2.One, drawSize * Vector2.One );
				Graphics.DrawRoundedRectangle( rect, col, Vector4.One * drawSize * 0.5f );
			}
		}
	}

	public override void DrawContent( ref RenderState state )
	{
		base.DrawContent( ref state );

		// Gizmo.Draw.te
	}
}

public class NEATScene : Component
{
	// public static SceneFile DefaultScene = ResourceLibrary.Get<SceneFile>( "game.scene" );
	public NEATManager Manager { get; set; }
	public new Scene Scene { get; private set; }

	private TimeSince TimeSinceLastDrop;

	public double[] DebugInputs { get; private set; }
	public double[] DebugOutputs { get; private set; }

	public bool IsSimulating { get; private set; } = false;
	private GameObject LastBall;
	public NEATScene()
	{
		//Manager = manager;
		Scene = new Scene();
	}

	public bool IsLeader()
	{
		return Manager?.IsSceneLeader( this ) ?? false;
	}

	protected override void OnAwake()
	{
		Scene.LoadFromFile( "game.scene" );

		Log.Info( "Loaded scene." );
		//Reset();
	}

	//public new void Reset()
	//{
	//	var manager = Scene.GetAllComponents<CykaManager>().FirstOrDefault();
	//	// manager.StartGame();

	//}

	private void Start()
	{
		var manager = Scene.GetAllComponents<CykaManager>().FirstOrDefault();
		manager.EndGame();
		manager.StartGame();

		var dropper = Scene.GetAllComponents<Dropper>().FirstOrDefault();
		dropper.UpNext = 1;

		var deathzone = Scene.GetAllComponents<DeathZone>().FirstOrDefault();
		deathzone.TimeInZone = 0;

		LastBall = null;
		TimeSinceLastDrop = 0;
	}

	private object sceneLock = new();

	protected override void OnUpdate()
	{
		float dt = Time.Delta;
		float now = Time.Now;

		if ( !IsSimulating )
			return;

		lock ( sceneLock )
		{
			using ( Scene.Push() )
			{
				// use parent timing.
				using var scope = Time.Scope( now, dt );
				Scene.GameTick();
				Scene.PhysicsWorld.Step( dt );
			}
		}
	}

	public void SetDebugInputState( Span<double> inputs )
	{
		DebugInputs = inputs.ToArray();

		// Log.Info( $"in {inputs[0]} {inputs[1]}" );
	}

	public void SetDebugOutputState( Span<double> outputs )
	{
		DebugOutputs = outputs.ToArray();

		// Log.Info($"cond {outputs[0]} {outputs[1]}");
	}

	public GameObject TryDropBall( CykaOutputState state )
	{
		float dt = Time.Delta;
		float now = Time.Now;

		GameObject obj = null;
		lock ( sceneLock )
		{
			using ( Scene.Push() )
			{
				// use parent timing.
				using var scope = Time.Scope( now, dt );
				var dropper = Scene.GetAllComponents<Dropper>().FirstOrDefault();

				if ( state.DropCondition > 0.5f )
				{
					obj = dropper.AIDrop( state.DropPosition );
				}
				else
				{
					// Just update the UI so we know it's still thinking.
					dropper.SetDropPos( state.DropPosition );
				}
			}
		}


		if ( obj != null )
		{
			TimeSinceLastDrop = 0;
			LastBall = obj;
		}

		return obj;
	}

	//private async Task TestDrop()
	//{
	//	await GameTask.DelaySeconds( 2.0f );
	//	var ball = DropBall( new CykaOutputState() { DropPosition = 0.5f } );

	//	await NEATScene.WaitForBallToSettle( ball );
	//}

	public void BeginSimulation( NEATManager manager )
	{
		Manager = manager;
		IsSimulating = true;

		Start();
	}

	public void EndSimulation()
	{
		// Gray out the window.
		IsSimulating = false;
	}

	protected override void OnDestroy()
	{
		Scene?.Destroy();
		Scene = null;
	}

	private Vector3 LastBallPos;
	public bool CanRun()
	{
		var manager = Scene.GetAllComponents<CykaManager>().FirstOrDefault();

		// Game over.
		if ( !manager.Playing )
			return false;

		if ( LastBall != null && TimeSinceLastDrop < 5.0f )
		{
			Vector3 ballPos = LastBall.Transform.Position;
			Vector3 delta = ballPos - LastBallPos;
			LastBallPos = ballPos;

			// keep running if the ball is moving significantly.
			if ( delta.LengthSquared > 0.5f )
				return true;
		}

		// AI has died, hasn't dropped a ball in a while.
		if ( TimeSinceLastDrop > 5.0f )
			return false;

		return true;
	}

	public void GetInputs( out CykaInputState state )
	{
		state = new();

		var manager = Scene.GetAllComponents<CykaManager>().FirstOrDefault();
		var dropper = Scene.GetAllComponents<Dropper>().FirstOrDefault();

		state.NextSize = (dropper.UpNext / 11.0f) * 2.0f - 1.0f;
		state.TimeSinceLastDrop = TimeSinceLastDrop;

		var deathzone = Scene.GetAllComponents<DeathZone>().FirstOrDefault();
		Assert.NotNull( deathzone );
		float deathZ = deathzone.Transform.Position.z;

		var balls = manager.Balls
			.Select( x => x.Components.Get<BallComponent>() )
			.OrderByDescending( x => x.Transform.Position.z )
			.ToArray();

		//if ( balls.Length > CykaInputState.NumTrackedBalls )
		//	Log.Info($"Exceeded max tracked ball count! {balls.Length}");

		const float boardHalfWidth = 248.556f;

		int nBalls = Math.Min( balls.Length, CykaInputState.NumTrackedBalls );
		for ( int i = 0; i < nBalls; i++ )
		{
			ref CykaBall data = ref state.Balls[i];

			//float ypos = balls[i].Transform.Position.z / deathZ * 0.95f;
			float ypos = balls[i].Transform.Position.z / deathZ * 2.0f - 1;
			ypos *= 0.95f;
			float xpos = balls[i].Transform.Position.y / boardHalfWidth;
			
			// Normalize inputs.
			xpos = Math.Clamp( xpos, -1.0f, 1.0f );
			ypos = Math.Clamp( ypos, -1.0f, 1.0f );

			data.Pos = new Vector2( xpos, ypos );
			data.Size = (balls[i].Size / 11.0f) * 2.0f - 1.0f;
		}

		for ( int i = nBalls; i < CykaInputState.NumTrackedBalls; i++ )
		{
			ref CykaBall data = ref state.Balls[i];
			data.Pos = new Vector3(0, -1);
			data.Size = -1;
		}
	}

	//public static async Task WaitForBallToSettle( GameObject obj )
	//{
	//	if (obj == null)
	//	{
	//		Log.Info("no ball?");
	//		return;
	//	}

	//	// wait for the ball to settle.
	//	Vector3 lastPos = obj.Transform.Position;

	//	// Minimum wait of 0.5 seconds.
	//	await GameTask.DelaySeconds( 0.5f );
	//	while ( true )
	//	{
	//		Vector3 pos = obj.Transform.Position;
	//		if ( Vector3.DistanceBetweenSquared( pos, lastPos ) < 1.0f )
	//			break;

	//		lastPos = pos;

	//		// check again in a bit.
	//		await GameTask.DelaySeconds( 0.5f );
	//	}
	//}

	public long GetFitnessScore()
	{
		var manager = Scene.GetAllComponents<CykaManager>().FirstOrDefault();
		return manager.Score;
	}
}

public class NEATManager : Component
{
	//public static NEATManager Current { get; private set; }
	[Property] public GameObject SceneTemplate { get; set; }
	public List<GameObject> Scenes { get; private set; } = new();
	public List<NEATScene> SceneLeaderboard { get; private set; } = new();

	private ExperimentConfig Config;

	private EvolutionAlgorithmStatistics CurStats = null;
	private PopulationStatistics CurPopStats = null;

	public int Generation => CurStats?.Generation ?? 0;

	public double BestFitness => CurPopStats?.BestFitness.PrimaryFitness ?? default;
	public double MeanFitness => CurPopStats?.MeanFitness ?? default;

	protected override void OnAwake()
	{
		//Assert.True( Current == null );
		//if ( Current != null )
		//	Current = this;
	}

	protected override void OnStart()
	{
		Log.Info( "NEAT manager loaded." );

		LoadConfig();
		_ = Train();

		// GameTask.RunInThreadAsync( () => Train() );
	}

	private bool Training = false;
	protected override void OnUpdate()
	{
		UpdateSceneLeaderboard();
	}

	int NumActiveScenes = 0;

	public NEATScene AllocateScene()
	{
		if ( NumActiveScenes >= Scenes.Count )
		{
			var scene = SceneTemplate.Clone( global::Transform.Zero, this.GameObject );
			Scenes.Add( scene );
		}

		var component = Scenes[NumActiveScenes++].Components.Get<NEATScene>();
		Assert.NotNull( component );
		component.BeginSimulation( this );
		return component;
	}

	//public void RemoveScene( NEATScene scene )
	//{
	//	scene.Destroy();
	//	Scenes.Remove( scene.GameObject );
	//}

	private void ResetScenes()
	{
		NumActiveScenes = 0;

		SceneLeaderboard.Clear();

		//foreach ( var scene in Scenes )
		//	scene.Destroy();
		//Scenes.Clear();
	}

	private void UpdateSceneLeaderboard()
	{
		SceneLeaderboard.Clear();
		foreach ( var obj in Scenes )
		{
			var component = obj.Components.Get<NEATScene>();
			if ( component != null && component.IsSimulating )
				SceneLeaderboard.Add( component );
		}

		SceneLeaderboard.Sort( ( a, b ) => { return (int)b.GetFitnessScore() - (int)a.GetFitnessScore(); } );
	}

	public bool IsSceneLeader( NEATScene scene )
	{
		if ( SceneLeaderboard.Count == 0 )
			return false;

		return SceneLeaderboard.IndexOf( scene ) == 0 && scene.GetFitnessScore() != 0;
	}

	private void LoadConfig()
	{
		using var fs = FileSystem.Mounted.OpenRead( "code/AI/cyka_config.json" );
		Config = JsonUtils.Deserialize<ExperimentConfig>( fs );
	}


	public async Task Train()
	{
		ResetScenes();

		// Training loop.

		// Create an evaluation scheme object for the prey capture task.
		var evalScheme = new CykaEvaluationScheme( this );

		// Create a NeatExperiment object with the evaluation scheme,
		// and assign some default settings (these can be overridden by config).
		var experiment = new NeatExperiment<double>( evalScheme, "cyka" )
		{
			IsAcyclic = false,
			CyclesPerActivation = 1,
			ActivationFnName = ActivationFunctionId.LeakyReLU.ToString()
		};

		// Apply configuration to the experiment instance.
		experiment.Configure( Config );

		// Create a NeatEvolutionAlgorithm instance ready to run the experiment.
		NeatEvolutionAlgorithm<double> ea = null;
		// var ea = NeatUtils.CreateNeatEvolutionAlgorithm( experiment );

		//Generation = 0;

		const bool loadedFromFile = false;
		if (loadedFromFile)
		{
			// Create a MetaNeatGenome.
			var metaNeatGenome = NeatUtils.CreateMetaNeatGenome( experiment );

			NeatPopulationLoader<double> loader = new( metaNeatGenome );
			string populationFolderPath = Path.Combine( "model", "gen199" );
			List<NeatGenome<double>> genomeList = loader.LoadFromFolder( populationFolderPath );

			INeatGenomeBuilder<double> genomeBuilder = NeatGenomeBuilderFactory<double>.Create( metaNeatGenome );

			var pop = new NeatPopulation<double>(
				metaNeatGenome,
				genomeBuilder,
				experiment.PopulationSize,
				genomeList );

			ea = NeatUtils.CreateNeatEvolutionAlgorithm( experiment, pop );
		}
		else
		{
			ea = NeatUtils.CreateNeatEvolutionAlgorithm( experiment );
		}

		await ea.Initialise();

		CurStats = ea.Stats;

		var neatPop = ea.Population;
		CurPopStats = neatPop.Stats;

		// Begin running.
		while ( true )
		{
			ResetScenes();

			//foreach ( var scene in Scenes )
			//	scene.Components.Get<NEATScene>().Start();

			Log.Info( $"Begin generation {ea.Stats.Generation}" );
			await ea.PerformOneGeneration();
			Log.Info( $"{ea.Stats.Generation} {neatPop.Stats.BestFitness.PrimaryFitness} {neatPop.Stats.MeanComplexity} {ea.ComplexityRegulationMode} {neatPop.Stats.MeanFitness}" );

			if ( !loadedFromFile )
			{
				Log.Info("Saving...");
				string dir = $"model";
				string file = $"gen{ea.Stats.Generation}";

				// Make sure parent dir exists.
				FileSystem.Data.CreateDirectory( dir );

				// Clear the previous model.
				string fullDir = Path.Combine( dir, file );
				if ( FileSystem.Data.DirectoryExists( fullDir ) )
					FileSystem.Data.DeleteDirectory( fullDir, true );

				NeatPopulationSaver.SaveToFolder<double>( neatPop.GenomeList, dir, file );
			}
			
			await GameTask.DelaySeconds( 1.0f );
		}
	}
}
