using System.Collections;
using System.Collections.Generic;
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
    public int HP = 0, MaxHp = 0;
    private bool _isDead = false;
    public override void Start()
    {
        HP = MaxHp;
        base.Start();
    }

    private void Update()
    {
        m_Fill_Deco.fillAmount = Mathf.Lerp(m_Fill_Deco.fillAmount, m_Fill.fillAmount, Time.deltaTime * 2.0f);
        
        if (_isDead) return;
        
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

    public void GetDamage(int dmg)
    {
        if (_isDead) return;
        
        HP -= dmg;
        m_Fill.fillAmount = (float) HP / MaxHp;
        Instantiate(_hitText, transform.position, Quaternion.identity).Initalize(dmg);
        if (HP <= 0)
        {
            _isDead = true;
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
        
        Destroy(gameObject);
    }
}
