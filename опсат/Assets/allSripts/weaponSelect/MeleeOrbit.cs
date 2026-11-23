using UnityEngine;
using System.Collections.Generic;

public class MeleeOrbit : MonoBehaviour
{
    // ссылка на главный нож Ч берЄм его статы
    public MeleeWeapon source;

    private float _cooldown;
    private readonly HashSet<Collider> _hitThisSwing = new();

    void Update()
    {
        if (source == null) return;

        transform.RotateAround(
            transform.parent.position,
            Vector3.up,
            source.rotationSpeed * 360f * Time.deltaTime
        );

        _cooldown -= Time.deltaTime;
        if (_cooldown <= 0f)
        {
            _hitThisSwing.Clear();
            _cooldown = 1f / source.rotationSpeed;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, source.hitRadius);
        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Zombie")) continue;
            if (_hitThisSwing.Contains(col)) continue;

            Health hp = col.GetComponent<Health>();
            if (hp == null) continue;

            _hitThisSwing.Add(col);
            hp.TakeDamage(source.damage);
        }
    }
}