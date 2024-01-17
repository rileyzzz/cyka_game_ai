﻿// This file is part of SharpNEAT; Copyright Colin D. Green.
// See LICENSE.txt for details.
namespace SharpNeat.Neat.Reproduction.Sexual.Strategy.UniformCrossover;

/// <summary>
/// For checking/testing if a proposed new connection on a NEAT genome would form a connectivity cycle.
/// </summary>
/// <remarks>
/// This class utilises a depth first graph traversal algorithm to check if a proposed new connection on a given
/// graph would form a cycle, as such it is assumed that the graph as given is acyclic, if it isn't then the graph
/// traversal stack will grown to infinity, ultimately resulting in an OutOfMemory exception.
///
/// The algorithm will perform a full depth first traversal of the graph starting at the proposed new connection's
/// target node, and if that connection's source node is encountered then it would form a cycle if it were added
/// to the graph.
///
/// Each instance of this class allocates a stack and a hashset for use by the traversal algorithm, and these
/// are cleared and re-used for each call to IsConnectionCyclic(). This avoids memory re-allocation and garbage
/// collection overhead, but the side effect is that IsConnectionCyclic() is not reentrant, i.e. can only be in
/// use by one execution thread at a given point in time. A reentrancy check will throw an exception if reentrancy
/// is attempted.
///
///
/// Implementation Details / Notes
/// ----------------------
/// This class is optimized for speed and efficiency and as such is tightly coupled with the connection gene list
/// data structure, and is perhaps not as easy to read/understand as a traditional depth first graph traversal
/// algorithm using function recursion. However this is essentially a depth first graph traversal algorithm that
/// utilises its own stack instead of using the call stack.
///
/// The traversal stack is a stack of Int32(s), each of which is an index into connList (the list of connections
/// that make up the graph, ordered by sourceId and then targetId). Thus, each stack entry points to a connection,
/// and represents traversal of that connection's source node and also which of that node's child connections/nodes
/// is the current traversal position/path from that node (note. this works because the connections are sorted by
/// sourceId first).
///
/// As such this algorithm has a far more compact stack frame than the equivalent algorithm implemented as a
/// recursive function, and avoids any other method call overhead as a further performance benefit (i.e. overhead
/// other than stack frame initialisation).
///
/// The main optimizations then are:
///
///    * No method call overhead from recursive method calls.
///
///    * Each stack frame is a single int32 and thus the stack as a whole is highly compact; this improves CPU cache
///      locality and hit rate, and also keeps the max size of the stack for any given traversal at a minimum.
///
///    * The stack and a visitedNodes HashSet are allocated for each class instance and are cleared and re-used for each
///      call to IsConnectionCyclic(), therefore minimizing memory allocation and garbage collection overhead.
///
///    * Using a stack on the heap also avoids any potential for a stack overflow on very deep graphs, which could occur
///      if using method call recursion.
///
/// Problems with the approach of this class are:
///
///    * The code is more complex than the same algorithm written as a recursive function; this makes the code harder
///      to read, understand and maintain, thus increasing the probability of subtle defects.
///
/// Also see:
/// <see cref="Graphs.Acyclic.CyclicConnectionCheck"/>
/// <see cref="Graphs.Acyclic.AcyclicGraphDepthAnalysis"/>
/// <see cref="CyclicGraphCheck"/>.
/// </remarks>
public sealed class CyclicConnectionCheck
{
    // The graph traversal stack, as required by a depth first graph traversal algorithm.
    // Each stack entry is an index into a connection list, representing both the current node being traversed
    // (the connections's source ID), and the current position in that node's outgoing connections.
    // for one source node.
    readonly IntStack _traversalStack = new(17);

    // Maintain a set of nodes that have been visited, this allows us to avoid unnecessary
    // re-traversal of nodes.
    readonly HashSet<int> _visitedNodes = new(17);

#if DEBUG
    // Indicates if a call to IsConnectionCyclic() is currently in progress.
    // For checking for attempts to re-enter that method while a call is in progress.
    int _reentranceFlag;
#endif

    #region Public Methods

    /// <summary>
    /// Tests if the proposed new connection <paramref name="newConn"/> would form a cycle if added to the
    /// existing directed acyclic graph described by connArr.
    /// </summary>
    /// <param name="connSpan">A set of connections that describe a directed acyclic graph.</param>
    /// <param name="newConn">A proposed new connection to add to the graph.</param>
    /// <returns>True if <paramref name="newConn"/> would form a cycle; otherwise false.</returns>
    public bool IsConnectionCyclic(Span<DirectedConnection> connSpan, in DirectedConnection newConn)
    {
#if DEBUG
        // Check for attempts to re-enter this method.
        if(Interlocked.CompareExchange(ref _reentranceFlag, 1, 0) == 1)
            throw new InvalidOperationException("Attempt to re-enter non-reentrant method.");
#endif

        try
        {
            return IsConnectionCyclicInner(connSpan, in newConn);
        }
        finally
        {   // Ensure cleanup occurs before we return so that we can guarantee the class instance is ready for
            // re-use on the next call.
            Cleanup();
        }
    }

    #endregion

    #region Private Methods

    private bool IsConnectionCyclicInner(Span<DirectedConnection> connSpan, in DirectedConnection newConn)
    {
        // Test if the new connection is pointing to itself.
        if(newConn.SourceId == newConn.TargetId)
            return true;

        // Note. We traverse forwards starting at the new connection's target node. If the new connection's source node is
        // encountered during traversal then the connection would form a cycle in the graph as a whole, and we return true.
        int startNodeId = newConn.TargetId;

        // Search for outgoing connections from the starting node.
        int connIdx = DirectedConnectionUtils.GetConnectionIndexBySourceNodeId(connSpan, startNodeId);
        if(connIdx < 0)
        {   // The current node has no outgoing connections, therefore newConn does not form a cycle.
            return false;
        }

        // Initialise and run the graph traversal algorithm.
        InitGraphTraversal(startNodeId, connIdx);

        // Note. we pass newConn.SourceId as the terminalNodeId; if traversal reaches this node then a cycle has been detected.
        return TraverseGraph(connSpan, newConn.SourceId);
    }

    private void Cleanup()
    {
        _traversalStack.Clear();
        _visitedNodes.Clear();

#if DEBUG
        // Reset reentrancy test flag.
        Interlocked.Exchange(ref _reentranceFlag, 0);
#endif
    }

    #endregion

    #region Private Methods [Graph Traversal]

    private void InitGraphTraversal(int startNodeId, int connIdx)
    {
        // Push connIdx onto the stack.
        _traversalStack.Push(connIdx);

        // Add the current node to the set of visited nodes; this prevents the traversal algorithm from re-entering this node
        // (it's on the stack thus it is in the process of being traversed).
        _visitedNodes.Add(startNodeId);
    }

    /// <summary>
    /// The graph traversal algorithm.
    /// </summary>
    /// <param name="connSpan">A set of connections that represents the graph to traverse.</param>
    /// <param name="terminalNodeId">// The 'terminal' node ID, i.e. if traversal reaches this node then newConn would form a cycle and we stop/terminate traversal.</param>
    private bool TraverseGraph(Span<DirectedConnection> connSpan, int terminalNodeId)
    {
        // While there are entries on the stack.
        while(_traversalStack.Count != 0)
        {
            // Get the connection index from the top of stack; this is the next connection to be traversed.
            int currConnIdx = _traversalStack.Peek();

            // Notes.
            // Before we traverse the current connection, update the stack state to point to the next connection to be
            // traversed, either from the current node or a parent node. I.e. we modify the stack state ready for when
            // the traversal down into the current connection completes and returns back to the current node.
            //
            // This approach results in tail call optimisation and thus will result in a shallower stack on average. It
            // also has the side effect that we can no longer examine the stack to observe the traversal path at a given
            // point in time, since some of the path may no longer be on the stack.
            MoveForward(connSpan, currConnIdx);

            // Test if the next traversal child node has already been visited.
            int childNodeId = connSpan[currConnIdx].TargetId;
            if(_visitedNodes.Contains(childNodeId))
                continue;

            // Test if the connection target is the terminal node.
            if(childNodeId == terminalNodeId)
                return true;

            // We're about to traverse into childNodeId, so mark it as visited to prevent re-traversal.
            _visitedNodes.Add(childNodeId);

            // Search for outgoing connections from childNodeId.
            int connIdx = DirectedConnectionUtils.GetConnectionIndexBySourceNodeId(connSpan, childNodeId);
            if(connIdx >= 0)
            {   // childNodeId has outgoing connections; push the first connection onto the stack to mark it for traversal.
                _traversalStack.Push(connIdx);
            }
        }

        // Traversal has completed without visiting the terminal node, therefore the new connection
        // does not form a cycle in the graph.
        return false;
    }

    /// <summary>
    /// Update the stack state to point to the next connection to traverse down.
    /// </summary>
    private void MoveForward(Span<DirectedConnection> connSpan, int currConnIdx)
    {
        // If the current node has at least one more outgoing connection leading to an unvisited node,
        // then update the node's entry on the top of the stack to point to said connection.
        for(int i = currConnIdx + 1; i < connSpan.Length && (connSpan[currConnIdx].SourceId == connSpan[i].SourceId); i++)
        {
            if(!_visitedNodes.Contains(connSpan[i].TargetId))
            {
                _traversalStack.Poke(i);
                return;
            }
        }

        // No more connections for the current node; pop/remove the current node from the top of the stack.
        // Traversal will thus continue from its traversal parent node's current position, or will terminate
        // if the stack is now empty.
        _traversalStack.Pop();
    }

    #endregion
}
