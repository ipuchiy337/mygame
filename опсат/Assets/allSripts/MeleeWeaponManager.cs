using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponManager : MonoBehaviour
{
    [Header("Настройки")]
    public float orbitRadius = 1.5f; // расстояние от игрока

    private List<GameObject> _activeWeapons = new();
    private GameObject _currentPrefab;
    private MeleeWeapon _currentStats; // статы текущего оружия (для апгрейдов)

    // Вызвать когда игрок подбирает/меняет оружие
    public void EquipWeapon(GameObject weaponPrefab)
    {
        _currentPrefab = weaponPrefab;
        RefreshWeapons();
    }

    // Вызвать после апгрейда x2weapons у текущего оружия
    public void RefreshWeapons()
    {
        if (_currentPrefab == null) return;

        // Узнаём сколько копий нужно: 1 базовая + x2weapons апгрейды
        MeleeWeapon stats = _currentPrefab.GetComponent<MeleeWeapon>();
        int count = 1 + (_currentStats != null ? _currentStats.Get2xLvl() : 0);

        // Удаляем все старые
        foreach (GameObject go in _activeWeapons)
            Destroy(go);
        _activeWeapons.Clear();

        // Спавним новые равномерно по кругу
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.right * orbitRadius;

            GameObject weapon = Instantiate(_currentPrefab, transform.position + offset, Quaternion.identity);
            weapon.transform.SetParent(transform);

            // Копируем ссылку на статы чтобы апгрейды работали на все копии
            _currentStats = weapon.GetComponent<MeleeWeapon>();

            _activeWeapons.Add(weapon);
        }
    }

    // Получить stats активного оружия (для UI апгрейдов)
    public MeleeWeapon GetCurrentWeapon() => _currentStats;
}