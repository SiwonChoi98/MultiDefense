using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hero : Character
{
    public float AttackRange = 1.0f;
    public float AttackSpeed = 1.0f;
    public NetworkObject Target;
    public LayerMask EnemyMask;

    private void Update()
    {
        CheckForEnemies();
    }

    private void CheckForEnemies()
    {
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, AttackRange, EnemyMask);
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
                monster.GetDamage(10);
            }
        }
    }
    // private void AttackEnemy(Monster enemy)
    // {
    //     AnimatorChange("DoAttack", true);
    //     enemy.GetDamage(10);
    // }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
