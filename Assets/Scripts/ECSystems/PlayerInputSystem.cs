﻿using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using MyComponents;

public class PlayerInputSystem : ComponentSystem
{
    EntityManager entityManager;
    int cubeSize;

    EntityArchetypeQuery query;

    ArchetypeChunkEntityType entityType;
    ArchetypeChunkComponentType<Position> positionType;
    ArchetypeChunkComponentType<PlayerInput> inputType;

    protected override void OnCreateManager()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();
        cubeSize = TerrainSettings.cubeSize;

        //  Chunks that need blocks generating
        query = new EntityArchetypeQuery
        {
            Any     = Array.Empty<ComponentType>(),
            None    = Array.Empty<ComponentType>(),
            All     = new ComponentType[] { typeof(Tags.PlayerEntity) }
        };
    }

    protected override void OnUpdate()
    {
        entityType = GetArchetypeChunkEntityType();
        positionType = GetArchetypeChunkComponentType<Position>();
        inputType = GetArchetypeChunkComponentType<PlayerInput>();

        NativeArray<ArchetypeChunk> chunks;
        chunks = entityManager.CreateArchetypeChunkArray(
            query,
            Allocator.TempJob
        );

        if(chunks.Length == 0) chunks.Dispose();
        else DoSomething(chunks);
    }

    void DoSomething(NativeArray<ArchetypeChunk> chunks)
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        for(int c = 0; c < chunks.Length; c++)
        {
            ArchetypeChunk chunk = chunks[c];

            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Position> positions = chunk.GetNativeArray(positionType);
            NativeArray<PlayerInput> inputs = chunk.GetNativeArray(inputType);
            
            for(int e = 0; e < entities.Length; e++)
            {
                Entity entity = entities[e];

                float3 move = new float3(1, 0, 0);

                Move moveComponent = new Move { positionChangePerSecond = move };

                commandBuffer.SetComponent<Move>(entity, moveComponent);
            }
        }

        commandBuffer.Playback(entityManager);
        commandBuffer.Dispose();

        chunks.Dispose();
    }
}