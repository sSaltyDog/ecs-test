﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainTypes { DIRT, GRASS, CLIFF }

public static class TerrainSettings
{
	public const int mapSquareWidth = 12;
	public const int viewDistance = 8;

	//	Must always be at >= squareWidth
	public const int terrainHeight = 16;
	public const int seed = 5678;

	public const float cellFrequency = 0.02f;
	public const float cellEdgeSmoothing = 10.0f;
	public const float cellularJitter = 0.15f;

	public static int BiomeIndex(float noise)
	{
		if(noise > 0.5f) return 1;
		return 0;
	}
}
