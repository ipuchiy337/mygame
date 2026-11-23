using UnityEngine;

/// Точка появления зомби. Ставится генератором карты в основной проходимой области — той же, где появляется игрок.
/// Найти все точки: MapGenerator.ZombieSpawns или FindObjectsByType<ZombieSpawnPoint>().
public class ZombieSpawnPoint : MonoBehaviour
{
    [Tooltip("Гнездо — место у кладбища, костей или руин, где зомби логично появляться чаще")]
    public bool isNest;

    void OnDrawGizmos()
    {
        Gizmos.color = isNest ? new Color(1f, 0.25f, 0.1f) : new Color(0.75f, 0.15f, 0.6f);
        var p = transform.position;
        Gizmos.DrawWireSphere(p + Vector3.up * 0.5f, isNest ? 1.5f : 1f);
        Gizmos.DrawLine(p, p + Vector3.up * 3f);
    }
}
