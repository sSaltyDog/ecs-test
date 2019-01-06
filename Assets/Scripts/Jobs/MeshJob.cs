﻿using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;
using MyComponents;

//[BurstCompile]
//DisposeSentinal errors
struct MeshJob : IJobParallelFor
{
	[NativeDisableParallelForRestriction] public NativeArray<float3> vertices;
	[NativeDisableParallelForRestriction] public NativeArray<float3> normals;
	[NativeDisableParallelForRestriction] public NativeArray<int> triangles;
	[NativeDisableParallelForRestriction] public NativeArray<float4> colors;

	[ReadOnly] public MapSquare mapSquare;
	
	[ReadOnly] public DynamicBuffer<Block> blocks;
	[ReadOnly] public NativeArray<Faces> faces;
	[ReadOnly] public NativeArray<Topology> heightMap;

	[ReadOnly] public JobUtil util;
	[ReadOnly] public int cubeSize;
	[ReadOnly] public int cubeSlice;

	[ReadOnly] public CubeVertices baseVerts;

	public void Execute(int i)
	{
		i += mapSquare.drawIndexOffset;
		Block block = blocks[i];

		//	Skip blocks that aren't exposed
		if(faces[i].count == 0) return;

		//	Get block position for vertex offset
		float3 positionInMesh = util.Unflatten(i, cubeSize);

		//	Current local indices
		int vertIndex = 0;
		int triIndex = 0;

		//	Block starting indices
		int vertOffset = faces[i].vertIndex;
		int triOffset = faces[i].triIndex;

		//	Vertices and Triangles for exposed sides
		if(faces[i].right == 1)
			DrawFace(0, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
		if(faces[i].left == 1)
			DrawFace(1, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
		if(faces[i].front == 1)
			DrawFace(2, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
		if(faces[i].back == 1)
			DrawFace(3, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
		if(faces[i].up == 1)
			if(block.slopeType != SlopeType.NOTSLOPED)	//	Sloped block
				DrawSlope(triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
			else
				DrawFace(4, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);
		if(faces[i].down == 1)
			DrawFace(5, triIndex+triOffset, vertIndex+vertOffset, block, positionInMesh, ref triIndex, ref vertIndex);

		//	Vertex colours
		for(int v = 0; v < vertIndex; v++)
			colors[v+vertOffset] = BlockTypes.color[blocks[i].type];
	}

	void DrawFace(int face, int triOffset, int vertOffset, Block block, float3 position, ref int triIndex, ref int vertIndex)
	{
		Triangles(triOffset, vertOffset);
		triIndex += 6;
		Vertices(face, position, block, vertOffset);
		vertIndex +=  4;
	}

	void DrawSlope(int triOffset, int vertOffset, Block block, float3 position, ref int triIndex, ref int vertIndex)
	{
		SlopedTriangles(triOffset, vertOffset, block);
		triIndex += 6;
		SlopedVertices(vertOffset, position, block);
		vertIndex += 6;
	}

	//	Vertices for given side
	void Vertices(int side, float3 position, Block block, int index)
	{	
		switch(side)
		{
			case 0:	//	Right
				vertices[index+0] = baseVerts[5]+position;
				vertices[index+1] = baseVerts[6]+position;
				vertices[index+2] = baseVerts[2]+position;
				vertices[index+3] = baseVerts[1]+position;
				break;
			case 1:	//	Left
				vertices[index+0] = baseVerts[7]+position;
				vertices[index+1] = baseVerts[4]+position;
				vertices[index+2] = baseVerts[0]+position;
				vertices[index+3] = baseVerts[3]+position;
				break;
			case 2:	//	Front
				vertices[index+0] = baseVerts[4]+position;
				vertices[index+1] = baseVerts[5]+position;
				vertices[index+2] = baseVerts[1]+position;
				vertices[index+3] = baseVerts[0]+position;
				break;
			case 3:	//	Back
				vertices[index+0] = baseVerts[6]+position;
				vertices[index+1] = baseVerts[7]+position;
				vertices[index+2] = baseVerts[3]+position;
				vertices[index+3] = baseVerts[2]+position;
				break;
			case 4:	//	Top
				vertices[index+0] = baseVerts[7]+position;
				vertices[index+1] = baseVerts[6]+position;
				vertices[index+2] = baseVerts[5]+position;
				vertices[index+3] = baseVerts[4]+position;
				break;
			case 5:	//	Bottom
				vertices[index+0] = baseVerts[0]+position;
				vertices[index+1] = baseVerts[1]+position;
				vertices[index+2] = baseVerts[2]+position;
				vertices[index+3] = baseVerts[3]+position;
				break;
			
			default: throw new System.ArgumentOutOfRangeException("Index out of range 5: " + side);
		}
	}

	void Triangles(int index, int vertIndex)
	{
		triangles[index+0] = 3 + vertIndex; 
		triangles[index+1] = 1 + vertIndex; 
		triangles[index+2] = 0 + vertIndex; 
		triangles[index+3] = 3 + vertIndex; 
		triangles[index+4] = 2 + vertIndex; 
		triangles[index+5] = 1 + vertIndex;
	}

	//	Triangles are always the same set offset to vertex index
	//	and align so rect division always bisects slope direction
	void SlopedTriangles(int index, int vertIndex, Block block)
	{
		//	Slope is facing NW or SE
		if(block.slopeFacing == SlopeFacing.NWSE)
			TrianglesNWSE(index, vertIndex);
		//	Slope is facing NE or SW
		else
			TrianglesSWNE(index, vertIndex);
	}
	void SlopedVertices(int index, float3 position, Block block)
	{
		//	Slope is facing NW or SE
		if(block.slopeFacing == SlopeFacing.NWSE)
			SlopeVertsNWSE(index, position, block);
		//	Slope is facing NE or SW
		else
			SlopeVertsSWNE(index, position, block);
	}
	
	void SlopeVertsSWNE(int index, float3 position, Block block)
	{
		vertices[index+0] = baseVerts[7]+new float3(0, block.backLeftSlope, 0)+position;	//	back Left
		vertices[index+1] = baseVerts[6]+new float3(0, block.backRightSlope, 0)+position;	//	back Right
		vertices[index+2] = vertices[index+1];
		vertices[index+3] = baseVerts[5]+new float3(0, block.frontRightSlope, 0)+position;	//	front Right
		vertices[index+4] = baseVerts[4]+new float3(0, block.frontLeftSlope, 0)+position;	//	front Left
		vertices[index+5] = vertices[index+4];
	}

	void SlopeVertsNWSE(int index, float3 position, Block block)
	{
		vertices[index+0] = baseVerts[7]+new float3(0, block.backLeftSlope, 0)+position;	//	back Left
		vertices[index+1] = vertices[index+0];
		vertices[index+2] = baseVerts[6]+new float3(0, block.backRightSlope, 0)+position;	//	back Right
		vertices[index+3] = baseVerts[5]+new float3(0, block.frontRightSlope, 0)+position;	//	front Right
		vertices[index+4] = vertices[index+3];
		vertices[index+5] = baseVerts[4]+new float3(0, block.frontLeftSlope, 0)+position;	//	front Left
	}

	void TrianglesSWNE(int index, int vertIndex)
	{
		triangles[index+0] = 4 + vertIndex; 
		triangles[index+1] = 1 + vertIndex; 
		triangles[index+2] = 0 + vertIndex; 
		triangles[index+3] = 5 + vertIndex; 
		triangles[index+4] = 3 + vertIndex; 
		triangles[index+5] = 2 + vertIndex;
	}
	void TrianglesNWSE(int index, int vertIndex)
	{
		triangles[index+0] = 3 + vertIndex; 
		triangles[index+1] = 0 + vertIndex; 
		triangles[index+2] = 5 + vertIndex; 
		triangles[index+3] = 1 + vertIndex; 
		triangles[index+4] = 4 + vertIndex; 
		triangles[index+5] = 2 + vertIndex;
	}
}

public struct CubeVertices
{
	public float3 v0; 
	public float3 v1; 
	public float3 v2; 
	public float3 v3; 
	public float3 v4; 
	public float3 v5; 
	public float3 v6; 
	public float3 v7; 

	public CubeVertices(bool param)
	{
		v0 = new float3( 	-0.5f, -0.5f,	 0.5f );	//	left bottom front;
		v1 = new float3( 	 0.5f, -0.5f,	 0.5f );	//	right bottom front;
		v2 = new float3( 	 0.5f, -0.5f,	-0.5f );	//	right bottom back;
		v3 = new float3( 	-0.5f, -0.5f,	-0.5f ); 	//	left bottom back;
		v4 = new float3( 	-0.5f,  0.5f,	 0.5f );	//	left top front;
		v5 = new float3( 	 0.5f,  0.5f,	 0.5f );	//	right top front;
		v6 = new float3( 	 0.5f,  0.5f,	-0.5f );	//	right top back;
		v7 = new float3( 	-0.5f,  0.5f,	-0.5f );	//	left top back;
	}


	public float3 this[int vert]
	{
		get
		{
			switch (vert)
			{
				case 0: return v0;
				case 1: return v1;
				case 2: return v2;
				case 3: return v3;
				case 4: return v4;
				case 5: return v5;
				case 6: return v6;
				case 7: return v7;
				default: throw new System.ArgumentOutOfRangeException("Index out of range 7: " + vert);
			}
		}
	}
}