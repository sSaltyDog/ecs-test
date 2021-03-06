﻿/*using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using MyComponents;
using Unity.Entities;

struct WorleyNoiseJob : IJob
{
    [ReadOnly] public float3 offset;
    [ReadOnly] public int squareWidth;
    [ReadOnly] public JobUtil util;
    [ReadOnly] public WorleyNoiseGenerator noise;

    [ReadOnly] public EntityCommandBuffer commandBuffer;
    [ReadOnly] public Entity mapSquareEntity;

    //  Fill flattened 2D array with noise matrix
    public void Execute()
    {
        NativeArray<WorleyNoise> worleyNoiseMap = new NativeArray<WorleyNoise>((int)math.pow(squareWidth, 2), Allocator.TempJob);

        for(int i = 0; i < worleyNoiseMap.Length; i++)
        {
            float3 position = util.Unflatten2D(i, squareWidth) + offset;
            worleyNoiseMap[i] = noise.GetEdgeData(position.x, position.z);
        }

        DynamicBuffer<WorleyNoise> worleyNoiseBuffer = commandBuffer.SetBuffer<WorleyNoise>(mapSquareEntity);
        worleyNoiseBuffer.CopyFrom(worleyNoiseMap);

        NativeArray<WorleyCell> worleyCellSet = UniqueWorleyCellSet(worleyNoiseMap);
        DynamicBuffer<WorleyCell> uniqueWorleyCells = commandBuffer.SetBuffer<WorleyCell>(mapSquareEntity);
        uniqueWorleyCells.CopyFrom(worleyCellSet);

        worleyNoiseMap.Dispose();
        worleyCellSet.Dispose();
    }

    public NativeArray<WorleyCell> UniqueWorleyCellSet(NativeArray<WorleyNoise> worleyNoiseMap)
    {
        NativeList<WorleyNoise> noiseSet = Util.Set<WorleyNoise>(worleyNoiseMap, Allocator.Temp);
        NativeArray<WorleyCell> cellSet = new NativeArray<WorleyCell>(noiseSet.Length, Allocator.TempJob);

        for(int i = 0; i < noiseSet.Length; i++)
        {
            WorleyNoise worleyNoise = noiseSet[i];

            WorleyCell cell = new WorleyCell {
                value = worleyNoise.currentCellValue,
                index = worleyNoise.currentCellIndex,
                position = worleyNoise.currentCellPosition
            };

            cellSet[i] = cell;
        }

        return cellSet;
    }
} */