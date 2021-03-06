﻿using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEditor;
using MyComponents;

[UpdateAfter(typeof(MapGenerateMeshDataSystem))]
public class ApplyMeshDataSystem : ComponentSystem
{
    EntityManager entityManager;

    int squareWidth;

    ComponentGroup applyMeshGroup;

	public static Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ShaderGraphTest.mat");
    
    protected override void OnCreateManager()
    {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        squareWidth = TerrainSettings.mapSquareWidth;

        EntityArchetypeQuery applyMeshQuery = new EntityArchetypeQuery{
			None  	= new ComponentType[] { typeof(Tags.EdgeBuffer), typeof(Tags.OuterBuffer), typeof(Tags.InnerBuffer) },
			All  	= new ComponentType[] { typeof(MapSquare), typeof(MeshVertex) }
		};
        applyMeshGroup = GetComponentGroup(applyMeshQuery);
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
		NativeArray<ArchetypeChunk> chunks = applyMeshGroup.CreateArchetypeChunkArray(Allocator.TempJob);

		ArchetypeChunkEntityType entityType = GetArchetypeChunkEntityType();
		ArchetypeChunkComponentType<MapSquare> squareType = GetArchetypeChunkComponentType<MapSquare>(true);
        ArchetypeChunkBufferType<MeshVertex> vertType = GetArchetypeChunkBufferType<MeshVertex>(true);
        ArchetypeChunkBufferType<MeshNormal> normType = GetArchetypeChunkBufferType<MeshNormal>(true);
        ArchetypeChunkBufferType<MeshTriangle> triType = GetArchetypeChunkBufferType<MeshTriangle>(true);
        ArchetypeChunkBufferType<MeshVertColor> colorType = GetArchetypeChunkBufferType<MeshVertColor>(true);


		for(int c = 0; c < chunks.Length; c++)
		{
			ArchetypeChunk chunk = chunks[c];

			NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
			NativeArray<MapSquare> squares = chunk.GetNativeArray(squareType);

            BufferAccessor<MeshVertex> vertBuffers = chunk.GetBufferAccessor<MeshVertex>(vertType);
            BufferAccessor<MeshNormal> triBuffers = chunk.GetBufferAccessor<MeshNormal>(normType);
            BufferAccessor<MeshTriangle> normBuffers = chunk.GetBufferAccessor<MeshTriangle>(triType);
            BufferAccessor<MeshVertColor> colorBuffers = chunk.GetBufferAccessor<MeshVertColor>(colorType);
		    
            for(int e = 0; e < entities.Length; e++)
			{
				DebugTools.IncrementDebugCount("mesh data");
				
				Entity entity = entities[e];

				bool redraw = entityManager.HasComponent<Tags.Redraw>(entity);

                Mesh mesh = MakeMesh(vertBuffers[e], triBuffers[e], normBuffers[e], colorBuffers[e]);
                SetMeshComponent(redraw, mesh, entity, commandBuffer);

				if(redraw) commandBuffer.RemoveComponent(entity, typeof(Tags.Redraw));

				commandBuffer.RemoveComponent(entity, typeof(MeshVertex));
				commandBuffer.RemoveComponent(entity, typeof(MeshNormal));
				commandBuffer.RemoveComponent(entity, typeof(MeshTriangle));
				commandBuffer.RemoveComponent(entity, typeof(MeshVertColor));
            }
        }

        commandBuffer.Playback(entityManager);
        commandBuffer.Dispose();

        chunks.Dispose();
    }

    Mesh MakeMesh(DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshNormal> normals, DynamicBuffer<MeshTriangle> triangles, DynamicBuffer<MeshVertColor> colors)
	{
        //	Convert vertices and colors from float3/float4 to Vector3/Color
		Vector3[] verticesArray = new Vector3[vertices.Length];
        int[] trianglesArray = new int[triangles.Length];
        Vector3[] normalsArray 	= new Vector3[vertices.Length];		
        Color[] colorsArray 	= new Color[colors.Length];
		for(int i = 0; i < vertices.Length; i++)
		{
			verticesArray[i] 	= vertices[i].vertex;
			normalsArray[i] 	= normals[i].normal;
			colorsArray[i] 		= new Color(colors[i].color.x, colors[i].color.y, colors[i].color.z, colors[i].color.w);
		}

		for(int i = 0; i < triangles.Length; i++)
		{
            trianglesArray[i]   = triangles[i].triangle;
		}

		Mesh mesh 		= new Mesh();
		mesh.vertices	= verticesArray;
		mesh.normals 	= normalsArray;
		mesh.colors 	= colorsArray;
		mesh.SetTriangles(trianglesArray, 0);

		mesh.RecalculateNormals();

		return mesh;
	}

    // Apply mesh to MapSquare entity
	void SetMeshComponent(bool redraw, Mesh mesh, Entity entity, EntityCommandBuffer commandBuffer)
	{
		if(redraw) commandBuffer.RemoveComponent<RenderMesh>(entity);

		RenderMesh renderer = new RenderMesh();
		renderer.mesh = mesh;
		renderer.material = material;

		commandBuffer.AddSharedComponent(entity, renderer);
	}
    
	void SetPosition(Entity entity, MapSquare mapSquare, float3 currentPosition, EntityCommandBuffer commandBuffer)
	{
		Translation newPosition = new Translation { Value = new float3(currentPosition.x, mapSquare.bottomBlockBuffer, currentPosition.z) };
		commandBuffer.SetComponent<Translation>(entity, newPosition);
	}
}
