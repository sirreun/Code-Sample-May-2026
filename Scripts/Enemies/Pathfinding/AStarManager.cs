using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AStarManager : MonoBehaviour
{
    public static event Action SetEnemySpawns;

    public List<ANode> AllNodes;

    public static AStarManager instance { get; private set; }

    void Awake()
    {
        instance = this;
        AllNodes = new List<ANode>();
        TerrainGenerator.TerrainGenerated += SetAllNodes;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void SetAllNodes()
    {
        AllNodes = TerrainGenerator.instance.AllNodes;
        SetEnemySpawns?.Invoke();
    }

    public List<ANode> DeterminePath(ANode start, ANode end)
    {
        List<ANode> currentNodes = new List<ANode>();

        foreach (ANode node in AllNodes)
        {
            node.GScore = float.MaxValue;
        }

        start.GScore = 0;
        start.HScore = Vector2.Distance(start.transform.position, end.transform.position); // Vecor2?
        currentNodes.Add(start);

        while (currentNodes.Count > 0)
        {
            int lowestF = default;
            
            for (int i = 1; i < currentNodes.Count; i++)
            {
                if (currentNodes[i].FScore() < currentNodes[lowestF].FScore())
                {
                    lowestF = i;
                }
            }

            ANode currentNode = currentNodes[lowestF];
            currentNodes.Remove(currentNode);

            if (currentNode == end)
            {
                List<ANode> path = new List<ANode>();

                path.Insert(0, end);

                while (currentNode != start)
                {
                    currentNode = currentNode.CameFrom;
                    path.Add(currentNode);
                }

                path.Reverse();
                return path;
            }

            foreach (ANode connectedNode in currentNode.Connections)
            {
                float heldGScore = currentNode.GScore + Vector2.Distance(currentNode.transform.position, connectedNode.transform.position); // Vector 2??

                if (heldGScore < connectedNode.GScore)
                {
                    connectedNode.CameFrom = currentNode;
                    connectedNode.GScore = heldGScore;
                    connectedNode.HScore = Vector3.Distance(connectedNode.transform.position, end.transform.position);

                    if (!currentNodes.Contains(connectedNode))
                    {
                        currentNodes.Add(connectedNode);
                    }
                }
            }
        }

        return null;
    }
}
