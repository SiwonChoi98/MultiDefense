using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Monster : Character
{
    [SerializeField] private float m_Speed;
    [SerializeField] private HitText _hitText;
    [SerializeField] private Image m_Fill, m_Fill_Deco;
    
    private int target_value = 0;
    public double HP = 0, MaxHp = 0;
    private bool _isDead = false;

    private List<Vector2> move_list = new();
    public override void Awake()
    {
        HP = CalculateMonsterHp(Game_Mng.Instance.Wave);
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

        return baseHp * powerMultiplier;
    }

    public void Init(List<Vector2> vectorList)
    {
        move_list = vectorList;
    }
    private void Update()
    {
        m_Fill_Deco.fillAmount = Mathf.Lerp(m_Fill_Deco.fillAmount, m_Fill.fillAmount, Time.deltaTime * 2.0f);
        
        if (_isDead) return;
        
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
            Game_Mng.Instance.RemoveMonster(this);
            this.gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
        
        // else if (IsClient)
        // {
        //     RequestDestroyMonsterServerRpc();
        // }
        
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDestroyMonsterServerRpc()
    {
        DestoryMonster();
    }

    private void DestoryMonster()
    {
        Destroy(this);
    }
}
