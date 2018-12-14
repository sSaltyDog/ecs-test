﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MyComponents;

[UpdateAfter(typeof(MapSquareSystem))]
public class CubeSystem : ComponentSystem
{
	EntityManager entityManager;
	int cubeSize;


	ArchetypeChunkEntityType entityType;

	ArchetypeChunkBufferType<Block> 	blocksType;
	ArchetypeChunkBufferType<MapCube> 	cubePosType;
	ArchetypeChunkBufferType<Height> 	heightmapType;

	EntityArchetypeQuery mapSquareQuery;

	protected override void OnCreateManager()
	{
		entityManager = World.Active.GetOrCreateManager<EntityManager>();
		cubeSize = TerrainSettings.cubeSize;

		//	Chunks that need blocks generating
		mapSquareQuery = new EntityArchetypeQuery
		{
			Any 	= Array.Empty<ComponentType>(),
			None 	= Array.Empty<ComponentType>(),
			All  	= new ComponentType[] { typeof(MapSquare), typeof(Tags.GenerateBlocks) }
		};
	}

	protected override void OnUpdate()
	{
		entityType 		= GetArchetypeChunkEntityType();

		blocksType 		= GetArchetypeChunkBufferType<Block>();
		cubePosType 	= GetArchetypeChunkBufferType<MapCube>();
		heightmapType 	= GetArchetypeChunkBufferType<Height>();

		NativeArray<ArchetypeChunk> chunks;
		chunks	= entityManager.CreateArchetypeChunkArray(
			mapSquareQuery,
			Allocator.TempJob
			);

		if(chunks.Length == 0) chunks.Dispose();
		else GenerateCubes(chunks);
			
	}

	void GenerateCubes(NativeArray<ArchetypeChunk> chunks)
	{
		EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

		for(int c = 0; c < chunks.Length; c++)
		{
			ArchetypeChunk chunk = chunks[c];

			NativeArray<Entity> entities = chunk.GetNativeArray(entityType);

			BufferAccessor<Block> blockAccessor 		= chunk.GetBufferAccessor(blocksType);
			BufferAccessor<MapCube> cubePosAccessor 	= chunk.GetBufferAccessor(cubePosType);
			BufferAccessor<Height> heightmapAccessor 	= chunk.GetBufferAccessor(heightmapType);

			for(int e = 0; e < entities.Length; e++)
			{
				Entity entity = entities[e];

				DynamicBuffer<Block> blockBuffer 	= blockAccessor[e];

				DynamicBuffer<MapCube> cubes		= cubePosAccessor[e];
				DynamicBuffer<Height> heightmap		= heightmapAccessor[e];
				
				//	Resize buffer to size of (blocks in a cube) * (number of cubes)
				int blockArrayLength = (int)math.pow(cubeSize, 3) * cubes.Length;
				blockBuffer.ResizeUninitialized(blockArrayLength);

				//	Generate block data from height map
				NativeArray<Block> blocks 	= GetBlocks(
												1,
												blockArrayLength,
												heightmap.ToNativeArray(),
												cubes.ToNativeArray()
												);

				//	Fill buffer
				for(int b = 0; b < blocks.Length; b++)
					blockBuffer [b] = blocks[b];

				commandBuffer.RemoveComponent(entity, typeof(Tags.GenerateBlocks));

				blocks.Dispose();
			}

			commandBuffer.Playback(entityManager);
			commandBuffer.Dispose();

			chunks.Dispose();
		}
	}

	public NativeArray<Block> GetBlocks(int batchSize, int blockArrayLength, NativeArray<Height> heightMap, NativeArray<MapCube> cubes)
	{
		//	Block data for all cubes in the map square
		var blocks = new NativeArray<Block>(blockArrayLength, Allocator.TempJob);

		//	Size of a single cube's flattened block array
		int singleCubeArrayLength = (int)math.pow(cubeSize, 3);

		//	Iterate over one cube at a time, generating blocks for the map square
		for(int i = 0; i < cubes.Length; i++)
		{
			var job = new BlocksJob()
			{
				blocks = blocks,
				cubeStart = i * singleCubeArrayLength,
				cubePosY = cubes[i].yPos,

				heightMap = heightMap,
				cubeSize = cubeSize,
				util = new JobUtil()
			};
		
        	job.Schedule(singleCubeArrayLength, batchSize).Complete();	
		}

		return blocks;
	}





}
