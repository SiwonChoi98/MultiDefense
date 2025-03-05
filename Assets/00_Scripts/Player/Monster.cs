using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Monster : Character
{
    public bool Boss;
    
    [SerializeField] private float m_Speed;
    private float originalSpeed;
    [SerializeField] private HitText _hitText;
    [SerializeField] private Image m_Fill, m_Fill_Deco;
    
    private int target_value = 0;
    public double HP = 0, MaxHp = 0;
    private bool _isDead = false;

    private List<Vector2> move_list = new();
    
    //슬로우
    private Coroutine slowCoroutine;
    [SerializeField] private Color slowColor;
    private float currentSlowAmount;
    private float currentSlowDuration;
    
    //스턴
    private Coroutine stunCoroutine;
    private bool isStun = false;
    [SerializeField] private GameObject stunPs;
    public override void Awake()
    {
        HP = CalculateMonsterHp(Game_Mng.Instance.Wave);
        MaxHp = HP;

        originalSpeed = m_Speed;
        base.Awake();
    }
    //지수적 증가 공식
    private double CalculateMonsterHp(int waveLevel)
    {
        double baseHp = 50.0f;

        double powerMultiplier = Mathf.Pow(1.1f, waveLevel);
        if (waveLevel % 10 == 0)
        {
            powerMultiplier += 0.05f * (waveLevel / 10);
        }

        return baseHp * powerMultiplier * (Boss ? 10 : 1);
    }

    public void Init(List<Vector2> vectorList)
    {
        move_list = vectorList;
    }
    private void Update()
    {
        m_Fill_Deco.fillAmount = Mathf.Lerp(m_Fill_Deco.fillAmount, m_Fill.fillAmount, Time.deltaTime * 2.0f);
        
        if (_isDead) return;
        if (isStun) return;
        transform.position = Vector2.MoveTowards(transform.position, move_list[target_value], Time.deltaTime * m_Speed);
        if (Vector2.Distance(transform.position, move_list[target_value]) <= 0.0f)
        {
            target_value++;
    
            _spriteRenderer.flipX = target_value > 2 ? true : false;
            
            if (target_value >= 4)
            {
                target_value = 0;
            }
        }
    }

    public void GetDamage(double dmg)
    {
        if (!IsServer) return;
        if (_isDead) return;
        
        GetDamageMonster(dmg);
        NotifyClientUpdateClientRpc(HP -= dmg, dmg);
    }

    private void GetDamageMonster(double dmg)
    {
        HP -= dmg;
        m_Fill.fillAmount = (float)(HP /MaxHp);
        Instantiate(_hitText, transform.position, Quaternion.identity).Initalize(dmg);
        
        if (HP <= 0)
        {
            _isDead = true;
            Game_Mng.Instance.GetMoney(1);
            // Game_Mng.Instance.RemoveMonster(this);
            gameObject.layer = LayerMask.NameToLayer("Default");
            StartCoroutine(Dead_Coroutine());
            AnimatorChange("DoDead", true);
        }
    }
    [ClientRpc]
    public void NotifyClientUpdateClientRpc(double hp, double dmg)
    {
        HP = hp;
        m_Fill.fillAmount = (float)(HP / MaxHp);
        Instantiate(_hitText, transform.position, Quaternion.identity).Initalize(dmg);
        
        if (HP <= 0)
        {
            _isDead = true;
            // Game_Mng.Instance.GetMoney(1);
            // Game_Mng.Instance.RemoveMonster(this);
            gameObject.layer = LayerMask.NameToLayer("Default");
            StartCoroutine(Dead_Coroutine());
            AnimatorChange("DoDead", true);
        }
    }

    private IEnumerator Dead_Coroutine()
    {
        float Alpha = 1.0f;
        while (_spriteRenderer.color.a > 0.0f)
        {
            Alpha -= Time.deltaTime;
            _spriteRenderer.color = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b,
                Alpha);

            yield return null;
        }

        if (IsServer)
        {
            Game_Mng.Instance.RemoveMonster(this, Boss);
            this.gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ApplyDebuffServerRpc(int debuffType, float[] values)
    {
        DebuffType debuff = (DebuffType)debuffType;
        switch (debuff)
        {
            case DebuffType.Slow:
                if (values[0] > currentSlowAmount ||
                    (values[0] == currentSlowAmount && values[1] > currentSlowDuration))
                {
                    currentSlowAmount = values[0];
                    currentSlowDuration = values[1];

                    ApplySlowClientRpc(values[0], values[1]);
                }
                break;
            case DebuffType.Stun:
                ApplyStunClientRpc(values[0]);
                break;
        }
    }
    //스턴
    [ClientRpc]
    private void ApplyStunClientRpc(float stunDuration)
    {
        CoroutineStop(stunCoroutine);

        stunCoroutine = StartCoroutine(EffectCoroutine(stunDuration, () =>
        {
            isStun = true;
            stunPs.SetActive(true);
        }, () =>
        {
            isStun = false;
            stunPs.SetActive(false);
        }));
    }

    //슬로우
    [ClientRpc]
    private void ApplySlowClientRpc(float slowAmount, float duration)
    {
        CoroutineStop(slowCoroutine);
        
        slowCoroutine = StartCoroutine(EffectCoroutine(duration, () =>
        {
            float newSpeed = originalSpeed - (originalSpeed * slowAmount);
            newSpeed = Mathf.Max(newSpeed, 0.1f); //0.1f 보다 아래로 가는거 방지

            m_Speed = newSpeed;
            _spriteRenderer.color = slowColor;
        }, () =>
        {
            m_Speed = originalSpeed;
            _spriteRenderer.color = Color.white;
        }));
    }

    private void CoroutineStop(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }
    private IEnumerator EffectCoroutine(float duration, Action FirstAction, Action SecondAction)
    {
        FirstAction?.Invoke();
        
        yield return new WaitForSeconds(duration);

        SecondAction?.Invoke();
        
    }

    // [ServerRpc(RequireOwnership = false)]
    // private void RequestDestroyMonsterServerRpc()
    // {
    //     DestoryMonster();
    // }
    //
    // private void DestoryMonster()
    // {
    //     Destroy(this);
    // }
    //
    
}
