using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hero : Character
{
    private Hero_Holder parent_Holder;
    public int ATK;
    public float AttackRange = 1.0f;
    public float AttackSpeed = 1.0f;
    public NetworkObject Target;
    public LayerMask EnemyMask;
    public ScriptableObject m_Data;

    public void Initialize(HeroData obj, Hero_Holder heroHolder)
    {
        parent_Holder = heroHolder;
        ATK = obj.heroATK;
        AttackRange = obj.heroRange;
        AttackSpeed = obj.heroATK_Speed;

        GetInitCharacter(obj.heroName);
    }
    private void Update()
    {
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
                AttackMonsterServerRpc(Target.NetworkObjectId);
            }
        }
        else
        {
            Target = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttackMonsterServerRpc(ulong monsterId)
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
