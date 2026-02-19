using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public int level = 1;
    public int currentExp = 0;
    public int requiredExp = 10;
    public int expIncreasePerLevel = 5;

    // UI Components
    private GameObject canvasObj;
    private Image expBarFill;

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
                playerHealth.IncreaseMaxHP(20);
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
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            if (Application.isEditor || true)
            {
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
    }
}
