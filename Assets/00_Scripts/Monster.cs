using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Monster : Character
{
    private int target_value = 0;
    [SerializeField] private float m_Speed;
    
    public override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, Character_Spawner._move_List[target_value], Time.deltaTime * m_Speed);
        if (Vector2.Distance(transform.position, Character_Spawner._move_List[target_value]) <= 0.0f)
        {
            target_value++;

            _spriteRenderer.flipX = target_value > 2 ? true : false;
            
            if (target_value >= 4)
            {
                target_value = 0;
            }
        }
    }
}
