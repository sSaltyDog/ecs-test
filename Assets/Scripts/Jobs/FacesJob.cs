﻿using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;
using MyComponents;

struct FacesJob : IJob
{
    public EntityCommandBuffer commandBuffer;

	[ReadOnly] public Entity entity;
	[ReadOnly] public MapSquare mapSquare;

	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> current;
	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> rightAdjacent;
	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> leftAdjacent;
	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> frontAdjacent;
	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> backAdjacent;

	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> adjacentLowestBlocks;

	[ReadOnly] public int squareWidth;
	[DeallocateOnJobCompletion][ReadOnly] public NativeArray<float3> directions;	
	[ReadOnly] public JobUtil util;

	public void Execute()
	{
		FaceCounts counts;
		NativeArray<Faces> faces = CheckBlockFaces(entity, adjacentLowestBlocks, out counts);

		commandBuffer.AddComponent<FaceCounts>(entity, counts);
		DynamicBuffer<Faces> facesBuffer = commandBuffer.AddBuffer<Faces>(entity);
		facesBuffer.CopyFrom(faces);

		commandBuffer.RemoveComponent(entity, typeof(Tags.DrawMesh));
		faces.Dispose();
	}

	NativeArray<Faces> CheckBlockFaces(Entity entity, NativeArray<int> adjacentLowestBlocks, out FaceCounts counts)
	{
		BlockFaceChecker faceChecker = new BlockFaceChecker(){
			exposedFaces 	= new NativeArray<Faces>(current.Length, Allocator.Temp),
			mapSquare 		= mapSquare,

			current 	= current,
			rightAdjacent 	= rightAdjacent,
			leftAdjacent 	= leftAdjacent,
			frontAdjacent 	= frontAdjacent,
			backAdjacent 	= backAdjacent,

			adjacentLowestBlocks = adjacentLowestBlocks,
			
			squareWidth = TerrainSettings.mapSquareWidth,
			directions 	= directions, 
			util 		= new JobUtil()
		};
		
		for(int i = 0; i < mapSquare.blockDrawArrayLength; i++)
		{
			faceChecker.Execute(i);
		}


		counts = CountExposedFaces(current, faceChecker.exposedFaces);
		return faceChecker.exposedFaces;
	}

	FaceCounts CountExposedFaces(NativeArray<Block> blocks, NativeArray<Faces> exposedFaces)
	{
		//	Count vertices and triangles	
		int faceCount 	= 0;
		int vertCount 	= 0;
		int triCount 	= 0;
		for(int i = 0; i < exposedFaces.Length; i++)
		{
			int count = exposedFaces[i].count;
			if(count > 0)
			{
				Faces blockFaces = exposedFaces[i];

				//	Starting indices in mesh arrays
				blockFaces.faceIndex 	= faceCount;
				blockFaces.vertIndex 	= vertCount;
				blockFaces.triIndex 	= triCount;

				exposedFaces[i] = blockFaces;

				for(int f = 0; f < 6; f++)
				{
					switch((Faces.Exp)blockFaces[f])
					{
						case Faces.Exp.HIDDEN: break;

						case Faces.Exp.FULL:
							vertCount 	+= 4;
							triCount  	+= 6;
							break;

						case Faces.Exp.HALFOUT:
						case Faces.Exp.HALFIN:
							vertCount 	+= 3;
							triCount 	+= 3;
							break;
					}
				} 
				//	Slopes always need two extra verts
				if(blocks[i].isSloped > 0) vertCount += 2;

				faceCount += count;
			}
		}

		return new FaceCounts(faceCount, vertCount, triCount);
	}
}