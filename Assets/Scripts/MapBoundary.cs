using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [Header("맵 크기 설정")]
    public float mapWidth = 20f;
    public float mapHeight = 14f;

    [Header("벽 두께")]
    public float wallThickness = 1f;

    void Awake()
    {
        CreateWall("Wall_Top",    new Vector2(0, mapHeight / 2 + wallThickness / 2),  new Vector2(mapWidth + wallThickness * 2, wallThickness));
        CreateWall("Wall_Bottom", new Vector2(0, -mapHeight / 2 - wallThickness / 2), new Vector2(mapWidth + wallThickness * 2, wallThickness));
        CreateWall("Wall_Left",   new Vector2(-mapWidth / 2 - wallThickness / 2, 0),  new Vector2(wallThickness, mapHeight + wallThickness * 2));
        CreateWall("Wall_Right",  new Vector2(mapWidth / 2 + wallThickness / 2, 0),   new Vector2(wallThickness, mapHeight + wallThickness * 2));
    }

    void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.parent = transform;
        wall.transform.position = new Vector3(position.x, position.y, 0);

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    // Scene 뷰에서 맵 경계를 시각적으로 확인할 수 있도록 기즈모 표시
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // 상
        Gizmos.DrawWireCube(new Vector3(0, mapHeight / 2 + wallThickness / 2, 0),  new Vector3(mapWidth + wallThickness * 2, wallThickness, 0));
        // 하
        Gizmos.DrawWireCube(new Vector3(0, -mapHeight / 2 - wallThickness / 2, 0), new Vector3(mapWidth + wallThickness * 2, wallThickness, 0));
        // 좌
        Gizmos.DrawWireCube(new Vector3(-mapWidth / 2 - wallThickness / 2, 0, 0),  new Vector3(wallThickness, mapHeight + wallThickness * 2, 0));
        // 우
        Gizmos.DrawWireCube(new Vector3(mapWidth / 2 + wallThickness / 2, 0, 0),   new Vector3(wallThickness, mapHeight + wallThickness * 2, 0));

        // 맵 영역 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapWidth, mapHeight, 0));
    }
}
