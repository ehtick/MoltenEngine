﻿namespace Molten;

/// <summary>
/// Represents an implementation of a matrix.
/// </summary>
/// <typeparam name="T">The type of the the underlying matrix components.</typeparam>
public interface IMatrix<T> 
    where T : unmanaged
{
    /// <summary> Gets a value indicating whether the current matrix is an identity matrix. </summary>
    /// <value>
    /// <c>true</c> if this instance is an identity matrix; otherwise, <c>false</c>.
    /// </value>
    bool IsIdentity { get; }

    /// <summary>
    /// Creates an array containing the elements of the <see cref="IMatrix{T}"/>.
    /// </summary>
    /// <returns>An array containing the components of the current <see cref="IMatrix{T}"/>.</returns>
    T[] ToArray();

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the matrix component, depending on the index.</value>
    /// <param name="index">The zero-based index of the component to access.</param>
    /// <returns>The value of the component at the specified index.</returns>
    T this[int index] { get; set; }

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the matrix component, depending on the index.</value>
    /// <param name="index">The zero-based index of the component to access.</param>
    /// <returns>The value of the component at the specified index.</returns>
    T this[uint index] { get; set; }

    /// <summary>
    /// Gets or sets the component at the specified row and column index.
    /// </summary>
    /// <value>The value of the matrix component, depending on the index.</value>
    /// <param name="row">The row of the matrix to access.</param>
    /// <param name="column">The column of the matrix to access.</param>
    /// <returns>The value of the component at the specified index.</returns>
    T this[int row, int column] { get; set; }

    /// <summary>
    /// Gets or sets the component at the specified row and column index.
    /// </summary>
    /// <value>The value of the matrix component, depending on the index.</value>
    /// <param name="row">The row of the matrix to access.</param>
    /// <param name="column">The column of the matrix to access.</param>
    /// <returns>The value of the component at the specified index.</returns>

    T this[uint row, uint column] { get; set; }

    /// <summary>
    /// Gets the number of components in the current matrix type.
    /// </summary>
    static readonly int ComponentCount;

    /// <summary>
    /// The number of rows in the current matrix type.
    /// </summary>
    static readonly int RowCount;

    /// <summary>
    /// The number of columns in the current matrix type.
    /// </summary>
    static readonly int ColumnCount;
}
