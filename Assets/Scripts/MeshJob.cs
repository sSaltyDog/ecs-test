﻿using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;

//[BurstCompile]
//DisposeSentinal errors
struct MeshJob : IJobParallelFor
{
	[NativeDisableParallelForRestriction] public NativeArray<float3> vertices;
	[NativeDisableParallelForRestriction] public NativeArray<int> triangles;
	[NativeDisableParallelForRestriction] public NativeArray<float4> colors;
	
	[ReadOnly] public DynamicBuffer<Block> blocks;
	[ReadOnly] public NativeArray<Faces> faces;

	//[ReadOnly] public MeshGenerator meshGenerator;
	[ReadOnly] public JobUtil util;
	[ReadOnly] public int chunkSize;

	[ReadOnly] public CubeVertices baseVerts;

	//	Vertices for given side
	void GetVerts(int side, float3 position, int index)
	{
		switch(side)
		{
			case 0:
				vertices[index+0] = baseVerts[5]+position;
				vertices[index+1] = baseVerts[6]+position;
				vertices[index+2] = baseVerts[2]+position;
				vertices[index+3] = baseVerts[1]+position;
				break;
			case 1:
				vertices[index+0] = baseVerts[7]+position;
				vertices[index+1] = baseVerts[4]+position;
				vertices[index+2] = baseVerts[0]+position;
				vertices[index+3] = baseVerts[3]+position;
				break;
			case 2:
				vertices[index+0] = baseVerts[7]+position;
				vertices[index+1] = baseVerts[6]+position;
				vertices[index+2] = baseVerts[5]+position;
				vertices[index+3] = baseVerts[4]+position;
				break;
			case 3:
				vertices[index+0] = baseVerts[0]+position;
				vertices[index+1] = baseVerts[1]+position;
				vertices[index+2] = baseVerts[2]+position;
				vertices[index+3] = baseVerts[3]+position;
				break;
			case 4:
				vertices[index+0] = baseVerts[4]+position;
				vertices[index+1] = baseVerts[5]+position;
				vertices[index+2] = baseVerts[1]+position;
				vertices[index+3] = baseVerts[0]+position;
				break;
			case 5:
				vertices[index+0] = baseVerts[6]+position;
				vertices[index+1] = baseVerts[7]+position;
				vertices[index+2] = baseVerts[3]+position;
				vertices[index+3] = baseVerts[2]+position;
				break;
			default: throw new System.ArgumentOutOfRangeException("Index out of range 5: " + side);
		}
	}

	//	Triangles are always the same set, offset to vertex index
	void GetTris(int index, int vertIndex)
	{
		triangles[index+0] = 3 + vertIndex; 
		triangles[index+1] = 1 + vertIndex; 
		triangles[index+2] = 0 + vertIndex; 
		triangles[index+3] = 3 + vertIndex; 
		triangles[index+4] = 2 + vertIndex; 
		triangles[index+5] = 1 + vertIndex;
	}

	public void Execute(int i)
	{
		//	Skip blocks that aren't exposed
		if(faces[i].count == 0) return;

		//	Get block position for vertex offset
		float3 pos = util.Unflatten(i, chunkSize);

		//	Current local indices
		int vertIndex = 0;
		int triIndex = 0;

		//	Block starting indices
		int vertOffset = faces[i].faceIndex * 4;
		int triOffset = faces[i].faceIndex * 6;

		//	Vertices and Triangles for exposed sides
		if(faces[i].right == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(0, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}
		if(faces[i].left == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(1, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}
		if(faces[i].up == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(2, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}
		if(faces[i].down == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(3, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}
		if(faces[i].forward == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(4, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}
		if(faces[i].back == 1)
		{
			GetTris(triIndex+triOffset, vertIndex+vertOffset);
			triIndex += 6;
			GetVerts(5, pos, vertIndex+vertOffset);
			vertIndex +=  4;
		}

		for(int v = 0; v < vertIndex; v++)
		{
			colors[v+vertOffset] = BlockTypes.color[blocks[i].blockType];
		}	 
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