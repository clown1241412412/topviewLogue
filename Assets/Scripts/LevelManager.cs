using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // ... (기존 코드 유지)
    public static LevelManager Instance;

    public int level = 1;
    public int currentExp = 0;
    public int requiredExp = 10;
    public int expIncreasePerLevel = 5;

    // UI Components
    private GameObject canvasObj;
    private Image expBarFill;
    
    // Boss HP Bar UI
    private GameObject bossHPBarObj;
    private Image bossHPFill;
    private Text bossHPText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1; // 씬 시작 시 시간 흐름 보장
        CreateEXPBar();
        EnsureEventSystem();
        CreateDebugUI();
    }

    void CreateEXPBar()
    {
        // 1. Canvas 찾거나 생성 (PlayerHealth에서 이미 만들었을 수도 있음)
        canvasObj = GameObject.Find("LevelCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("LevelCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101; // HP바보다 위에
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. EXP Bar 배경 (검은색, 화면 상단 전체)
        GameObject bgObj = new GameObject("ExpBarBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = new Vector2(0, 0);
        bgRect.sizeDelta = new Vector2(0, 20); // 높이 20, 너비는 화면 꽉 채움

        // 3. EXP Bar 채우기 (파란색)
        GameObject fillObj = new GameObject("ExpBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        expBarFill = fillObj.AddComponent<Image>();
        expBarFill.color = Color.blue;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        UpdateEXPUI();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        // 레벨업 체크
        while (currentExp >= requiredExp)
        {
            LevelUp();
        }

        UpdateEXPUI();
    }

    void LevelUp()
    {
        currentExp -= requiredExp;
        level++;
        requiredExp += expIncreasePerLevel;
        
        Debug.Log("Level Up! New Level: " + level);

        // SkillSelector 찾기 (없으면 생성)
        if (SkillSelector.Instance == null)
        {
            GameObject selectorObj = new GameObject("SkillSelector");
            selectorObj.AddComponent<SkillSelector>();
        }

        // 스킬 선택 창 띄우기
        SkillSelector.Instance.ShowSelection(() => {
            Debug.Log("Skill selection complete for level " + level);
        });

        // Player 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. 체력 증가 (+20)
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.IncreaseMaxHP(10);
                playerHealth.UpdateLevelText(level);
            }

            // 2. 공격력 증가 (+1)
            Attack playerAttack = player.GetComponent<Attack>();
            if (playerAttack != null)
            {
                playerAttack.IncreaseDamage(1);
            }
        }
    }

    void UpdateEXPUI()
    {
        if (expBarFill != null)
        {
            float ratio = (float)currentExp / requiredExp;
            // fillAmount 대신 단순히 scale이나 anchor를 조절하는 방식 사용 (Image Type을 Simple로 가정)
            // 여기서는 앵커 조절 방식으로 구현
            RectTransform fillRect = expBarFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(ratio, 1);
        }
    }

    void CreateDebugUI()
    {
        if (canvasObj == null) return;

        // Level Up Button (오른쪽 하단)
        GameObject btnObj = new GameObject("LevelUpCheatButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            Debug.Log("[Cheat] Level Up button clicked!");
            // 현재 필요 경험치만큼 더해서 즉시 레벨업
            AddExp(requiredExp - currentExp);
        });

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0); // 우측 하단
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20); // 여백
        rect.sizeDelta = new Vector2(120, 40);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 20;
        t.color = Color.white;
        t.text = "Level Up";
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;

        // Next Wave Button (Level Up 버튼 위에)
        GameObject skipBtnObj = new GameObject("SkipWaveCheatButton");
        skipBtnObj.transform.SetParent(canvasObj.transform, false);
        
        Image skipImg = skipBtnObj.AddComponent<Image>();
        skipImg.color = new Color(0.3f, 0.1f, 0.1f, 0.8f); // 약간 붉은 계열
        
        Button skipBtn = skipBtnObj.AddComponent<Button>();
        skipBtn.onClick.AddListener(() => {
            if (EnemySpawner.Instance != null)
            {
                Debug.Log("[Cheat] Next Wave button clicked!");
                EnemySpawner.Instance.SkipWave();
            }
        });

        RectTransform skipRect = skipBtnObj.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1, 0);
        skipRect.anchorMax = new Vector2(1, 0);
        skipRect.pivot = new Vector2(1, 0);
        skipRect.anchoredPosition = new Vector2(-20, 70); // Level Up 버튼 위
        skipRect.sizeDelta = new Vector2(120, 40);

        GameObject skipTextObj = new GameObject("Text");
        skipTextObj.transform.SetParent(skipBtnObj.transform, false);
        Text skipT = skipTextObj.AddComponent<Text>();
        skipT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipT.fontSize = 20;
        skipT.color = Color.white;
        skipT.text = "Next Wave";
        skipT.alignment = TextAnchor.MiddleCenter;
        
        RectTransform skipTRect = skipTextObj.GetComponent<RectTransform>();
        skipTRect.anchorMin = Vector2.zero;
        skipTRect.anchorMax = Vector2.one;
        skipTRect.sizeDelta = Vector2.zero;
    }

    public void GameOver(bool isWin = false)
    {
        // 중복 생성 방지
        if (GameObject.Find("GameOverPanel") != null) return;

        // 1. 게임 일시 정지
        Time.timeScale = 0;

        // 2. 배경 페널 생성 (검은색)
        GameObject gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f); 
        panelImage.raycastTarget = true;

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        gameOverPanel.transform.SetAsLastSibling();

        // 3. GAME END 텍스트 생성
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(gameOverPanel.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 80;
        t.color = isWin ? Color.yellow : Color.red;
        t.text = isWin ? "VICTORY!" : "GAME OVER";
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; 

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 80);
        textRect.sizeDelta = new Vector2(600, 100);

        // 4. 재시작 버튼 생성
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(gameOverPanel.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.8f, 0.2f, 1.0f); 
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img; // 확실하게 지정
        btn.onClick.AddListener(RestartGame);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -70); // 텍스트 아래
        rect.sizeDelta = new Vector2(200, 60);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text bt = btnTextObj.AddComponent<Text>();
        bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bt.fontSize = 30;
        bt.color = Color.white;
        bt.text = "RESTART";
        bt.alignment = TextAnchor.MiddleCenter;
        
        RectTransform btRect = btnTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;
    }

    public void RestartGame()
    {
        Debug.Log("RestartGame called!");
        Time.timeScale = 1; // 시간 흐름 복구
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CreateBossHPBar(int maxHP)
    {
        if (bossHPBarObj != null) Destroy(bossHPBarObj);

        // 1. Boss HP Bar 배경
        bossHPBarObj = new GameObject("BossHPBarBG");
        bossHPBarObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bossHPBarObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        RectTransform bgRect = bossHPBarObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 1);
        bgRect.anchorMax = new Vector2(0.5f, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = new Vector2(0, -60); // EXP바 아래, Wave Count 아래 쯤
        bgRect.sizeDelta = new Vector2(600, 40);

        // 2. Boss HP Bar 채우기 (빨간색)
        GameObject fillObj = new GameObject("BossHPFill");
        fillObj.transform.SetParent(bossHPBarObj.transform, false);
        bossHPFill = fillObj.AddComponent<Image>();
        bossHPFill.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // 3. Boss HP 텍스트
        GameObject hpTextObj = new GameObject("BossHPText");
        hpTextObj.transform.SetParent(bossHPBarObj.transform, false);
        bossHPText = hpTextObj.AddComponent<Text>();
        bossHPText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossHPText.fontSize = 24;
        bossHPText.color = Color.white;
        bossHPText.alignment = TextAnchor.MiddleCenter;
        bossHPText.text = maxHP + " / " + maxHP;

        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;
    }

    public void UpdateBossHPBar(int currentHP, int maxHP)
    {
        if (bossHPFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            bossHPFill.rectTransform.anchorMax = new Vector2(ratio, 1);
        }
        if (bossHPText != null)
        {
            bossHPText.text = currentHP + " / " + maxHP;
        }
    }

    public void HideBossHPBar()
    {
        if (bossHPBarObj != null)
        {
            bossHPBarObj.SetActive(false);
        }
    }

    void EnsureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        // New Input System 사용 시 필수 모듈 체크 및 추가
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            // 기존의 StandaloneInputModule이 있다면 제거 (충돌 방지)
            UnityEngine.EventSystems.BaseInputModule oldModule = es.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
            if (oldModule != null && !(oldModule is UnityEngine.InputSystem.UI.InputSystemUIInputModule))
            {
                DestroyImmediate(oldModule);
            }
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
