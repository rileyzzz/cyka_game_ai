using System.Collections.Generic;
using Sandbox;

public sealed class CykaManager : Component
{
	[Property] public string LeaderboardName { get; set; } = "highscores";
	public long Score { get; private set; } = 0;
	public long HighScore { get; private set; } = 0;
	public Sandbox.Services.Leaderboards.Board Leaderboard;

	protected override void OnStart()
	{
		FetchLeaderboardInfo();
	}

	protected override void OnUpdate()
	{

	}

	public void AddScore( int size )
	{
		var score = 0;
		for ( int i = 0; i < size - 1; i++ )
		{
			score += i + 1;
		}

	}

	async void FetchLeaderboardInfo()
	{
		Leaderboard = Sandbox.Services.Leaderboards.Get( LeaderboardName );
		await Leaderboard.Refresh();
		foreach ( var entry in Leaderboard.Entries )
		{
			if ( entry.SteamId == Game.SteamId )
			{
				HighScore = (long)entry.Value;
			}
		}
	}

}