﻿using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using MyComponents;

//	Get y buffer for mesh drawing based on adjacent top/bottom blocks
[UpdateAfter(typeof(MapUpdateGroups.InitialiseSquaresGroup))]
public class MapAdjacentSystem : ComponentSystem
{
    EntityManager entityManager;
	MapSquareSystem managerSystem;

	int squareWidth;

	ComponentGroup adjacentGroup;

    protected override void OnCreateManager()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();
		managerSystem = World.Active.GetOrCreateManager<MapSquareSystem>();

		squareWidth = TerrainSettings.mapSquareWidth;

		EntityArchetypeQuery adjacentQuery = new EntityArchetypeQuery{
            None 	= new ComponentType[] { typeof(AdjacentSquares), typeof(Tags.EdgeBuffer) },
			All 	= new ComponentType[] { typeof(MapSquare), typeof(Tags.InitialiseStageComplete) }
		};
		adjacentGroup = GetComponentGroup(adjacentQuery);
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer 		commandBuffer 	= new EntityCommandBuffer(Allocator.Temp);
        NativeArray<ArchetypeChunk> chunks 			= adjacentGroup.CreateArchetypeChunkArray(Allocator.TempJob);

        ArchetypeChunkEntityType 				entityType 		= GetArchetypeChunkEntityType();
        ArchetypeChunkComponentType<Translation> 	positionType 	= GetArchetypeChunkComponentType<Translation>(true);

		//	Map square position offsets in 8 cardinal directions
		float3[] adjacentPositions = new float3[8];
		float3[] directions = Util.CardinalDirections();
		for(int i = 0; i < directions.Length; i++)
			adjacentPositions[i] = directions[i] * squareWidth;

        for(int c = 0; c < chunks.Length; c++)
		{
			ArchetypeChunk chunk = chunks[c];

			NativeArray<Entity> 	entities    = chunk.GetNativeArray(entityType);
            NativeArray<Translation> 	positions 	= chunk.GetNativeArray(positionType);

			for(int e = 0; e < entities.Length; e++)
			{
				DebugTools.IncrementDebugCount("adjacent");
				
				Entity entity 	= entities[e];
				float3 position = positions[e].Value;

				Entity rightEntity 		= managerSystem.mapMatrix.GetItem(position + adjacentPositions[0]);
				Entity leftEntity 		= managerSystem.mapMatrix.GetItem(position + adjacentPositions[1]);
				Entity frontEntity 		= managerSystem.mapMatrix.GetItem(position + adjacentPositions[2]);
				Entity backEntity 		= managerSystem.mapMatrix.GetItem(position + adjacentPositions[3]);
				Entity frontRightEntity	= managerSystem.mapMatrix.GetItem(position + adjacentPositions[4]);
				Entity frontLeftEntity 	= managerSystem.mapMatrix.GetItem(position + adjacentPositions[5]);
				Entity backRightEntity 	= managerSystem.mapMatrix.GetItem(position + adjacentPositions[6]);
				Entity backLeftEntity 	= managerSystem.mapMatrix.GetItem(position + adjacentPositions[7]);

				AdjacentSquares adjacent = new AdjacentSquares{
					right 		= rightEntity,
					left 		= leftEntity,
					front 		= frontEntity,
					back 		= backEntity,
					frontRight 	= frontRightEntity,
					frontLeft 	= frontLeftEntity,
					backRight 	= backRightEntity,
					backLeft 	= backLeftEntity
				};

				for(int i = 0; i < 8; i++)
				{
					if(!entityManager.Exists(adjacent[i]))
					{
						DebugTools.Cube(Color.red, (position + adjacentPositions[i])+(squareWidth/2), squareWidth/2);
		        		DebugTools.Cube(Color.green, position + (squareWidth/2), squareWidth/2 +1);
					
						throw new System.Exception("Adjacent Entity does not exist at "+(position + adjacentPositions[i]));
					}
				}

				commandBuffer.AddComponent<AdjacentSquares>(entity, adjacent);
            }
        }
    
    	commandBuffer.Playback(entityManager);
		commandBuffer.Dispose();

    	chunks.Dispose();
    }
}