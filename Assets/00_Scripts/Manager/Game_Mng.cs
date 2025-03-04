using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public delegate void OnMoneyEventHandler();
public delegate void OnTimerEventHandler();
public class Game_Mng : NetworkBehaviour
{
    public static Game_Mng Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        b_data = Resources.Load<Boss_Scriptable>("Boss/Boss_Scriptable");
    }

    public float Timer = 60.0f;
    public int Wave = 1;
    
    public int HeroCount;
    public int HeroMaximumCount = 25;
    public int[] Upgrade = new int[4];
    
    public int Money;
    public int SummonCount;
    public int MonsterCount;

    public bool GetBoss = false;
    public event OnMoneyEventHandler OnMoneyUp;
    public event OnTimerEventHandler OnTimerUp;
    
    public List<Monster> Monsters = new();
    public List<Monster> Boss_Monsters = new();
    public Boss_Scriptable b_data;
    private void Update()
    {
        if (IsServer)
        {
            bool GetWaveUp = false;
            if (Timer > 0)
            {
                Timer -= Time.deltaTime;
                Timer = Mathf.Max(Timer, 0); //음수 방지
            }
            else
            {
                if (GetBoss)
                {
                    Debug.Log("게임 실패");
                    return;
                }
                Wave++;
                GetWaveUp = true;
                Timer = 60f;
            }
            NotifyTimerClientRpc(Timer, Wave, GetWaveUp);
        }
    }
    
    public void GetMoney(int value, HostType hostType = HostType.All)
    {
        if (hostType == HostType.All)
        {
            NotifyGetMoneyClientRpc(value);
        }
    }
    public void AddMonster(Monster monster, bool boss = false)
    {
        if (boss)
        {
            Boss_Monsters.Add(monster);
        }
        else
        {
            Monsters.Add(monster);
        }

        MonsterCount++;
        UpdateMonsterCountOnClients();
    }

    public void RemoveMonster(Monster monster, bool boss = false)
    {
        if (boss)
        {
            Boss_Monsters.Remove(monster);
            if (Boss_Monsters.Count == 0)
            {
                GetBoss = false;
                Timer = 0.0f;
            }
        }
        else
        {
            Monsters.Remove(monster);
        }
        
        MonsterCount--;
        UpdateMonsterCountOnClients();
    }

    private void UpdateMonsterCountOnClients()
    {
        NotifyClientMonsterCountClientRpc(MonsterCount);
    }
    
    [ClientRpc]
    private void NotifyTimerClientRpc(float timer, int wave, bool getWaveUp)
    {
        Timer = timer;
        Wave = wave;

        if (getWaveUp)
        {
            GetBoss = false;
            
            if (Wave % 10 == 0)
            {
                GetBoss = true;
                Spawner.Instance.BossSpawn();
            }
            else
            {
                Spawner.Instance.ReMonsterSpawn();
            }
            
            UI_Main.Instance.GetWavePopup(GetBoss);
        }
        
        OnTimerUp?.Invoke();
    }

    [ClientRpc]
    private void NotifyGetMoneyClientRpc(int value)
    {
        Money += value;
        OnMoneyUp?.Invoke();
    }
    [ClientRpc]
    private void NotifyClientMonsterCountClientRpc(int count)
    {
        MonsterCount = count;
    }
}
