using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;

    [Header("스폰 범위 (맵 크기에 맞게 조절)")]
    public float mapWidth = 20f;
    public float mapHeight = 14f;

    [Header("플레이어 근처 스폰 방지")]
    public float minDistanceFromPlayer = 5f;

    private Transform player;
    private float timer;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 시작 시 로그 출력으로 동작 확인
        Debug.Log("EnemySpawner 시작! 맵 크기: " + mapWidth + " x " + mapHeight);
    }

    void Update()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy Prefab이 연결되지 않았습니다! Inspector에서 설정하세요.");
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = GetSafeSpawnPosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Debug.Log("적 스폰 위치: " + spawnPos);
    }

    Vector3 GetSafeSpawnPosition()
    {
        float halfW = mapWidth / 2f - 1f;
        float halfH = mapHeight / 2f - 1f;

        for (int i = 0; i < 30; i++)
        {
            float x = Random.Range(-halfW, halfW);
            float y = Random.Range(-halfH, halfH);
            Vector3 pos = new Vector3(x, y, 0);

            if (player == null || Vector3.Distance(pos, player.position) >= minDistanceFromPlayer)
            {
                return pos;
            }
        }

        // 안전한 위치를 못 찾으면 맵 구석
        return new Vector3(-halfW, -halfH, 0);
    }
}
