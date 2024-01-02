using System;
using Sandbox;

public sealed class Dropper : Component
{
	[Property] CykaManager Manager { get; set; }
	[Property] GameObject BallPrefab { get; set; }
	[Property] float Speed { get; set; } = 1f;
	[Property] float LerpSpeed { get; set; } = 10f;
	[Property] float Range { get; set; } = 100f;

	public int UpNext = 1;

	float hspeed = 0f;
	TimeSince timeSinceLastDrop = 0f;

	protected override void OnUpdate()
	{
		if ( !Manager.Playing ) return;

		if ( Input.Down( "Left" ) )
		{
			hspeed = hspeed.LerpTo( Speed, Time.Delta * LerpSpeed );
		}
		else if ( Input.Down( "Right" ) )
		{
			hspeed = hspeed.LerpTo( -Speed, Time.Delta * LerpSpeed );
		}
		else
		{
			hspeed = hspeed.LerpTo( 0f, Time.Delta * LerpSpeed );
		}

		var y = Transform.Position.y;
		y += Time.Delta * hspeed;
		y = Math.Clamp( y, -Range, Range );
		Transform.Position = Transform.Position.WithY( y );


		if ( Input.Pressed( "Jump" ) )
		{
			Drop();
		}
	}

	void Drop()
	{
		if ( timeSinceLastDrop < 0.25f )
			return;

		var obj = SceneUtility.Instantiate( BallPrefab, Transform.Position + Vector3.Zero.WithY( Random.Shared.Float( -1f, 1f ) ) );
		var ball = obj.Components.Get<BallComponent>();
		if ( ball is not null )
		{
			ball.Manager = Manager;
			ball.Size = UpNext;
		}
		Manager.AddScore( UpNext );
		Manager.AddBall( obj );
		UpNext = Random.Shared.Int( 1, 5 );
		timeSinceLastDrop = 0;
	}
}