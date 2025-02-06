using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    protected Animator _animator;
    protected SpriteRenderer _spriteRenderer;
    public virtual void Start()
    {
        _animator = transform.GetChild(0).GetComponent<Animator>();
        _spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
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
