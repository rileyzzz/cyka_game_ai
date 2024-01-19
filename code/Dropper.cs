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


	TimeSince timeSinceLastMouseMove = 0f;

	protected override void OnUpdate()
	{
		if ( !Manager.Playing ) return;

		/*
		if ( Input.AnalogLook.yaw != 0f )
		{
			timeSinceLastMouseMove = 0f;
		}

		if ( timeSinceLastMouseMove < 0.5f )
		{
			hspeed = hspeed.LerpTo( Input.AnalogLook.yaw * Speed * 10f, Time.Delta * LerpSpeed * 5f );
		}
		else if ( Input.Down( "Left" ) )
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


		if ( Input.Pressed( "Jump" ) || Input.Pressed( "attack1" ) )
		{
			Drop();
		}
		*/
	}

	public void SetDropPos(float y)
	{
		if ( !Manager.Playing ) return;
		y = (y * 2.0f - 1.0f) * Range;
		// y = y * Range;
		//y = Math.Clamp( y, -1, 1 ) * Range;
		y = Math.Clamp( y, -Range, Range );
		Transform.Position = Transform.Position.WithY( y );
	}

	public GameObject AIDrop(float y)
	{
		if ( !Manager.Playing ) return null;

		// denormalize from [0, 1] to [-Range, Range]

		SetDropPos( y );
		return Drop();
	}

	GameObject Drop()
	{
		if ( timeSinceLastDrop < 0.25f )
			return null;

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

		return obj;
	}
}
