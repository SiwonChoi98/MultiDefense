using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Character_Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _spawnPrefab;
    [SerializeField] private Monster _spawn_Monster_Prefab;

    public static List<Vector2> _move_List = new List<Vector2>();
    private List<Vector2> _spawn_List = new List<Vector2>();
    private List<bool> _spawn_List_Array = new List<bool>();
    
    private void Start()
    {
        Grid_Start();

        for (int i = 0; i < transform.childCount; i++)
        {
            _move_List.Add(transform.GetChild(i).position);
        }

        StartCoroutine(Spawn_Monster_Coroutine());
    }

    #region Make_Grid

    private void Grid_Start()
    {
        SpriteRenderer parentSprite = GetComponent<SpriteRenderer>();
        float parentWidth = parentSprite.bounds.size.x;
        float parentHeight = parentSprite.bounds.size.y;
        
        float xCount = transform.localScale.x / 6;
        float yCount = transform.localScale.y / 3;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                float xPos = (-parentWidth / 2) + (col * xCount) + (xCount / 2);
                float yPos = (parentHeight / 2) - (row * yCount) + (yCount / 2);

                _spawn_List.Add(new Vector2(xPos, yPos + transform.position.y - yCount));
                _spawn_List_Array.Add(false);
            }
        }
    }

    #endregion
    
    #region 캐릭터 소환

    public void Summon()
    {
        if (Game_Mng.Instance.Money < Game_Mng.Instance.SummonCount)
            return;

        Game_Mng.Instance.Money -= Game_Mng.Instance.SummonCount;
        Game_Mng.Instance.SummonCount += 2;
        
        int position_value = -1;
        var go = Instantiate(_spawnPrefab);
        for (int i = 0; i < _spawn_List_Array.Count; i++)
        {
            if (_spawn_List_Array[i] == false)
            {
                position_value = i;
                _spawn_List_Array[i] = true;
                break;
            }
        }

        go.transform.position = _spawn_List[position_value];
    }

    #endregion
    
    #region 몬스터 소환

    private IEnumerator Spawn_Monster_Coroutine()
    {
        var go = Instantiate(_spawn_Monster_Prefab, _move_List[0], Quaternion.identity);
        Game_Mng.Instance.AddMonster(go);
        
        yield return new WaitForSeconds(0.7f);

        StartCoroutine(Spawn_Monster_Coroutine());
    }

    #endregion
}
