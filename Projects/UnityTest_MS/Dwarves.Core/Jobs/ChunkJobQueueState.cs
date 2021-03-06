﻿// ----------------------------------------------------------------------------
// <copyright file="ChunkJobQueueState.cs" company="Dematic">
//     Copyright © Dematic 2009-2013. All rights reserved
// </copyright>
// ----------------------------------------------------------------------------
namespace Dwarves.Core.Jobs
{
    using System.Collections;
    using System.Collections.Generic;
    using Dwarves.Core.Math;
    using UnityEngine;

    /// <summary>
    /// The state of a chunk job queue.
    /// </summary>
    public class ChunkJobQueueState
    {
        /// <summary>
        /// Indicates whether the a rebuild mesh job needs to be queued due to changes to point data.
        /// </summary>
        private bool rebuildMeshRequired;

        /// <summary>
        /// Indicates whether the a mesh filter update job needs to be queued due to changes to mesh data.
        /// </summary>
        private bool updateMeshFilterRequired;

        /// <summary>
        /// Synchronises rebuilding of meshes for adjacent chunks such that they are updated in the same frame with no
        /// flickering along the boundaries. This only applies in the situation where a change in the geometry spans
        /// both meshes (such as a tunnel being dug across the boundary).
        /// </summary>
        private SynchronisedUpdate meshFilterSync;

        /// <summary>
        /// Indicates whether a job is queued to load the points.
        /// </summary>
        private bool loadPointsInProgress;

        /// <summary>
        /// Indicates whether a job is queued to rebuild the chunk mesh.
        /// </summary>
        private bool rebuildMeshInProgress;

        /// <summary>
        /// Indicates whether a job is queued to update the mesh filter.
        /// </summary>
        private bool updateMeshFilterInProgress;

        /// <summary>
        /// The dictionary tracking the current dig circle jobs.
        /// </summary>
        private Dictionary<Vector2I, int> digCircleInProgress;

        /// <summary>
        /// Initialises a new instance of the ChunkJobQueueState class.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public ChunkJobQueueState(Vector2I chunk)
        {
            this.Chunk = chunk;
            this.digCircleInProgress = new Dictionary<Vector2I, int>();
        }

        /// <summary>
        /// Gets the chunk.
        /// </summary>
        public Vector2I Chunk { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a load points job has completed.
        /// </summary>
        public bool LoadPointsCompleted { get; private set; }

        #region Load Points

        /// <summary>
        /// Check whether a LoadPoints job can execute.
        /// </summary>
        /// <param name="chunk">The chunk being loaded.</param>
        /// <returns>True if the job can be enqueued.</returns>
        public bool CanLoadPoints(Vector2I chunk)
        {
            return this.Chunk != chunk || !this.loadPointsInProgress;
        }

        /// <summary>
        /// Reserves a LoadPoints job.
        /// </summary>
        /// <param name="chunk">The chunk being loaded.</param>
        public void ReserveLoadPoints(Vector2I chunk)
        {
            if (this.Chunk == chunk)
            {
                this.loadPointsInProgress = true;
            }

            this.rebuildMeshRequired = true;
        }

        /// <summary>
        /// Un-reserves a LoadPoints job.
        /// </summary>
        /// <param name="chunk">The chunk being loaded.</param>
        public void UnreserveLoadPoints(Vector2I chunk)
        {
            if (this.Chunk == chunk)
            {
                this.loadPointsInProgress = false;
                this.LoadPointsCompleted = true;
            }
        }

        #endregion

        #region Rebuild Mesh

        /// <summary>
        /// Check whether a RebuildMesh job can execute.
        /// </summary>
        /// <param name="chunk">The chunk being rebuilt.</param>
        /// <returns>True if the job can be enqueued.</returns>
        public bool CanRebuildMesh(Vector2I chunk)
        {
            return this.Chunk != chunk || (!this.rebuildMeshInProgress && this.rebuildMeshRequired);
        }

        /// <summary>
        /// Reserves a RebuildMesh job.
        /// </summary>
        /// <param name="chunk">The chunk being rebuilt.</param>
        public void ReserveRebuildMesh(Vector2I chunk)
        {
            if (this.Chunk == chunk)
            {
                this.rebuildMeshInProgress = true;

                // Merge the rebuild mesh sync chunks into the update mesh filter chunks
                this.updateMeshFilterRequired = true;
                if (this.meshFilterSync != null)
                {
                    this.meshFilterSync.SetReady(this.Chunk);
                }

                // Reset the rebuild mesh required flags
                this.rebuildMeshRequired = false;
            }
        }

        /// <summary>
        /// Un-reserves a RebuildMesh job.
        /// </summary>
        /// <param name="chunk">The chunk being rebuilt.</param>
        public void UnreserveRebuildMesh(Vector2I chunk)
        {
            if (this.Chunk == chunk)
            {
                this.rebuildMeshInProgress = false;
            }
        }

        #endregion

        #region Update Mesh Filter

        /// <summary>
        /// Check whether a UpdateMeshFilter job can execute.
        /// </summary>
        /// <param name="chunksToSync">The chunks that need to have their mesh filter updated in the same frame as
        /// this. Null if no chunk sync is required.</param>
        /// <returns>True if the job can be enqueued.</returns>
        public bool CanUpdateMeshFilter(out Vector2I[] chunksToSync)
        {
            if (!this.updateMeshFilterInProgress && this.updateMeshFilterRequired)
            {
                if (this.meshFilterSync == null)
                {
                    chunksToSync = null;
                    return true;
                }
                else if (this.meshFilterSync.IsSynchronised)
                {
                    chunksToSync = this.meshFilterSync.GetChunks();
                    return true;
                }
            }

            chunksToSync = null;
            return false;
        }

        /// <summary>
        /// Reserves a UpdateMeshFilter job.
        /// </summary>
        public void ReserveUpdateMeshFilter()
        {
            this.updateMeshFilterInProgress = true;
            this.updateMeshFilterRequired = false;
            this.meshFilterSync = null;
        }

        /// <summary>
        /// Un-reserves a UpdateMeshFilter job.
        /// </summary>
        public void UnreserveUpdateMeshFilter()
        {
            this.updateMeshFilterInProgress = false;
        }

        #endregion

        #region Dig Circle

        /// <summary>
        /// Check whether a DigCircle job can execute.
        /// </summary>
        /// <param name="chunk">The chunk in which the origin lies.</param>
        /// <param name="origin">The circle origin.</param>
        /// <param name="radius">The circle radius.</param>
        /// <returns>True if the job can be enqueued.</returns>
        public bool CanDigCircle(Vector2I chunk, Vector2I origin, int radius)
        {
            if (chunk == this.Chunk)
            {
                bool exists;
                int existing;
                lock ((this.digCircleInProgress as ICollection).SyncRoot)
                {
                    exists = this.digCircleInProgress.TryGetValue(origin, out existing);
                }

                return !exists || radius > existing;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Reserves a DigCircle job.
        /// </summary>
        /// <param name="chunk">The chunk in which the origin lies.</param>
        /// <param name="origin">The circle origin.</param>
        /// <param name="radius">The circle radius.</param>
        /// <param name="toSync">The chunks requiring synchronisation such that their mesh filters are updated in
        /// the same frame.</param>
        public void ReserveDigCircle(Vector2I chunk, Vector2I origin, int radius, SynchronisedUpdate toSync)
        {
            if (chunk == this.Chunk)
            {
                lock ((this.digCircleInProgress as ICollection).SyncRoot)
                {
                    if (this.digCircleInProgress.ContainsKey(origin))
                    {
                        this.digCircleInProgress[origin] = radius;
                    }
                    else
                    {
                        this.digCircleInProgress.Add(origin, radius);
                    }
                }
            }

            this.rebuildMeshRequired = true;
            this.meshFilterSync = SynchronisedUpdate.MergeAndReset(this.meshFilterSync, toSync);
        }

        /// <summary>
        /// Un-reserves a DigCircle job.
        /// </summary>
        /// <param name="chunk">The chunk in which the origin lies.</param>
        /// <param name="origin">The circle origin.</param>
        /// <param name="radius">The circle radius.</param>
        public void UnreserveDigCircle(Vector2I chunk, Vector2I origin, int radius)
        {
            if (chunk == this.Chunk)
            {
                lock ((this.digCircleInProgress as ICollection).SyncRoot)
                {
                    if (this.digCircleInProgress[origin] == radius)
                    {
                        this.digCircleInProgress.Remove(origin);
                    }
                }
            }
        }

        #endregion
    }
}