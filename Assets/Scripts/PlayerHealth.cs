using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public float flashDuration = 0.3f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    // UI 관련
    private GameObject canvasObj;
    private Image hpBarFill;

    void Start()
    {
        currentHP = maxHP;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        CreateHPBar();
    }

    void CreateHPBar()
    {
        // Canvas 생성
        canvasObj = new GameObject("PlayerHPCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();

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
        bgRect.sizeDelta = new Vector2(250, 25);

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

    void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(ratio, 1);
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
        if (canvasObj != null) Destroy(canvasObj);
        Destroy(gameObject);
    }
}
