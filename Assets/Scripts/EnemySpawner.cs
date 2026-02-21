using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [Header("웨이브 설정")]
    public int currentWave = 1;
    public int enemiesPerWaveBase = 10;
    public float waveInterval = 3f; // 웨이브 사이 대기 시간

    private int enemiesToDefeat;
    private int enemiesDefeated;
    private bool isWaveActive = false;
    private float timer;

    // UI Components
    private GameObject canvasObj;
    private Text waveText;
    private Text countText;

    // Singleton for easy access
    public static EnemySpawner Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        CreateWaveUI();
        StartCoroutine(StartWaveRoutine(currentWave));
    }

    void CreateWaveUI()
    {
        // Canvas 찾기 (LevelCanvas 사용)
        canvasObj = GameObject.Find("LevelCanvas");
        if (canvasObj == null)
        {
            // LevelManager가 아직 안 만들었으면 생성 (보통 LevelManager가 먼저 돔)
            canvasObj = new GameObject("LevelCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Wave Text (화면 중앙 상단)
        GameObject waveTextObj = new GameObject("WaveText");
        waveTextObj.transform.SetParent(canvasObj.transform, false);
        waveText = waveTextObj.AddComponent<Text>();
        waveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 폰트
        waveText.fontSize = 60;
        waveText.color = Color.yellow;
        waveText.alignment = TextAnchor.MiddleCenter;
        waveText.text = "";
        
        RectTransform waveRect = waveTextObj.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(0.5f, 0.5f);
        waveRect.anchorMax = new Vector2(0.5f, 0.5f);
        waveRect.pivot = new Vector2(0.5f, 0.5f);
        waveRect.anchoredPosition = new Vector2(0, 100); // 중앙에서 약간 위
        waveRect.sizeDelta = new Vector2(400, 100);

        // Count Text (화면 최상단 중앙)
        GameObject countTextObj = new GameObject("CountText");
        countTextObj.transform.SetParent(canvasObj.transform, false);
        countText = countTextObj.AddComponent<Text>();
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize = 30; // 공격력 텍스트보다 조금 크게
        countText.color = Color.white;
        countText.alignment = TextAnchor.UpperCenter;
        countText.text = "0 / 10";

        RectTransform countRect = countTextObj.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 1);
        countRect.anchorMax = new Vector2(0.5f, 1);
        countRect.pivot = new Vector2(0.5f, 1);
        countRect.anchoredPosition = new Vector2(0, -20); // 최상단
        countRect.sizeDelta = new Vector2(200, 50);
    }

    IEnumerator StartWaveRoutine(int wave)
    {
        isWaveActive = false;
        
        // 웨이브 텍스트 표시
        if (waveText != null)
        {
            waveText.text = "Wave " + wave;
            waveText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(3f); // 3초 대기

        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }

        StartWave(wave);
    }

    void StartWave(int wave)
    {
        currentWave = wave;
        // 적 수 증가 (+10씩)
        enemiesToDefeat = enemiesPerWaveBase + (wave - 1) * 10; 
        enemiesDefeated = 0;
        isWaveActive = true;
        
        UpdateCountUI();

        // 1.5배씩 빨라지는 스폰 주기 (최소 0.2초)
        float currentSpawnInterval = 3f / Mathf.Pow(1.5f, wave - 1);
        spawnInterval = Mathf.Max(0.2f, currentSpawnInterval);

        Debug.Log("Wave " + wave + " Start! Goal: " + enemiesToDefeat + ", Interval: " + spawnInterval);
    }

    void Update()
    {
        if (!isWaveActive || enemyPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            
            // 한번에 여러 마리 나오게 (웨이브 번호만큼 스폰)
            int spawnCount = currentWave; 

            for(int i=0; i<spawnCount; i++)
            {
                 SpawnEnemy();
            }
        }
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = GetSafeSpawnPosition();
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetHPByWave(currentWave);
        }
    }

    public void OnEnemyKilled()
    {
        if (!isWaveActive) return;

        enemiesDefeated++;
        UpdateCountUI();

        if (enemiesDefeated >= enemiesToDefeat)
        {
            EndWave();
        }
    }

    public void SkipWave()
    {
        if (!isWaveActive) return;

        // 모든 적 제거
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in allEnemies)
        {
            Destroy(e.gameObject);
        }

        EndWave();
    }

    void EndWave()
    {
        isWaveActive = false;
        Debug.Log("Wave " + currentWave + " Cleared!");
        
        // 다음 웨이브 준비
        StartCoroutine(StartWaveRoutine(currentWave + 1));
    }

    void UpdateCountUI()
    {
        if (countText != null)
        {
            countText.text = enemiesDefeated + " / " + enemiesToDefeat;
        }
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

        return new Vector3(-halfW, -halfH, 0);
    }
}
