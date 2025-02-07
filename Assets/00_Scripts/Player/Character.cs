using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    protected Animator _animator;
    protected SpriteRenderer _spriteRenderer;
    public virtual void Awake()
    {
        _animator = transform.GetChild(0).GetComponent<Animator>();
        _spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    public void OrderChange(int value)
    {
        _spriteRenderer.sortingOrder = value;
    }
    public void GetInitCharacter(string path)
    {
        _animator.runtimeAnimatorController = Resources.Load<Hero_Scriptable>("Character_Scriptable/" + path).Animator;
    }

    protected void AnimatorChange(string temp, bool trigger)
    {
        if (trigger)
        {
            _animator.SetTrigger(temp);
        }
        else
        {
            _animator.SetBool(temp, true);
        }
    }
}
