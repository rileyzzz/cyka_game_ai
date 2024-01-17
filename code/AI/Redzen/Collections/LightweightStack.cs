﻿// This file is part of the Redzen code library; Copyright Colin D. Green.
// See LICENSE.txt for details.
using System.Diagnostics.CodeAnalysis;

namespace Redzen.Collections;

/// <summary>
/// A lightweight generic stack.
/// </summary>
/// <remarks>
/// An alternative to <see cref="Stack{T}"/> with a simpler implementation which may give improved performance in some
/// scenarios. This implementation also provides additional Poke() and TryPoke() methods.
/// </remarks>
/// <typeparam name="T">Stack item type.</typeparam>
public sealed class LightweightStack<T>
{
    const int __defaultCapacity = 4;
    T[] _array;
    int _size;

    #region Constructors

    /// <summary>
    /// Default constructor.
    /// </summary>
    public LightweightStack()
    {
        _array = new T[__defaultCapacity];
    }

    /// <summary>
    /// Construct with the given initial capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    public LightweightStack(int capacity)
    {
        if(capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), ExceptionMessages.NegativeCapacity);

        _array = new T[capacity];
    }

    #endregion

    #region Public Methods / Properties

    /// <summary>
    /// Gets the number of items on the stack.
    /// </summary>
    public int Count => _size;

    /// <summary>
    /// Pushes a value onto the top of the stack.
    /// </summary>
    /// <param name="val">The value to push.</param>
    public void Push(in T val)
    {
        if(_size == _array.Length)
            Array.Resize(ref _array, (_array.Length == 0) ? __defaultCapacity : 2 * _array.Length);

        _array[_size++] = val;
    }

    /// <summary>
    /// Pop a value from the top of the stack.
    /// </summary>
    /// <returns>The popped value from the top of the stack.</returns>
    public T Pop()
    {
        if(_size == 0)
            throw new InvalidOperationException(ExceptionMessages.AccessEmptyStackError);

        return _array[--_size];
    }

    /// <summary>
    /// Attempt to pop a value from the top of the stack.
    /// </summary>
    /// <param name="result">The value from the top of the stack.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool TryPop(
        [MaybeNullWhen(false)] out T result)
    {
        if(_size == 0)
        {
            result = default;
            return false;
        }

        result = _array[--_size];
        return true;
    }

    /// <summary>
    /// Returns the value at the top of the stack without popping it.
    /// </summary>
    /// <returns>The value at the top of the stack.</returns>
    public T Peek()
    {
        if(_size == 0)
            throw new InvalidOperationException(ExceptionMessages.AccessEmptyStackError);

        return _array[_size - 1];
    }

    /// <summary>
    /// Returns the value at the top of the stack without popping it, if the stack is not empty.
    /// </summary>
    /// <param name="result">The value at the top of the stack.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool TryPeek(
        [MaybeNullWhen(false)] out T result)
    {
        if(_size == 0)
        {
            result = default;
            return false;
        }
        result = _array[_size - 1];
        return true;
    }

    /// <summary>
    /// Sets/overwrites he value at the top of the stack.
    /// </summary>
    /// <param name="val">The value to set.</param>
    public void Poke(in T val)
    {
        if(_size == 0)
            throw new InvalidOperationException(ExceptionMessages.AccessEmptyStackError);

        _array[_size - 1] = val;
    }

    /// <summary>
    /// Sets/overwrites he value at the top of the stack, if the stack is not empty.
    /// </summary>
    /// <param name="val">The value to set.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool TryPoke(in T val)
    {
        if(_size == 0)
            return false;

        _array[_size - 1] = val;
        return true;
    }

    /// <summary>
    /// Removes all items from the stack; i.e. moves the stack pointer to the bottom of the stack.
    /// </summary>
    public void Clear()
    {
        // Note. For efficiency the elements of _array are not reset.
        _size = 0;
    }

    /// <summary>
    /// Gets a Span over the stack items in the internal items array.
    /// </summary>
    /// <returns>A new instance of <see cref="Span{T}"/>.</returns>
    /// <remarks>
    /// The Span length is equal to the number of items on the stack.
    /// The span item order is from bottom to top of stack, i.e., the bottom stack item is at index zero.
    /// If mutation operations (such as Push, Pop, or Clear) are called on the Stack during the Span's lifetime,
    /// then the Span my be pointing to data that is no longer being used by the Stack.
    /// </remarks>
    public Span<T> AsSpan()
    {
        return _array.AsSpan(0, _size);
    }

    #endregion
}
