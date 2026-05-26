using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class TerrainGenerator : NetworkBehaviour
{
    public bool On = true;
    public static TerrainGenerator instance { get ; private set ; }
    public RulesetData[] Rulesets;
    public int RulesetDataIndex;
    public int Width;
    public int Height;
    public Vector3 SpawnCoordinates; // TODO: created network variable
    public NetworkList<int> TerrainDataIndicies_SERVER;
    public float ObjectWidth;
    public GameObject ANodePrefab;
    public Transform TerrainParent;
    public Transform ANodeParent;
    public List<ANode> AllNodes = new List<ANode>();
    public DebugUICanvas debugUI;

    private ModuleSO[,] terrainData;

    public static event Action TerrainGenerated; // Must be subscribed to in Awake()

    void Awake()
    {
        instance = this;
        TerrainDataIndicies_SERVER = new NetworkList<int>(default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server); 
    }

    // TODO: Later: in games, this will be spawned after network
    public override void OnNetworkSpawn()
    {
        FindAllRulesets(out Rulesets);

        if (IsHost)
        {
            TerrainDataIndicies_SERVER.Add(Width);
            TerrainDataIndicies_SERVER.Add(Height);
            GenerateNewTerrain();
        }
        else
        {
            if (TerrainDataIndicies_SERVER == null)
            {
                Debug.LogWarning("Terrain generated indices list is empty");
            }
            else
            {
                GenerateNetworkTerrain();
            }
        }
    }

    private List<ModuleSO> GetRuleset()
    {
        return Rulesets[RulesetDataIndex].GetModuleList();
    }

    private void GenerateNewTerrain()
    {
        List<ModuleSO> ruleset = GetRuleset();
        Dictionary<ModuleSO, int> reverseModuleDictionary = new Dictionary<ModuleSO, int>();

        for (int i = 0; i < ruleset.Count; i++)
        {
            reverseModuleDictionary.Add(ruleset[i], i);
        }
        
        if (On)
        {
            terrainData = WaveFunctionCollapse.Generate(ruleset, Width, Height);
            debugUI.AddLog("Terrain data generated for a " + terrainData.GetLength(0) + " by " + terrainData.GetLength(1) + " grid.");
            Debug.Log(terrainData.GetLength(0) + " by " + terrainData.GetLength(1) + " grid.");
            PlaceTerrain(reverseModuleDictionary);
        }
    }

    private void GenerateNetworkTerrain()
    {
        if (On)
        {
            Width = TerrainDataIndicies_SERVER[0];
            Height = TerrainDataIndicies_SERVER[1];

            terrainData = new ModuleSO[Width, Height];
            TerrainIndiciesToModuleSO();

            debugUI.AddLog("Terrain data generated for a " + terrainData.GetLength(0) + " by " + terrainData.GetLength(1) + " grid.");
            Debug.Log(terrainData.GetLength(0) + " by " + terrainData.GetLength(1) + " grid.");
            if (terrainData[0, 0] == null)
            {
                Debug.LogWarning("2. before place terrain func: null data");
            }
            PlaceTerrain();
        }
    }

    private void TerrainIndiciesToModuleSO()
    {
        List<ModuleSO> ruleset = GetRuleset();

        Debug.Log("Terrain ruleset used: ");
        foreach (ModuleSO rule in ruleset)
        {
            Debug.Log("rule " + rule.Name);
        }

        //Debug.Log("Terrain: network list count: " + TerrainDataIndicies_SERVER.Count);

        for (int i = 2; i < TerrainDataIndicies_SERVER.Count; i++)
        {
            (int, int) coordinates = GetCoordinateFromIndex(i - 2);

            Debug.Log(" i = " + i + ", modified = " + (i - 2));
            if (coordinates.Item1 > 18 ||  coordinates.Item2 > 18)
            {
                Debug.Log(">>coordinates: " + coordinates.ToString());
            }
            
            terrainData[coordinates.Item1, coordinates.Item2] = ruleset[TerrainDataIndicies_SERVER[i]];

            if (ruleset[TerrainDataIndicies_SERVER[i]] == null)
            {
                Debug.LogWarning("Module SO hasn't been retrieved correctly and is null");
            }

            if (terrainData[coordinates.Item1, coordinates.Item2] == null)
            {
                Debug.Log("houston we have aprobelem");
            }
        }

        if (terrainData[0,0] == null)
        {
            Debug.LogWarning("1. terrain allocaiton func: null data at 0,0");
        }

        Debug.Log(" terrain data count columns: " + terrainData.Length);
    }

    private void PlaceTerrain(Dictionary<ModuleSO, int> reverseModuleDictionary = null)
    {
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                Vector3 position = ConvertGridSpaceToWorldSpace(i,j);

                if (terrainData[i,j] == null)
                {
                    Debug.LogWarning("Terrain data is the issue at (" + i + ", " + j + ").");
                }
                else if (terrainData[i,j].Prefab == null)
                {
                    Debug.LogWarning("Terrin data prefab is the issue (" + i + ", " + j + ").");
                }

                if (position == null)
                {
                    Debug.LogWarning(" position is the issue (" + i + ", " + j + ").");
                }

                if (TerrainParent == null)
                {
                    Debug.LogWarning("Terrain parent is the issue (" + i + ", " + j + ").");
                }


                GameObject newTerrain = Instantiate(terrainData[i,j].Prefab, position, Quaternion.identity, TerrainParent);
                newTerrain.transform.localScale *= ObjectWidth;

                if (reverseModuleDictionary != null)
                {
                    TerrainDataIndicies_SERVER.Add(reverseModuleDictionary[terrainData[i, j]]);
                }
                
                // Note that this will change for obstacles.
                // This will mess up the getneighboring nodes function, so will need to check if the node exists
                ANode newNode = Instantiate(ANodePrefab, new Vector3(position.x, position.y + terrainData[i,j].Prefab.transform.localScale.y, position.z), Quaternion.identity, ANodeParent).GetComponent<ANode>(); // TODO: determine if need to make the width smaller depending on tile size later
                AllNodes.Add(newNode);
            }
        }

        // Go through every Node in the list and set all connections
        for (int i = 0; i < AllNodes.Count; i++)
        {
            AllNodes[i].Connections = GetNeighboringNodes(i);
        }

        //debugUI.AddLog("ANodes list has " + AllNodes.Count + " nodes.");

        TerrainGenerated?.Invoke();
    }

    private List<ANode> GetNeighboringNodes(int index)
    {
        List<ANode> output = new List<ANode>();
        List<int> neighboringIndicies = new List<int>();
        
        (int, int) currentCoordinate = GetCoordinateFromIndex(index);
        //Debug.Log("Starting coordinate " + currentCoordinate.ToString());

        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                (int, int) neighboorCoordinate = (currentCoordinate.Item1 + i, currentCoordinate.Item2 + j);
                //Debug.Log("Checking Neighbor coordinate: " + neighboorCoordinate.ToString());
                if (currentCoordinate.Item1 + i >= Width || currentCoordinate.Item1 + i < 0)
                {
                    continue;
                }
                else if (currentCoordinate.Item2 + j >= Height || currentCoordinate.Item2 + j < 0)
                {
                    continue;
                }
                else if (currentCoordinate.Item1 + i == currentCoordinate.Item1 && currentCoordinate.Item2 + j == currentCoordinate.Item2)
                {
                    continue;
                }

                //neighboorCoordinate = (currentCoordinate.Item1 + i, currentCoordinate.Item2 + j);
                neighboringIndicies.Add(GetIndexFromCoordiate(neighboorCoordinate));
                output.Add(AllNodes[GetIndexFromCoordiate(neighboorCoordinate)]);
                /*if (GetIndexFromCoordiate(neighboorCoordinate) >= Width * Height || GetIndexFromCoordiate(neighboorCoordinate) < 0)
                {
                    Debug.LogWarning("index out of range");
                }*/
                //Debug.Log("Adding neighbor of " + currentCoordinate.ToString() + ": " + neighboorCoordinate.ToString() + " at index " + GetIndexFromCoordiate(neighboorCoordinate));
            }
        }

        return output;
    }

    private (int, int) GetCoordinateFromIndex(int index)
    {
        int col = index % Width; 
        int row = index / Height; 

        return (col, row);
    }

    private int GetIndexFromCoordiate((int, int) coordinate)
    {
        return (coordinate.Item2 * Height) + coordinate.Item1;
    }

    private Vector3 ConvertGridSpaceToWorldSpace(int column, int row)
    {
        float x = SpawnCoordinates.x + (column * (ObjectWidth));
        float z = SpawnCoordinates.z + (row * (ObjectWidth));
        return new Vector3(x,0,z);
    }

    private void FindAllRulesets(out RulesetData[] rulesets)
    {
        rulesets = Resources.LoadAll<RulesetData>("Terrain/Rulesets");
    }
}
