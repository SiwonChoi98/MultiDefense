using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : Character
{
    public float AttackRange = 1.0f;
    public float AttackSpeed = 1.0f;
    public Monster Target;
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
            Target = enemiesInRange[0].GetComponent<Monster>();
            if (AttackSpeed >= 1.0f)
            {
                AttackSpeed = 0.0f;
                AttackEnemy(Target);
            }
        }
        else
        {
            Target = null;
        }
    }
    
    private void AttackEnemy(Monster enemy)
    {
        AnimatorChange("DoAttack", true);
        enemy.GetDamage(10);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
