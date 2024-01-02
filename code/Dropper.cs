using System;
using Sandbox;

public sealed class Dropper : Component
{
	[Property] GameObject BallPrefab { get; set; }
	[Property] float Speed { get; set; } = 1f;
	[Property] float LerpSpeed { get; set; } = 10f;
	[Property] float Range { get; set; } = 100f;

	float hspeed = 0f;
	TimeSince timeSinceLastDrop = 0f;

	protected override void OnUpdate()
	{
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

		SceneUtility.Instantiate( BallPrefab, Transform.Position + Vector3.Zero.WithY( Random.Shared.Float( -1f, 1f ) ) );
		timeSinceLastDrop = 0;
	}
}