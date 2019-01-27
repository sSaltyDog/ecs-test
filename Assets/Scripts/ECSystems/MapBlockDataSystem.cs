﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MyComponents;

//	Generate 3D block data from 2D terrain data
[UpdateAfter(typeof(MapBlockBufferSystem))]
public class MapBlockDataSystem : ComponentSystem
{
	EntityManager entityManager;

	int cubeSize;	

	ComponentGroup generateBlocksGroup;

	protected override void OnCreateManager()
	{
		entityManager = World.Active.GetOrCreateManager<EntityManager>();
		
		cubeSize = TerrainSettings.cubeSize;

		EntityArchetypeQuery mapSquareQuery = new EntityArchetypeQuery
		{
			Any 	= Array.Empty<ComponentType>(),
			None  	= new ComponentType[] { typeof(Tags.EdgeBuffer), typeof(Tags.OuterBuffer) },
			All  	= new ComponentType[] { typeof(MapSquare), typeof(Tags.GenerateBlocks) }
		};
		generateBlocksGroup = GetComponentGroup(mapSquareQuery);
	}

	protected override void OnUpdate()
	{
		EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

		ArchetypeChunkEntityType 				entityType 		= GetArchetypeChunkEntityType();
		ArchetypeChunkComponentType<MapSquare> 	mapSquareType	= GetArchetypeChunkComponentType<MapSquare>();
		ArchetypeChunkBufferType<Block> 		blocksType 		= GetArchetypeChunkBufferType<Block>();
        ArchetypeChunkBufferType<Topology> 		heightmapType 	= GetArchetypeChunkBufferType<Topology>();

		NativeArray<ArchetypeChunk> chunks = generateBlocksGroup.CreateArchetypeChunkArray(Allocator.TempJob);

		for(int c = 0; c < chunks.Length; c++)
		{
			ArchetypeChunk chunk = chunks[c];

			NativeArray<Entity> entities 				= chunk.GetNativeArray(entityType);
			NativeArray<MapSquare> mapSquares			= chunk.GetNativeArray(mapSquareType);
			BufferAccessor<Block> blockAccessor 		= chunk.GetBufferAccessor(blocksType);
            BufferAccessor<Topology> heightmapAccessor 	= chunk.GetBufferAccessor(heightmapType);
			
			for(int e = 0; e < entities.Length; e++)
			{
				Entity entity 						= entities[e];
				DynamicBuffer<Block> blockBuffer 	= blockAccessor[e];
                DynamicBuffer<Topology> heightmap	= heightmapAccessor[e];
				MapSquare mapSquare 				= mapSquares[e];

				//	Resize buffer to size of (blocks in a cube) * (number of cubes)
				blockBuffer.ResizeUninitialized(mapSquare.blockGenerationArrayLength);

				//	Generate block data from height map
				NativeArray<Block> blocks = GetBlocks(
					mapSquares[e],
					heightmap
					);

				//	Fill buffer
				for(int b = 0; b < blocks.Length; b++)
					blockBuffer [b] = blocks[b];

				//	Set slopes next
				commandBuffer.RemoveComponent<Tags.GenerateBlocks>(entity);

				blocks.Dispose();
			}
		}
		
		commandBuffer.Playback(entityManager);
		commandBuffer.Dispose();

		chunks.Dispose();
	}

	NativeArray<Block> GetBlocks(MapSquare mapSquare, DynamicBuffer<Topology> heightMap)
	{
		//	Block data for all cubes in the map square
		var blocks = new NativeArray<Block>(mapSquare.blockGenerationArrayLength, Allocator.TempJob);

		BlocksJob job = new BlocksJob{
			blocks = blocks,
			mapSquare = mapSquare,
			heightMap = heightMap,
			cubeSize = cubeSize,
			util = new JobUtil()
		};
		
		job.Schedule(mapSquare.blockGenerationArrayLength, 1).Complete(); 

		return blocks;
	}
}
