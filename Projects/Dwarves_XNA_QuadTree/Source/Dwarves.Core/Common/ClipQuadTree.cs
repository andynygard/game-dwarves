﻿// ----------------------------------------------------------------------------
// <copyright file="ClipQuadTree.cs" company="Acidwashed Games">
//     Copyright 2012 Acidwashed Games. All right reserved.
// </copyright>
// ----------------------------------------------------------------------------
namespace Dwarves.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// A position-based quadtree which stores data in the leaf nodes. When data is set in the tree, it is placed in the
    /// smallest possible sized quadrant for the given bounds. The bounds with which the data was set is not retained,
    /// rather the data is 'clipped' to the bounds of the quadrant in which it belongs.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the quad tree.</typeparam>
    public class ClipQuadTree<T> : IEnumerable<ClipQuadTree<T>>
    {
        #region Private Variables

        /// <summary>
        /// The data at this leaf node.
        /// </summary>
        private T data;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ClipQuadTree class.
        /// </summary>
        /// <param name="bounds">The bounds of this quad tree node.</param>
        public ClipQuadTree(Square bounds)
        {
            if (!this.IsPowerOf2(bounds.Length))
            {
                throw new ArgumentException("Length of quad tree bounds must be a power of 2.");
            }

            this.Bounds = bounds;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the bounds of the node.
        /// </summary>
        public Square Bounds { get; private set; }

        /// <summary>
        /// Gets the top left quadrant.
        /// </summary>
        public ClipQuadTree<T> TopLeft { get; private set; }

        /// <summary>
        /// Gets the top right quadrant.
        /// </summary>
        public ClipQuadTree<T> TopRight { get; private set; }

        /// <summary>
        /// Gets the bottom left quadrant.
        /// </summary>
        public ClipQuadTree<T> BottomLeft { get; private set; }

        /// <summary>
        /// Gets the bottom right quadrant.
        /// </summary>
        public ClipQuadTree<T> BottomRight { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this node is a leaf (has no child nodes).
        /// </summary>
        public bool IsLeaf
        {
            get { return this.TopLeft == null; }
        }

        /// <summary>
        /// Gets the number of leaves contained by the node.
        /// </summary>
        public int LeafCount
        {
            get
            {
                if (this.IsLeaf)
                {
                    return 1;
                }
                else
                {
                    return
                        this.TopLeft.LeafCount +
                        this.TopRight.LeafCount +
                        this.BottomLeft.LeafCount +
                        this.BottomRight.LeafCount;
                }
            }
        }

        /// <summary>
        /// Gets or sets the data at this node. Data can only be set for leaf nodes.
        /// </summary>
        public T Data
        {
            get
            {
                return this.data;
            }

            set
            {
                if (this.IsLeaf)
                {
                    if (!EqualityComparer<T>.Default.Equals(this.data, value))
                    {
                        this.data = value;
                    }
                }
                else
                {
                    throw new ApplicationException("Cannot set data value for a non-leaf ClipQuadTree node.");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the <typeparamref name="T"/> data in the quad tree at the smallest node containing the given bounds.
        /// <para />
        /// The data will be set in the smallest possible sized quadrant for the given bounds. The bounds with which the
        /// data is set is not retained, rather the data is 'clipped' to the bounds of the destination quadrant.
        /// Existing data will be replaced and child nodes cleared at the destination quadrant.
        /// </summary>
        /// <param name="data">The data to set.</param>
        /// <param name="bounds">The bounds of the data to set.</param>
        /// <param name="dataSplitter">The object for splitting any existing leaf-node data; Null value will cause an
        /// exception to be thrown if a data split is required.</param>
        /// <returns>True if the data was set; False the given bounds lies outside the bounds of this node.</returns>
        public bool SetData(T data, Square bounds, QuadTreeDataSplitter<T> dataSplitter)
        {
            if (!this.Bounds.Contains(bounds))
            {
                // Data's bounds exceed this node's bounds
                return false;
            }

            // Attempt to set the data in a child node
            if (!this.SetChildData(data, bounds, dataSplitter))
            {
                // The item doesn't fit into a child node, so this is the best fit possible
                // Remove any child-nodes as data can only exist on leaf-nodes (which this node will now become)
                this.TopLeft = null;
                this.TopRight = null;
                this.BottomLeft = null;
                this.BottomRight = null;

                // Set the data
                this.Data = data;
            }

            return true;
        }

        /// <summary>
        /// Try to get the <typeparamref name="T"/> data at the given point.
        /// </summary>
        /// <param name="point">The point at which to fetch the data.</param>
        /// <param name="data">The data at the point; default(<typeparamref name="T"/>) if the point lies outside the
        /// bounds of this node.
        /// </param>
        /// <returns>True if the data was retrieved; False if the point lies outside the bounds of this node.</returns>
        public bool GetDataAt(Point point, out T data)
        {
            ClipQuadTree<T> node;
            if (this.GetNodeAt(point, out node))
            {
                data = node.data;
                return true;
            }
            else
            {
                // The point lies outside the bounds of this node
                data = default(T);
                return false;
            }
        }

        /// <summary>
        /// Gets the node of the quad tree which fully contains the given point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="node">The node at the point. Null if no node was not found.</param>
        /// <returns>True if the node was retrieved; False if the point lies outside the bounds of this node.</returns>
        public bool GetNodeAt(Point point, out ClipQuadTree<T> node)
        {
            if (this.Bounds.Contains(point))
            {
                if (this.IsLeaf)
                {
                    // Cannot go any smaller than a leaf node
                    node = this;
                    return true;
                }
                else
                {
                    // Try and see if the point lies in one of the sub-quadrants
                    return
                        this.TopLeft.GetNodeAt(point, out node) ||
                        this.TopRight.GetNodeAt(point, out node) ||
                        this.BottomLeft.GetNodeAt(point, out node) ||
                        this.BottomRight.GetNodeAt(point, out node);
                }
            }
            else
            {
                // The point doesn't fit in this node
                node = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the node of the quad tree which fully contains the given rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="node">The node at the point. Null if no node was not found.</param>
        /// <returns>True if the node was retrieved; False if the rectangle lies outside the bounds of this node.
        /// </returns>
        public bool GetNodeContaining(Rectangle rect, out ClipQuadTree<T> node)
        {
            if (this.Bounds.Contains(rect))
            {
                if (this.IsLeaf)
                {
                    // Cannot go any smaller than a leaf node
                    node = this;
                    return true;
                }
                else
                {
                    // Try and see if the rectangle fits in one of the sub-quadrants
                    bool existsInChild =
                        this.TopLeft.GetNodeContaining(rect, out node) ||
                        this.TopRight.GetNodeContaining(rect, out node) ||
                        this.BottomLeft.GetNodeContaining(rect, out node) ||
                        this.BottomRight.GetNodeContaining(rect, out node);

                    // If the rectangle doesn't fit in a sub-quadrant, this node is the smallest fit
                    if (!existsInChild)
                    {
                        node = this;
                    }

                    return true;
                }
            }
            else
            {
                // The rect doesn't fit in this node
                node = null;
                return false;
            }
        }

        /// <summary>
        /// Get the enumerable set of the leaf nodes which intersect the given rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <returns>The enumerable set.</returns>
        public IEnumerable<ClipQuadTree<T>> GetNodesIntersecting(Rectangle rect)
        {
            if (this.Bounds.Intersects(rect))
            {
                // Get the smallest node containing the whole rectangle
                ClipQuadTree<T> containingNode;
                if (!this.GetNodeContaining(rect, out containingNode))
                {
                    containingNode = this;
                }

                // Iterate all leaf nodes
                foreach (ClipQuadTree<T> node in containingNode)
                {
                    // Check that this leaf node overlaps the rectangle
                    if (node.Bounds.Intersects(rect))
                    {
                        yield return node;
                    }
                }
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Get an enumerator that iterates through all the leaf nodes in the quad tree.
        /// </summary>
        /// <returns>An enumerator object.</returns>
        public IEnumerator<ClipQuadTree<T>> GetEnumerator()
        {
            var stack = new Stack<ClipQuadTree<T>>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                ClipQuadTree<T> node = stack.Pop();

                if (node.IsLeaf)
                {
                    yield return node;
                }
                else
                {
                    stack.Push(node.TopLeft);
                    stack.Push(node.TopRight);
                    stack.Push(node.BottomLeft);
                    stack.Push(node.BottomRight);
                }
            }
        }

        /// <summary>
        /// Get an enumerator that iterates through all data in the quad tree.
        /// </summary>
        /// <returns>An enumerator object.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Attempt to set the <typeparamref name="T"/> data in a child quadrant at the given bounds.
        /// <para />
        /// </summary>
        /// <param name="data">The data to set.</param>
        /// <param name="bounds">The bounds of the data to set.</param>
        /// <param name="dataSplitter">The object for splitting any existing leaf-node data; Null value will causean
        /// exception to be thrown if a data split is required.</param>
        /// <returns>True if the data was set in a child quadrant; False if the data was not set because the given
        /// bounds do not exist within the bounds of a child quadrant.
        /// </returns>
        protected bool SetChildData(T data, Square bounds, QuadTreeDataSplitter<T> dataSplitter)
        {
            if (!this.IsLeaf)
            {
                // This is not a leaf node so attempt to set the data in a child quadrant
                return
                    this.TopLeft.SetData(data, bounds, dataSplitter) ||
                    this.TopRight.SetData(data, bounds, dataSplitter) ||
                    this.BottomLeft.SetData(data, bounds, dataSplitter) ||
                    this.BottomRight.SetData(data, bounds, dataSplitter);
            }
            else
            {
                // This is a leaf node so test if this node could be split further to more tightly fit the given bounds
                if (this.Bounds.Length > 1)
                {
                    if (this.Bounds.GetTopLeftQuadrant().Contains(bounds))
                    {
                        this.Split(dataSplitter);
                        return this.TopLeft.SetData(data, bounds, dataSplitter);
                    }
                    else if (this.Bounds.GetTopRightQuadrant().Contains(bounds))
                    {
                        this.Split(dataSplitter);
                        return this.TopRight.SetData(data, bounds, dataSplitter);
                    }
                    else if (this.Bounds.GetBottomLeftQuadrant().Contains(bounds))
                    {
                        this.Split(dataSplitter);
                        return this.BottomLeft.SetData(data, bounds, dataSplitter);
                    }
                    else if (this.Bounds.GetBottomRightQuadrant().Contains(bounds))
                    {
                        this.Split(dataSplitter);
                        return this.BottomRight.SetData(data, bounds, dataSplitter);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Split the square into four even sub-quadrants.
        /// </summary>
        /// <param name="dataSplitter">The object for splitting any existing leaf-node data; Null value will causean
        /// exception to be thrown if a data split is required.</param>
        protected void Split(QuadTreeDataSplitter<T> dataSplitter)
        {
            if (this.Bounds.Length <= 1)
            {
                throw new ApplicationException("Cannot split quad tree node as it is the smallest size possible.");
            }

            // Split any existing data
            QuadTreeDataSplitter<T>.Result splitData = null;
            if (!EqualityComparer<T>.Default.Equals(this.Data, default(T)))
            {
                if (dataSplitter != null)
                {
                    // Perform split
                    splitData = dataSplitter.Split(this.Data, this.Bounds);

                    // Clear data at this node
                    this.data = default(T);
                }
                else
                {
                    string message = "Cannot split quad tree node as it contains data. Please provide a " +
                        "QuadTreeDataSplitter object or remove the existing data first.";
                    throw new ApplicationException(message);
                }
            }

            // Create the quadrants
            this.TopLeft = new ClipQuadTree<T>(this.Bounds.GetTopLeftQuadrant());
            this.TopRight = new ClipQuadTree<T>(this.Bounds.GetTopRightQuadrant());
            this.BottomLeft = new ClipQuadTree<T>(this.Bounds.GetBottomLeftQuadrant());
            this.BottomRight = new ClipQuadTree<T>(this.Bounds.GetBottomRightQuadrant());

            // Set the data values
            if (splitData != null)
            {
                this.TopLeft.Data = splitData.TopLeft;
                this.TopRight.Data = splitData.TopRight;
                this.BottomLeft.Data = splitData.BottomLeft;
                this.BottomRight.Data = splitData.BottomRight;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determine if the given number is a power of 2.
        /// </summary>
        /// <param name="x">The number to check.</param>
        /// <returns>True if the number is a power of 2.</returns>
        private bool IsPowerOf2(int x)
        {
            return (x > 0) && ((x & (x - 1)) == 0);
        }

        #endregion
    }
}