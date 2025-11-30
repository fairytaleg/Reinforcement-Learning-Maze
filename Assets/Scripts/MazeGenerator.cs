using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GÜNCELLEME: Duvar kalýnlýðý sorunu çözüldü.
// Artýk duvarlar "0.1" kalýnlýðýnda üretiliyor, böylece yollar kapanmýyor.

public class MazeGenerator : MonoBehaviour
{
    [Header("Labirent Ayarlarý")]
    public int width = 10;
    public int height = 10;
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    // Duvar kalýnlýðý (0.1 idealdir)
    private float wallThickness = 0.1f;

    public class Cell
    {
        public bool visited = false;
        public bool[] walls = { true, true, true, true }; // Top, Right, Bottom, Left
    }

    private Cell[,] grid;
    private List<GameObject> spawnedWalls = new List<GameObject>();

    public void GenerateMaze(Transform parent)
    {
        ClearMaze();

        grid = new Cell[width, height];

        // 1. Zeminleri ve Hücreleri Oluþtur
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell();

                // Zemini oluþtur (Yolun temeli)
                if (floorPrefab != null)
                {
                    // Zemini tam merkeze koyuyoruz
                    GameObject floor = Instantiate(floorPrefab, new Vector3(x, 0, y), Quaternion.identity, parent);
                    floor.name = $"Floor_{x}_{y}";
                    // Zemini ince bir plaka yapalým ki duvarlarla karýþmasýn
                    floor.transform.localScale = new Vector3(1, 0.1f, 1);
                    spawnedWalls.Add(floor);
                }
            }
        }

        // 2. Recursive Backtracker ile Yollarý Aç
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(0, 0);
        grid[current.x, current.y].visited = true;
        stack.Push(current);

        while (stack.Count > 0)
        {
            List<int> neighbors = GetUnvisitedNeighbors(current);
            if (neighbors.Count > 0)
            {
                int nextIndex = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int next = GetNeighborCoords(current, nextIndex);

                RemoveWall(current, next, nextIndex);

                grid[next.x, next.y].visited = true;
                stack.Push(next);
                current = next;
            }
            else
            {
                current = stack.Pop();
            }
        }

        // 3. Duvarlarý Fiziksel Olarak Sahneye Koy
        DrawMaze(parent);
    }

    void DrawMaze(Transform parent)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell c = grid[x, y];
                Vector3 pos = new Vector3(x, 0.5f, y); // Duvarlar zeminden 0.5 yukarýda olsun

                // Duvarlarý oluþtururken SCALE (Boyut) ayarý yapýyoruz
                // Vector3(1, 1, wallThickness) -> Geniþlik 1, Yükseklik 1, Kalýnlýk 0.1

                if (c.walls[0]) SpawnWall(pos + new Vector3(0, 0, 0.5f), new Vector3(1, 1, wallThickness), parent); // Üst (Top)
                if (c.walls[1]) SpawnWall(pos + new Vector3(0.5f, 0, 0), new Vector3(wallThickness, 1, 1), parent); // Sað (Right) - Kalýnlýk X ekseninde

                // Sadece en alt ve en sol kenarlar için ekstra duvar çiz
                if (y == 0 && c.walls[2]) SpawnWall(pos + new Vector3(0, 0, -0.5f), new Vector3(1, 1, wallThickness), parent); // Alt
                if (x == 0 && c.walls[3]) SpawnWall(pos + new Vector3(-0.5f, 0, 0), new Vector3(wallThickness, 1, 1), parent); // Sol
            }
        }
    }

    void SpawnWall(Vector3 pos, Vector3 scale, Transform parent)
    {
        if (wallPrefab == null) return;

        GameObject w = Instantiate(wallPrefab, pos, Quaternion.identity, parent);
        w.name = "Wall";
        w.transform.localScale = scale; // <-- ÝÞTE ÇÖZÜM BURASI
        spawnedWalls.Add(w);
    }

    void RemoveWall(Vector2Int current, Vector2Int next, int direction)
    {
        grid[current.x, current.y].walls[direction] = false;
        int opposite = (direction + 2) % 4;
        grid[next.x, next.y].walls[opposite] = false;
    }

    List<int> GetUnvisitedNeighbors(Vector2Int p)
    {
        List<int> neighbors = new List<int>();
        if (p.y + 1 < height && !grid[p.x, p.y + 1].visited) neighbors.Add(0); // Top
        if (p.x + 1 < width && !grid[p.x + 1, p.y].visited) neighbors.Add(1); // Right
        if (p.y - 1 >= 0 && !grid[p.x, p.y - 1].visited) neighbors.Add(2); // Bottom
        if (p.x - 1 >= 0 && !grid[p.x - 1, p.y].visited) neighbors.Add(3); // Left
        return neighbors;
    }

    Vector2Int GetNeighborCoords(Vector2Int p, int dir)
    {
        if (dir == 0) return new Vector2Int(p.x, p.y + 1);
        if (dir == 1) return new Vector2Int(p.x + 1, p.y);
        if (dir == 2) return new Vector2Int(p.x, p.y - 1);
        if (dir == 3) return new Vector2Int(p.x - 1, p.y);
        return p;
    }

    public void ClearMaze()
    {
        foreach (GameObject go in spawnedWalls)
        {
            if (go != null) Destroy(go);
        }
        spawnedWalls.Clear();
    }
}