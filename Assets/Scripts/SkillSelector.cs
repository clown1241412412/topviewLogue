using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillSelector : MonoBehaviour
{
    public static SkillSelector Instance;

    private GameObject canvasObj;
    private GameObject panelObj;
    private System.Action onComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowSelection(System.Action completeCallback)
    {
        onComplete = completeCallback;
        Time.timeScale = 0f; // Pause game

        CreateUI();
    }

    void CreateUI()
    {
        canvasObj = GameObject.Find("LevelCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("LevelCanvas");
            canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // EventSystem이 없으면 클릭이 안됨. 자동 생성 추가.
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // New Input System을 사용하는 프로젝트에서는 StandaloneInputModule 대신 InputSystemUIInputModule을 써야 함.
            if (Application.isEditor || true) // Input System Package가 설치된 것으로 간주
            {
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        panelObj = new GameObject("SkillSelectionPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Options
        List<string> allSkills = new List<string> { "Fireball (Q)", "Spin Attack (E)", "Parry (E)", "Sword Wave (R)", "Heal (+30 HP)", "Damage +1", "Max HP +20" };
        
        // 이미 획득한 액티브 스킬 제거
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Attack atk = player.GetComponent<Attack>();
            if (atk != null)
            {
                if (atk.hasFireball) allSkills.Remove("Fireball (Q)");
                // E키 스킬은 상호 배타적: 하나를 선택하면 다른 하나도 제거
                if (atk.hasSpin || atk.hasParry)
                {
                    allSkills.Remove("Spin Attack (E)");
                    allSkills.Remove("Parry (E)");
                }
                if (atk.hasSwordWave) allSkills.Remove("Sword Wave (R)");
            }
        }

        List<string> selectedSkills = new List<string>();
        
        while (selectedSkills.Count < 3 && allSkills.Count > 0)
        {
            int rnd = Random.Range(0, allSkills.Count);
            selectedSkills.Add(allSkills[rnd]);
            allSkills.RemoveAt(rnd);
        }

        for (int i = 0; i < selectedSkills.Count; i++)
        {
            CreateOption(i, selectedSkills[i]);
        }
    }

    void CreateOption(int index, string skillName)
    {
        GameObject buttonObj = new GameObject("Option_" + index);
        buttonObj.transform.SetParent(panelObj.transform, false);
        
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        Button btn = buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(() => OnSkillSelected(skillName));

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(350, 500); // 크기 확대 (250x100 -> 350x500)
        rect.anchoredPosition = new Vector2((index - 1) * 400, 0); // 간격 조정 (280 -> 400)

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 32; // 텍스트 크기도 조금 확대
        t.color = Color.white;
        t.text = skillName;
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
    }

    void OnSkillSelected(string skillName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Attack atk = player.GetComponent<Attack>();
            PlayerHealth hp = player.GetComponent<PlayerHealth>();

            if (skillName.Contains("Fireball")) atk.hasFireball = true;
            else if (skillName.Contains("Spin")) atk.hasSpin = true;
            else if (skillName.Contains("Parry")) atk.hasParry = true;
            else if (skillName.Contains("Sword Wave")) atk.hasSwordWave = true;
            else if (skillName.Contains("Heal"))
            {
                hp.currentHP = Mathf.Min(hp.maxHP, hp.currentHP + 30);
                player.SendMessage("UpdateHPBar", SendMessageOptions.DontRequireReceiver);
            }
            else if (skillName.Contains("Damage"))
            {
                atk.IncreaseDamage(1);
            }
            else if (skillName.Contains("Max HP"))
            {
                hp.IncreaseMaxHP(20);
            }
        }

        Destroy(panelObj);
        Time.timeScale = 1f;
        onComplete?.Invoke();
    }
}
