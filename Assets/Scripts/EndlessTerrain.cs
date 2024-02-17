using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDistance = 450;

    public Transform[] viewers = new Transform[20];
    public static Vector2[] viewersPositions = new Vector2[20];

    static MapGenerator mapGenerator;
    public Material mapMaterial;

    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    void Update()
    {
        for (int i = 0; i < viewers.Length; i++)
        {
            viewersPositions[i] = new Vector2(viewers[i].position.x, viewers[i].position.z);
        }
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false, null);
        }
        
        terrainChunksVisibleLastUpdate.Clear();

        for (int i = 0; i < viewers.Length; i++)
        {
            int currentChunkCoordX = Mathf.RoundToInt(viewersPositions[i].x / chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(viewersPositions[i].y / chunkSize);

            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk(viewersPositions[i], viewers[i]);
                        if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                        {
                            terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                    }
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;
        private Transform viewerFirstInChunk;

        MapData mapData;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false, null);

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk(Vector2 viewerPosition, Transform viewer)
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible, viewer);
        }

        public void SetVisible(bool visible, Transform viewer)
        {
            if (visible && !meshObject.activeSelf)
            {
                meshObject.SetActive(true);
                viewerFirstInChunk = viewer;
            }
            else if (!visible && meshObject.activeSelf && viewer == viewerFirstInChunk)
            {
                meshObject.SetActive(false);
                viewerFirstInChunk = null;
            }
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
}