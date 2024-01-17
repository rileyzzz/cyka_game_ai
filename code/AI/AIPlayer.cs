using System;
using Sandbox;

/// <summary>
/// An AI player.
/// Can be run in learning mode or simulation mode.
/// </summary>
public class AIPlayer : Component
{
	public AIPlayer()
	{
	}


	public void StartGame()
	{

	}

	private void GatherInputs()
	{
		var balls = Scene.GetAllComponents<BallComponent>();

	}
}
