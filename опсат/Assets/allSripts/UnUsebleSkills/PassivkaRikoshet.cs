using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassivkaRikoshet : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public List<GunWithTracer> guns;

    public int Rikochet = 1;
    public float shans = 4f;


    public void AddRick()
    {
        pasMan.plusDmg += 1;

        // Находим ВСЕ GunWithTracer, даже выключенные
        GunWithTracer[] allGuns = Resources.FindObjectsOfTypeAll<GunWithTracer>();

        Debug.Log($"Найдено оружия: {allGuns.Length}");

        foreach (var gun in allGuns)
        {
            // Проверяем что объект на сцене (не префаб в папке)
            if (gun != null && gun.gameObject.scene.isLoaded)
            {
                gun.currentRicochets += Rikochet;
                gun.currentRicochetChance += shans;

                Debug.Log($"Обновлено: {gun.gameObject.name}, Рикошеты: {gun.currentRicochets}, Шанс: {gun.currentRicochetChance}%");
            }
        }
    }
}
