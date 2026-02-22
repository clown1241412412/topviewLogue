using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 60;
    public int currentHP;
    public float flashDuration = 0.3f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount; // 최대 체력이 늘어난 만큼 현재 체력도 회복
        UpdateHPBar();
    }

    // UI 관련
    private GameObject canvasObj;
    private Image hpBarFill;
    private Text levelText;
    private Text hpText;
    private Text damageText;
    private Image skillIconBgQ;
    private Image skillIconBgE;
    private Image skillIconBgR;
    private Text skillKeyTextQ;
    private Text skillKeyTextE;
    private Text skillKeyTextR;

    private Attack attackScript;

    void Start()
    {
        attackScript = GetComponent<Attack>();
        if (attackScript == null) attackScript = GetComponentInParent<Attack>();

        // LevelManager 자동 생성 (없을 경우)
        if (LevelManager.Instance == null)
        {
            GameObject lm = new GameObject("LevelManager");
            lm.AddComponent<LevelManager>();
        }

        currentHP = maxHP;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        CreateHPBar();
        CreateSkillUI();
    }

    void Update()
    {
        UpdateSkillUI();
    }

    void UpdateSkillUI()
    {
        if (attackScript == null) return;

        // Q Cooltime UI
        if (skillKeyTextQ != null)
        {
            if (!attackScript.hasFireball)
            {
                skillKeyTextQ.text = ""; // 스킬 없음
            }
            else
            {
                float qCool = attackScript.FireballCooldownRemaining;
                if (qCool > 0)
                {
                    skillKeyTextQ.text = Mathf.CeilToInt(qCool).ToString();
                    skillKeyTextQ.color = Color.gray;
                }
                else
                {
                    skillKeyTextQ.text = "Q";
                    skillKeyTextQ.color = Color.white;
                }
            }
        }

        // E Cooltime UI
        if (skillKeyTextE != null)
        {
            if (!attackScript.hasSpin)
            {
                skillKeyTextE.text = ""; // 스킬 없음
            }
            else
            {
                float eCool = attackScript.SkillCooldownRemaining;
                if (eCool > 0)
                {
                    skillKeyTextE.text = Mathf.CeilToInt(eCool).ToString();
                    skillKeyTextE.color = Color.gray;
                }
                else
                {
                    skillKeyTextE.text = "E";
                    skillKeyTextE.color = Color.white;
                }
            }
        }

        // R Cooltime UI
        if (skillKeyTextR != null)
        {
            if (!attackScript.hasSwordWave)
            {
                skillKeyTextR.text = ""; // 스킬 없음
            }
            else
            {
                float rCool = attackScript.SwordWaveCooldownRemaining;
                if (rCool > 0)
                {
                    skillKeyTextR.text = Mathf.CeilToInt(rCool).ToString();
                    skillKeyTextR.color = Color.gray;
                }
                else
                {
                    skillKeyTextR.text = "R";
                    skillKeyTextR.color = Color.white;
                }
            }
        }
    }

    void CreateHPBar()
    {
        // Canvas 생성
        canvasObj = GameObject.Find("PlayerHPCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PlayerHPCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // HP바 배경 (어두운 회색)
        GameObject bgObj = new GameObject("HPBarBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(20, -20);
        bgRect.sizeDelta = new Vector2(300, 35); // 크기 증가 (250x25 -> 300x35)

        // HP바 채우기 (초록색)
        GameObject fillObj = new GameObject("HPBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        hpBarFill = fillObj.AddComponent<Image>();
        hpBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // HP 텍스트 (현재/최대)
        GameObject hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(bgObj.transform, false);
        hpText = hpTextObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 20;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.text = currentHP + " / " + maxHP;
        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;

        // 레벨 텍스트 생성
        GameObject textObj = new GameObject("LevelText");
        textObj.transform.SetParent(canvasObj.transform, false);
        levelText = textObj.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 26; // 폰트 크기 증가
        levelText.color = Color.white;
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.text = "Lv. 1";
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.pivot = new Vector2(0, 1);
        // HP바 오른쪽 끝(20 + 300)에서 조금 띄워서(15) 배치
        textRect.anchoredPosition = new Vector2(335, -20);
        textRect.sizeDelta = new Vector2(100, 35);

        // 공격력 텍스트 생성 (우측 상단)
        GameObject dmgTextObj = new GameObject("DamageText");
        dmgTextObj.transform.SetParent(canvasObj.transform, false);
        damageText = dmgTextObj.AddComponent<Text>();
        damageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        damageText.fontSize = 26; // 레벨과 같은 크기
        damageText.color = Color.white;
        damageText.alignment = TextAnchor.MiddleRight;
        damageText.text = "공격력 1";
        
        RectTransform dmgTextRect = dmgTextObj.GetComponent<RectTransform>();
        dmgTextRect.anchorMin = new Vector2(1, 1);
        dmgTextRect.anchorMax = new Vector2(1, 1);
        dmgTextRect.pivot = new Vector2(1, 1);
        dmgTextRect.anchoredPosition = new Vector2(-20, -20);
        dmgTextRect.sizeDelta = new Vector2(200, 35);
    }

    void CreateSkillUI()
    {
        if (canvasObj == null) return;

        // Q 스킬 슬롯 (왼쪽)
        GameObject qBgObj = new GameObject("SkillSlotQ");
        qBgObj.transform.SetParent(canvasObj.transform, false);
        skillIconBgQ = qBgObj.AddComponent<Image>();
        skillIconBgQ.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform qRect = qBgObj.GetComponent<RectTransform>();
        qRect.anchorMin = new Vector2(0, 0);
        qRect.anchorMax = new Vector2(0, 0);
        qRect.pivot = new Vector2(0, 0);
        qRect.anchoredPosition = new Vector2(20, 20);
        qRect.sizeDelta = new Vector2(60, 60);

        GameObject qTextObj = new GameObject("SkillKeyTextQ");
        qTextObj.transform.SetParent(qBgObj.transform, false);
        skillKeyTextQ = qTextObj.AddComponent<Text>();
        skillKeyTextQ.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skillKeyTextQ.fontSize = 24;
        skillKeyTextQ.color = Color.white;
        skillKeyTextQ.alignment = TextAnchor.MiddleCenter;
        skillKeyTextQ.text = "Q";
        
        RectTransform qTextRect = qTextObj.GetComponent<RectTransform>();
        qTextRect.anchorMin = Vector2.zero;
        qTextRect.anchorMax = Vector2.one;
        qTextRect.sizeDelta = Vector2.zero;

        // E 스킬 슬롯 (오른쪽으로 밀림)
        GameObject eBgObj = new GameObject("SkillSlotE");
        eBgObj.transform.SetParent(canvasObj.transform, false);
        skillIconBgE = eBgObj.AddComponent<Image>();
        skillIconBgE.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform eRect = eBgObj.GetComponent<RectTransform>();
        eRect.anchorMin = new Vector2(0, 0);
        eRect.anchorMax = new Vector2(0, 0);
        eRect.pivot = new Vector2(0, 0);
        eRect.anchoredPosition = new Vector2(90, 20); // 20 + 60 + 10 = 90
        eRect.sizeDelta = new Vector2(60, 60);

        GameObject eTextObj = new GameObject("SkillKeyTextE");
        eTextObj.transform.SetParent(eBgObj.transform, false);
        skillKeyTextE = eTextObj.AddComponent<Text>();
        skillKeyTextE.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skillKeyTextE.fontSize = 24;
        skillKeyTextE.color = Color.white;
        skillKeyTextE.alignment = TextAnchor.MiddleCenter;
        skillKeyTextE.text = "E";

        RectTransform eTextRect = eTextObj.GetComponent<RectTransform>();
        eTextRect.anchorMin = Vector2.zero;
        eTextRect.anchorMax = Vector2.one;
        eTextRect.sizeDelta = Vector2.zero;

        // R 스킬 슬롯 (E 옆에)
        GameObject rBgObj = new GameObject("SkillSlotR");
        rBgObj.transform.SetParent(canvasObj.transform, false);
        skillIconBgR = rBgObj.AddComponent<Image>();
        skillIconBgR.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform rRect = rBgObj.GetComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0, 0);
        rRect.anchorMax = new Vector2(0, 0);
        rRect.pivot = new Vector2(0, 0);
        rRect.anchoredPosition = new Vector2(160, 20); // 90 + 60 + 10 = 160
        rRect.sizeDelta = new Vector2(60, 60);

        GameObject rTextObj = new GameObject("SkillKeyTextR");
        rTextObj.transform.SetParent(rBgObj.transform, false);
        skillKeyTextR = rTextObj.AddComponent<Text>();
        skillKeyTextR.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skillKeyTextR.fontSize = 24;
        skillKeyTextR.color = Color.white;
        skillKeyTextR.alignment = TextAnchor.MiddleCenter;
        skillKeyTextR.text = "R";

        RectTransform rTextRect = rTextObj.GetComponent<RectTransform>();
        rTextRect.anchorMin = Vector2.zero;
        rTextRect.anchorMax = Vector2.one;
        rTextRect.sizeDelta = Vector2.zero;
    }

    public void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = "Lv. " + level;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        UpdateHPBar();

        if (!isFlashing)
        {
            StartCoroutine(FlashRed());
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 기존 TakeHit 호환 (적 접촉 시 데미지 1)
    public void TakeHit()
    {
        TakeDamage(1);
    }

    public void UpdateDamageText(int damage)
    {
        if (damageText != null)
        {
            damageText.text = "공격력 " + damage;
        }
    }

    void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(ratio, 1);
        }

        if (hpText != null)
        {
            hpText.text = currentHP + " / " + maxHP;
        }
    }

    IEnumerator FlashRed()
    {
        isFlashing = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
    }

    void Die()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.GameOver();
        }

        // 바로 파괴하지 않고 비활성화 (기타 로직이 이번 프레임에 작동할 수 있으므로)
        gameObject.SetActive(false);
        if (canvasObj != null) canvasObj.SetActive(false);
    }
}
