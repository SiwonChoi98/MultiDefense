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
    public event OnMoneyEventHandler OnMoneyUp;
    public event OnTimerEventHandler OnTimerUp;
    
    public List<Monster> Monsters = new();
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
    private void NotifyTimerClientRpc(float timer, int wave, bool getWaveUp)
    {
        Timer = timer;
        Wave = wave;

        if (getWaveUp)
        {
            bool getBossCheck = false;
            
            if (Wave % 10 == 0)
            {
                getBossCheck = true;
                Spawner.Instance.BossSpawn();
            }
            UI_Main.Instance.GetWavePopup(getBossCheck);
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
