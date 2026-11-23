using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// Единый «мозг» орды. Зомби (ZombieFollow) — пустышки с настройками и состоянием, своих Update/FixedUpdate
// у них нет: менеджер раз в кадр и раз в физтик проходит по всем. Разнообразие зомби (скорость, урон,
// прыжки, карабканье, гранатомётчик, призыватель) живёт в настройках и компонентах каждого шаблона.
// Создаётся сам при появлении первого зомби — на сцену ставить не нужно.
public class ZombieHorde : MonoBehaviour
{
    static ZombieHorde instance;

    readonly List<ZombieFollow> grounding = new List<ZombieFollow>(1024);
    NativeArray<RaycastCommand> commands;
    NativeArray<RaycastHit> hits;

    public static void Ensure()
    {
        if (instance == null) instance = new GameObject("ZombieHorde").AddComponent<ZombieHorde>();
    }

    void Update()
    {
        var all = ZombieFollow.All;
        for (int i = all.Count - 1; i >= 0; i--)
            if (i < all.Count) all[i].Tick(); // список может укоротиться, если кто-то умер по ходу
    }

    void FixedUpdate()
    {
        var all = ZombieFollow.All;

        // 1) Лучи «есть ли земля» всей орды — одной пачкой в фоновых потоках вместо сотен Physics.Raycast подряд.
        Reserve(all.Count);
        grounding.Clear();
        for (int i = 0; i < all.Count; i++)
            if (all[i].PrepareGroundCheck(out RaycastCommand cmd))
            {
                commands[grounding.Count] = cmd;
                grounding.Add(all[i]);
            }
        int n = grounding.Count;
        if (n > 0)
        {
            RaycastCommand.ScheduleBatch(commands.GetSubArray(0, n), hits.GetSubArray(0, n), 32).Complete();
            for (int i = 0; i < n; i++) grounding[i].ApplyGround(hits[i]);
        }

        // 2) Движение, обход, прыжки, карабканье.
        for (int i = all.Count - 1; i >= 0; i--)
            if (i < all.Count) all[i].FixedTick();
    }

    void Reserve(int count)
    {
        if (commands.IsCreated && commands.Length >= count) return;
        Release();
        int size = Mathf.NextPowerOfTwo(Mathf.Max(count, 256));
        commands = new NativeArray<RaycastCommand>(size, Allocator.Persistent);
        hits = new NativeArray<RaycastHit>(size, Allocator.Persistent);
    }

    void Release()
    {
        if (commands.IsCreated) commands.Dispose();
        if (hits.IsCreated) hits.Dispose();
    }

    void OnDestroy()
    {
        Release();
        if (instance == this) instance = null;
    }
}
