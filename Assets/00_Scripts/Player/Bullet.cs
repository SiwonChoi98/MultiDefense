using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float m_Speed;
    [SerializeField] private GameObject distroy_ps;
    
    private Transform target;
    private Hero parent_Hero;

    public void Init(Transform t, Hero hero)
    {
        Debug.Log("Init Bullet");
        target = t;
        parent_Hero = hero;
    }

    private void Update()
    {
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > 0.03f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, m_Speed * Time.deltaTime);
        }
        else if (distance <= 0.03f)
        {
            Instantiate(distroy_ps, transform.position, Quaternion.identity);
            parent_Hero.SetDamage();
            Destroy(this.gameObject);
        }
    }
}
