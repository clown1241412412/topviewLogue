using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public Transform weapon;
    public float swingDuration = 0.2f;
    public float swingAngle = 120f; // Total arc angle
    public float cooldown = 0.1f;
    public float guardAngle = 90f;
    public int attackDamage = 1;

    public void IncreaseDamage(int amount)
    {
        attackDamage += amount;
        if (weaponController != null)
        {
            weaponController.damage = attackDamage;
        }
        
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.UpdateDamageText(attackDamage);
        }
    }

    private bool isAttacking = false;
    private bool isGuarding = false;
    public bool IsGuarding { get { return isGuarding; } } // 외부에서 접근 가능하도록 프로퍼티 추가
    private Quaternion initialRotation;
    private WeaponController weaponController;

    void Start()
    {
        if (weapon == null)
        {
            // "word" 또는 "Rarm" 등 무기 부모 찾기 시도
            weapon = transform.Find("word");
            if (weapon == null) weapon = transform.Find("Rarm");
        }

        if (weapon != null)
        {
            initialRotation = weapon.localRotation;
            
            // 1. "sword" 이름으로 찾기
            Transform sword = weapon.Find("sword");
            // 2. 없으면 첫 번째 자식을 무기로 간주 (Square 등일 수 있음)
            if (sword == null && weapon.childCount > 0) 
            {
                sword = weapon.GetChild(0);
                // Debug.Log("sword를 이름으로 못 찾아서 첫 번째 자식(" + sword.name + ")을 사용합니다.");
            }

            if (sword != null)
            {
                weaponController = sword.GetComponent<WeaponController>();
                if (weaponController == null)
                {
                    weaponController = sword.gameObject.AddComponent<WeaponController>();
                    // Debug.Log("WeaponController가 없어서 자동으로 추가했습니다.");
                }
                weaponController.damage = attackDamage;
            }

            // UI 초기화
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health == null) health = GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.UpdateDamageText(attackDamage);
            }
            else
            {
                // Debug.LogError("무기에 자식 오브젝트(칼날)가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Weapon transform을 찾을 수 없습니다! 'word' 또는 'Rarm' 자식이 있는지 확인하세요.");
        }
    }

    void Update()
    {
        if (weapon == null) return;

        bool rightPressed = Mouse.current.rightButton.isPressed;

        if (rightPressed && !isAttacking)
        {
            isGuarding = true;
            weapon.localRotation = initialRotation * Quaternion.Euler(0, 0, guardAngle);
        }
        else if (!rightPressed && isGuarding)
        {
            isGuarding = false;
            weapon.localRotation = initialRotation;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking && !isGuarding && weapon != null)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        if (weaponController != null) weaponController.StartAttack(); // 공격 판정 켜기

        Quaternion startRot = initialRotation * Quaternion.Euler(0, 0, swingAngle / 2f);
        Quaternion endRot = initialRotation * Quaternion.Euler(0, 0, -swingAngle / 2f);

        float elapsedTime = 0f;

        while (elapsedTime < swingDuration)
        {
            float t = elapsedTime / swingDuration;
            t = t * t * (3f - 2f * t);

            weapon.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        weapon.localRotation = endRot;
        yield return null;

        if (weaponController != null) weaponController.EndAttack(); // 공격 판정 끄기

        weapon.localRotation = initialRotation;
        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }


}
