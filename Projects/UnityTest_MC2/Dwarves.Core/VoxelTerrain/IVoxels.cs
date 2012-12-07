﻿// ----------------------------------------------------------------------------
// <copyright file="IVoxels.cs" company="Acidwashed Games">
//     Copyright 2012 Acidwashed Games. All right reserved.
// </copyright>
// ----------------------------------------------------------------------------
namespace Dwarves.Core.VoxelTerrain
{
    using System.Collections.Generic;
    using Dwarves.Core.Math;

    /// <summary>
    /// The terrain voxels.
    /// </summary>
    public interface IVoxels
    {
        /// <summary>
        /// Gets or sets the voxel at the given position.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="z">The z position.</param>
        /// <returns>The voxel.</returns>
        Voxel this[int x, int y, int z] { get; set; }
    }
}