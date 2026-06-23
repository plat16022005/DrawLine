using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public int width = 20;
    public int height = 12;
    public float cellSize = 1f;
    public Material lineMaterial;

    public string sortingLayerName = "Grid";
    public int sortingOrder = 9999;
    public float zOffset = -5f;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        float startX = -width * cellSize / 2f;
        float startY = -height * cellSize / 2f;

        for (int x = 0; x <= width; x++)
        {
            float xPos = startX + x * cellSize;
            CreateLine(
                new Vector3(xPos, startY, zOffset),
                new Vector3(xPos, startY + height * cellSize, zOffset)
            );
        }

        for (int y = 0; y <= height; y++)
        {
            float yPos = startY + y * cellSize;
            CreateLine(
                new Vector3(startX, yPos, zOffset),
                new Vector3(startX + width * cellSize, yPos, zOffset)
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Grid Line");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}