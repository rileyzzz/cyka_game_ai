// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
using Sandbox;
using System.Diagnostics;
using System.Linq.Expressions;

namespace SharpNeat.NeuralNets.Double;

/// <summary>
/// This class is functionally equivalent to <see cref="NeuralNetCyclic"/>, but doesn't use any of the unsafe
/// memory pointer techniques used in that class, and therefore this class is much slower.
///
/// This class is intended to be used as a safe reference implementation that can be used when testing or
/// debugging <see cref="NeuralNetCyclic"/>.
/// </summary>
public sealed class NeuralNetCyclicSafe : IBlackBox<double>
{
    // Connection arrays.
    readonly ConnectionIds _connIds;
    readonly double[] _weightArr;

    // Activation function.
    readonly VecFn2<double> _activationFn;

    // Node pre- and post-activation signal arrays.
    readonly double[] _preActivationArr;
    readonly double[] _postActivationArr;

    // Convenient counts.
    readonly int _inputCount;
    readonly int _outputCount;
    readonly int _cyclesPerActivation;

    #region Constructor

    /// <summary>
    /// Constructs a cyclic neural network.
    /// </summary>
    /// <param name="digraph">The weighted directed graph that defines the neural network structure and connection weights.</param>
    /// <param name="activationFn">The neuron activation function to use at all neurons in the network.</param>
    /// <param name="cyclesPerActivation">The number of activation cycles to perform per overall activation of the cyclic network.</param>
    public NeuralNetCyclicSafe(
        WeightedDirectedGraph<double> digraph,
        VecFn2<double> activationFn,
        int cyclesPerActivation)
    : this(
         digraph,
         digraph.WeightArray,
         activationFn,
         cyclesPerActivation)
    {
    }

    /// <summary>
    /// Constructs a cyclic neural network.
    /// </summary>
    /// <param name="digraph">The directed graph that defines the neural network structure.</param>
    /// <param name="weightArr">An array of neural network connection weights.</param>
    /// <param name="activationFn">The neuron activation function to use at all neurons in the network.</param>
    /// <param name="cyclesPerActivation">The number of activation cycles to perform per overall activation of the cyclic network.</param>
    public NeuralNetCyclicSafe(
        DirectedGraph digraph,
        double[] weightArr,
        VecFn2<double> activationFn,
        int cyclesPerActivation)
    {
        Sandbox.Diagnostics.Assert.True(digraph.ConnectionIds.GetSourceIdSpan().Length == weightArr.Length);

        // Store refs to network structure data.
        _connIds = digraph.ConnectionIds;
        _weightArr = weightArr;

        // Store network activation function and parameters.
        _activationFn = activationFn;
        _cyclesPerActivation = cyclesPerActivation;

        // Store input/output node counts.
        _inputCount = digraph.InputCount;
        _outputCount = digraph.OutputCount;

        // Create node pre- and post-activation signal arrays.
        int nodeCount = digraph.TotalNodeCount;
        _preActivationArr = new double[nodeCount];
        _postActivationArr = new double[nodeCount];

		// Map the input and output vectors to the corresponding segments of _postActivationArr.
		Inputs = new ArrayRef<double>( _postActivationArr, 0, _inputCount );

		// Note. Output neurons follow input neurons in the arrays.
		Outputs = new ArrayRef<double>( _postActivationArr, _inputCount, _outputCount );
	}

    #endregion

    #region IBlackBox

    /// <summary>
    /// Gets a memory segment that represents a vector of input values.
    /// </summary>
    public ArrayRef<double> Inputs { get; }

    /// <summary>
    /// Gets a memory segment that represents a vector of output values.
    /// </summary>
    public ArrayRef<double> Outputs { get; }

    /// <summary>
    /// Activate the network for a fixed number of iterations defined by the 'maxIterations' parameter
    /// at construction time. Activation reads input signals from InputSignalArray and writes output signals
    /// to OutputSignalArray.
    /// </summary>
    public void Activate()
    {
        ReadOnlySpan<int> srcIds = _connIds.GetSourceIdSpan();
        ReadOnlySpan<int> tgtIds = _connIds.GetTargetIdSpan();
        ReadOnlySpan<double> weights = _weightArr.AsSpan();
        Span<double> preActivations = _preActivationArr.AsSpan();
        Span<double> postActivations = _postActivationArr.AsSpan();

        // Note. Here we skip over the activations corresponding to the input neurons, as these have no
        // incoming connections, and therefore have fixed post-activation values and are never activated.
        int nonInputCount = _preActivationArr.Length - _inputCount;
        Span<double> preActivationsNonInputs = preActivations.Slice(_inputCount);
        Span<double> postActivationsNonInputs = postActivations.Slice(_inputCount);

        // Activate the network for a fixed number of timesteps.
        for(int i=0; i < _cyclesPerActivation; i++)
        {
            // Loop connections. Get each connection's input signal, apply the weight and add the result to
            // the pre-activation signal of the target neuron.
            for(int j=0; j < srcIds.Length; j++)
            {
                preActivations[tgtIds[j]] =
                    Math.FusedMultiplyAdd(
                        postActivations[srcIds[j]],
                        weights[j],
                        preActivations[tgtIds[j]]);
            }

            // Pass the pre-activation levels through the activation function, storing the results in the
            // post-activation span/array.
            _activationFn(
                preActivationsNonInputs.Slice( 0, nonInputCount ),
                postActivationsNonInputs.Slice( 0, nonInputCount ));

            // Reset the elements of _preActivationArray that represent the output and hidden nodes.
            preActivationsNonInputs.Clear();
        }
    }

    /// <summary>
    /// Reset the network's internal state.
    /// </summary>
    public void Reset()
    {
        // Reset the elements of _preActivationArray and _postActivationArr that represent the
        // output and hidden nodes.
        int nonInputCount = _preActivationArr.Length - _inputCount;
        Array.Clear(_preActivationArr, _inputCount, nonInputCount);
        Array.Clear(_postActivationArr, _inputCount, nonInputCount);
    }

	void IGraphDraw.DrawGraph( Sandbox.Rect rect, Color color )
	{
		ReadOnlySpan<int> srcIds = _connIds.GetSourceIdSpan();
		ReadOnlySpan<int> tgtIds = _connIds.GetTargetIdSpan();
		ReadOnlySpan<double> weights = _weightArr.AsSpan();

		int iNode;

		var nodePos = new Dictionary<int, Vector2>();
		var nodeCon = new List<(int, int, double)>();


		// Setup input positions.
		float draw_x = rect.Left;
		for ( iNode = 0; iNode < _inputCount; iNode++ )
		{
			float draw_y = rect.Top + rect.Size.y * ((float)(iNode + 0.5f) / _inputCount);
			nodePos[iNode] = new Vector2( draw_x, draw_y );
		}

		// Setup output positions.
		draw_x = rect.Right;
		for ( iNode = 0; iNode < _outputCount; iNode++ )
		{
			float draw_y = rect.Top + rect.Size.y * ((float)(iNode + 0.5f) / _outputCount);
			nodePos[_inputCount + iNode] = new Vector2( draw_x, draw_y );
		}

		// Loop connections.
		for ( int j = 0; j < srcIds.Length; j++ )
		{
			nodeCon.Add( (srcIds[j], tgtIds[j], weights[j]) );
		}


		// Scale up the rectangle.
		const float scale = 2.0f;
		var extents = rect.Size * 0.5f;
		var center = rect.Center;
		rect.Left = center.x - extents.x * scale;
		rect.Right = center.x + extents.x * scale;
		rect.Top = center.y - extents.y * scale;
		rect.Bottom = center.y + extents.y * scale;

		// Draw the links.
		Graphics.Attributes.Set( "LayerMat", Matrix.Identity );
		Graphics.Attributes.Set( "Texture", Texture.White );
		Graphics.Attributes.SetCombo( "D_BLENDMODE", BlendMode.Normal );

		foreach ( var (src, dst, weight) in nodeCon )
		{
			var srcPos = nodePos[src];
			var dstPos = nodePos[dst];

			const float drawSize = 0.5f;

			var baseCol = dst > src ? Color.Blue : Color.Orange;
			var lineCol = baseCol * Math.Abs( (float)weight );
			lineCol.a = 1.0f;

			Vertex[] v = new Vertex[4];
			v[0] = new Vertex( srcPos - Vector2.Up * drawSize, new Vector2( 0f, 0f ), color * lineCol );
			v[1] = new Vertex( dstPos - Vector2.Up * drawSize, new Vector2( 1f, 0f ), color * lineCol );
			v[2] = new Vertex( srcPos + Vector2.Up * drawSize, new Vector2( 0f, 1f ), color * lineCol );
			v[3] = new Vertex( dstPos + Vector2.Up * drawSize, new Vector2( 1f, 1f ), color * lineCol );

			Sandbox.Graphics.Draw( v, 4, Material.UI.Basic, null, Graphics.PrimitiveType.TriangleStrip );
		}

		// Now draw the nodes.
		foreach ( var (nodeId, pos) in nodePos )
		{
			const float drawSize = 3f;
			var nodeRect = new Sandbox.Rect( pos - (drawSize * 0.5f) * Vector2.One, drawSize * Vector2.One );
			Sandbox.Graphics.DrawRoundedRectangle( nodeRect, color * Color.Green, Vector4.One * drawSize * 0.5f );
		}
	}

	#endregion

	#region IDisposable

	/// <inheritdoc/>
	public void Dispose()
    {
    }

    #endregion
}
