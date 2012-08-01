﻿// ----------------------------------------------------------------------------
// <copyright file="TerrainRenderComponent.cs" company="Acidwashed Games">
//     Copyright 2012 Acidwashed Games. All right reserved.
// </copyright>
// ----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Component for rendering the terrain.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(TerrainComponent))]
public class TerrainRenderComponent : MonoBehaviour
{
    /// <summary>
    /// The core terrain component.
    /// </summary>
	private TerrainComponent cTerrain;
	
    /// <summary>
    /// The mesh filter component.
    /// </summary>
	private MeshFilter cMeshFilter;
	
    /// <summary>
    /// Gets the mesh generator.
    /// </summary>
    public TerrainMeshGenerator MeshGenerator { get; private set; }

    /// <summary>
    /// Initialises the component.
    /// </summary>
    public void Start()
    {
        this.MeshGenerator = new TerrainMeshGeneratorCubes();
		
        // Get a reference to the related terrain components
        this.cTerrain = this.GetComponent<TerrainComponent>();
        this.cMeshFilter = this.GetComponent<MeshFilter>();
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    public void Update()
    {
		// Check if the terrain mesh needs to be rebuilt
		if (this.cTerrain.Terrain.Mesh.MeshChanged)
		{
			this.RebuildMesh();
		}
    }
	
    /// <summary>
    /// Rebuild the geometry on the mesh filter.
    /// </summary>
    private void RebuildMesh()
    {
		// Build the arrays for the vertices and triangle indices for each submesh
		Vector3[] vertices = new Vector3[this.cTerrain.Terrain.Mesh.GetVerticeCount()];
		var materialIndices = new Dictionary<MaterialType, int[]>();
		var materialArrayIndexes = new Dictionary<MaterialType, int>();
		foreach (MaterialType material in this.cTerrain.Terrain.Mesh.GetMaterials())
		{
			materialIndices.Add(material, new int[this.cTerrain.Terrain.Mesh.GetIndiceCount(material)]);
			materialArrayIndexes.Add(material, 0);
		}
		
		// Populate the vertice and indice arrays
		int verticeArrayIndex = 0;
		foreach (BlockMesh blockMesh in this.cTerrain.Terrain.Mesh)
		{	
			// Copy the vertices
			Array.Copy(blockMesh.Vertices, 0, vertices, verticeArrayIndex, blockMesh.Vertices.Length);
			verticeArrayIndex += blockMesh.Vertices.Length;
			
			// Copy the indices
			int[] indices = materialIndices[blockMesh.Material];
			int indiceArrayIndex = materialArrayIndexes[blockMesh.Material];
			Array.Copy(blockMesh.Indices, 0, indices, indiceArrayIndex, indices.Length);
			materialArrayIndexes[blockMesh.Material] = indiceArrayIndex + indices.Length;
		}
		
		// Update the mesh filter geometry
		// TODO
		
		// Reset the mesh changed flag
		this.cTerrain.Terrain.Mesh.ResetMeshChanged();
	}
}