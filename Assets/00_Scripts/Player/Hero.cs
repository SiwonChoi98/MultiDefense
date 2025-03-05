using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Hero : Character
{
    private Hero_Holder parent_Holder;

    private double baseATK;
    public double ATK 
    {
        get
        {
            float upgradeBonus = Game_Mng.Instance.Upgrade[UpgradeCount()] != 0 ? Game_Mng.Instance.Upgrade[UpgradeCount()] * 0.1f : 0;
            return baseATK + (1 + upgradeBonus);
        }
        //set => baseATK = Math.Max(0, value);
    }
    public float AttackRange = 1.0f;
    public float AttackSpeed = 1.0f;
    public NetworkObject Target;
    public LayerMask EnemyMask;
    public Hero_Scriptable m_Data;
    
    private bool isMove = false;

    public string HeroName;
    public Rarity HeroRarity;
    public Color[] colors;
    public SpriteRenderer circleRanderer;
    [SerializeField] private GameObject SpawnParticle;

    //슬로우 
    private float slowChance = 0.5f;
    private float slowAmount = 0.3f;
    private float slowDuration = 2.0f;
    
    private int UpgradeCount()
    {
        switch (m_Data.rare)
        {
            case Rarity.Common:
            case Rarity.UnCommon:
            case Rarity.Rare:
                return 0;
            case Rarity.Hero:
                return 1;
            case Rarity.Legendar:
                return 2;
        }

        return -1;
    }
    public void Initialize(HeroData obj, Hero_Holder heroHolder, string rarity)
    {
        m_Data = Resources.Load<Hero_Scriptable>("Character_Scriptable/" + rarity + "/" +obj.heroName);
        
        parent_Holder = heroHolder;
        baseATK = obj.heroATK;
        AttackRange = obj.heroRange;
        AttackSpeed = obj.heroATK_Speed;

        HeroName = obj.heroName;
        HeroRarity = (Rarity)Enum.Parse(typeof(Rarity), rarity);
        
        circleRanderer.color = colors[(int)HeroRarity];
        
        GetInitCharacter(obj.heroName, rarity);

        Instantiate(SpawnParticle, parent_Holder.transform.position, Quaternion.identity);
    }

    public void Position_Change(Hero_Holder holder, List<Vector2> poss, int myIndex)
    {
        isMove = true;
        AnimatorChange("IsMove", false);

        parent_Holder = holder;
        
        if (IsServer)
        {
            transform.parent = holder.transform;
        }
        

        //음수면 -1 양수면 +1 반환
        int sign = (int)Mathf.Sign(poss[myIndex].x - transform.position.x);

        switch (sign)
        {
            case -1:
                _spriteRenderer.flipX = true;
                break;
            case 1:
                _spriteRenderer.flipX = false;
                break;
        }
        StartCoroutine(Move_Coroutine(poss[myIndex]));
    }

    private IEnumerator Move_Coroutine(Vector2 endPos)
    {
        float current = 0.0f;
        float percent = 0.0f;

        Vector2 start = transform.position;
        Vector2 end = endPos;
        while (percent < 1.0f)
        {
            current += Time.deltaTime;
            percent = current / 0.5f;
            Vector2 lerpPos = Vector2.Lerp(start, end, percent);
            transform.position = lerpPos;

            yield return null;
        }
        isMove = false;
        AnimatorChange("IsIdle", false);
        _spriteRenderer.flipX = true;
    }
    private void Update()
    {
        if (isMove)
            return;
        CheckForEnemies();
    }

    private void CheckForEnemies()
    {
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(parent_Holder.transform.position, AttackRange, EnemyMask);
        AttackSpeed += Time.deltaTime;
        
        if (enemiesInRange.Length > 0)
        {
            Target = enemiesInRange[0].GetComponent<NetworkObject>();
            if (AttackSpeed >= 1.0f)
            {
                AttackSpeed = 0.0f;
                AnimatorChange("DoAttack", true);
                //AttackMonsterServerRpc(Target.NetworkObjectId);
                GetBullet();
            }
        }
        else
        {
            Target = null;
        }
    }

    public void GetBullet()
    {
        var go = Instantiate(m_Data.bullet, transform.position + new Vector3(0.0f, 0.1f), Quaternion.identity);
        go.Init(Target.transform, this);
    }

    public void SetDamage()
    {
        if (Target != null)
        {
            AttackMonsterServerRpc(Target.NetworkObjectId);
            if (Random.value <= slowChance)
            {
                Target.GetComponent<Monster>().ApplySlowServerRpc(slowAmount, slowDuration);
            }
        }
            
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AttackMonsterServerRpc(ulong monsterId)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(monsterId,
                out var spawnObject))
        {
            Monster monster = spawnObject.GetComponent<Monster>();
            if (monster != null)
            {
                monster.GetDamage(ATK);
            }
        }
    }
    
    
    // private void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireSphere(parent_Holder.transform.position, AttackRange);
    // }
}
