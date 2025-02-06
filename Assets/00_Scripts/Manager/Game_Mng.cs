using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public delegate void OnMoneyEventHandler();
public class Game_Mng : NetworkBehaviour
{
    public static Game_Mng Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public int Money;
    public int SummonCount;
    public int MonsterCount;
    public event OnMoneyEventHandler OnMoneyUp;
    public List<Monster> Monsters = new();
    public void GetMoney(int value)
    {
        Money += value;
        OnMoneyUp?.Invoke();
    }
    public void AddMonster(Monster monster)
    {
        Monsters.Add(monster);
        MonsterCount++;
        UpdateMonsterCountOnClients();
    }

    public void RemoveMonster(Monster monster)
    {
        Monsters.Remove(monster);
        MonsterCount--;
        UpdateMonsterCountOnClients();
    }

    private void UpdateMonsterCountOnClients()
    {
        NotifyClientMonsterCountClientRpc(MonsterCount);
    }
    [ClientRpc]
    private void NotifyClientMonsterCountClientRpc(int count)
    {
        MonsterCount = count;
    }
}
