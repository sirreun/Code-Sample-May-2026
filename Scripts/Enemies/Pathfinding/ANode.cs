using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ANode : MonoBehaviour
{
    public ANode CameFrom;
    public List<ANode> Connections;

    public float GScore;
    public float HScore;

    public float FScore()
    {
        return GScore + HScore;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if (Connections.Count > 0)
        {
            foreach (ANode node in Connections)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }
#endif
}
