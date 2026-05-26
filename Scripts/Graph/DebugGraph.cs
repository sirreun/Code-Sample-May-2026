using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey.Utils;

public class DebugGraph : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite;
    private RectTransform graphContainer;
    private const float maxRadiation = 243f;
    public int maxReadings = 100;
    private int numberOfReadings = 0;
    private List<float> data = new List<float>();
    private float graphHeight;
    private float graphLength;
    public bool ShowDebugGraph = false;
    private bool graphToBeUpdated = false;

    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        graphHeight = graphContainer.sizeDelta.y;
        graphLength = graphContainer.sizeDelta.x;
    }

    private void Update()
    {
        UpdateGraph(); 
    }

    private void UpdateGraph()
    {
        if (!graphToBeUpdated)
        {
            return;
        }

        // TODO : consider moving the lines instead of destroying everything each update
        foreach (Transform dataPoint in graphContainer)
        {
            if (dataPoint.gameObject.tag == "dataPoint")
            {
                Destroy(dataPoint.gameObject);
            }
        }

        if (!ShowDebugGraph)
        {
            return;
        }
        
        for (int i = 0; i < data.Count; i++)
        {
            if (i > 0)
            {
                CreateDotConnection(new Vector2(i - 1, data[i - 1]), new Vector2(i, data[i]));
            }
        }

        graphToBeUpdated = false;
    }

    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.gameObject.tag = "dataPoint";
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(1, 2); // Data point size
        rectTransform.anchorMin = new Vector2 (0, 0);
        rectTransform.anchorMax = new Vector2 (0, 0);

        //Debug.Log("DebugGraph: Added point at (" + anchoredPosition.x + ", " + anchoredPosition.y + ").");
        return gameObject;
    }

    public void AddData(float rawData)
    {
        numberOfReadings += 1;
        if (numberOfReadings >= maxReadings)
        {
            numberOfReadings = maxReadings;
            data.RemoveAt(0);
        }
        
        float yPosition = (rawData / maxRadiation) * graphHeight; // Normalizes data point to graph container

        data.Add(yPosition);
        graphToBeUpdated = true;
    }

    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        //Debug.Log(" dot a pos: " + dotPositionA.ToString() + ", dot pos b: " + dotPositionB.ToString());
        GameObject gameObject = new GameObject("dotConnection", typeof(Image));
        gameObject.transform.gameObject.tag = "dataPoint";
        gameObject.transform.SetParent(graphContainer, false);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 direction = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);
        rectTransform.anchorMin = new Vector2 (0, 0);
        rectTransform.anchorMax = new Vector2 (0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 0.7f); 

        float mX = (dotPositionA.x + dotPositionB.x) / 2;
        float mY = (dotPositionA.y + dotPositionB.y) / 2;
        //Debug.Log(" line pos: (" + mX + ", " + mY + ") ");
        rectTransform.anchoredPosition = new Vector3 (mX, mY, 0);//dotPositionA;// + (direction * distance * 0.5f);
        rectTransform.localEulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(direction));
    }
}
