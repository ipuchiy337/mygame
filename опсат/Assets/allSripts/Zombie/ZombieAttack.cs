using UnityEngine;
using System.Collections.Generic;
public class ZombieAttack : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float damage = 10f;
    public float attackCooldown = 1f;

    private float lastAttackTime = 0f;

    public Transform player;
    public Animator anim;

    public AudioSource AttackZ;
    public AudioClip attackClip;
    public bool attaked;
    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            
            TryAttack();
            attaked = true;
            anim.SetTrigger("Attack");
        }
        else
        {
            attaked = false;
        }
       
    }

    public void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // ÂÛÁÈÐÀÅÌ ÑËÓ×ÀÉÍÓÞ ÀÒÀÊÓ
        // anim.SetInteger("AttackType", Random.Range(0, 2));

        // ÇÀÏÓÑÊ ÀÍÈÌÀÖÈÈ
        AttackZ.PlayOneShot(attackClip);

        // ÍÀÍÎÑÈÌ ÓÐÎÍ
        Health hp = player.GetComponent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
           
        }
          

    }
}
