using System;
using System.Linq;
using Sandbox;

public struct ArrayRef<T>
{
	public T[] Arr { get; set; }
	int Start;
	int Length;

	public ArrayRef( T[] arr, int start, int length )
	{
		Arr = arr;
		Start = start;
		Length = length;
	}

	public static implicit operator ArrayRef<T>( T[] arr )
	{
		return new ArrayRef<T>( arr, 0, arr.Length );
	}

	public Span<T> AsSpan() { return new Span<T>( Arr, Start, Length ); }
}

public static class ArrayExtensions
{
	public static T[] CloneSafe<T>( this T[] array )
	{
		if ( array == null )
			return null;

		T[] obj = new T[array.LongLength];
		array.CopyTo( obj, 0 );
		return obj;
	}

	public static float[,] CloneSafe( this float[,] array )
	{
		if ( array == null )
			return null;

		// LongLength trips a whitelist error for some reason. :)
		//T[,] obj = new T[array.GetLongLength(0), array.GetLongLength(1)];
		float[,] obj = new float[array.GetLength( 0 ), array.GetLength( 1 )];

		// Fuck off.
		for ( int i = 0; i < array.GetLength( 0 ); i++ )
		{
			for ( int j = 0; j < array.GetLength( 1 ); j++ )
				obj[i, j] = array[i, j];
		}

		return obj;
	}
}

public class AggregateException : Exception
{
	public AggregateException(string msg)
		: base(msg)
	{
	}
}
