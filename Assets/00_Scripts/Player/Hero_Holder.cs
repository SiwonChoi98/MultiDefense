using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Unity.Android.Gradle.Manifest;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Host에서만 동작하는 객체
/// </summary>
public class Hero_Holder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    [SerializeField] private Transform Circle_Range;
    [SerializeField] private Transform SetClick;
    [SerializeField] private Transform GetClick;
    [SerializeField] private GameObject CanvasObject;
    
    public string Holder_Part_Name;
    public string Holder_Name;
    public List<Hero> m_Heros = new();
    
    public int index;
    public HeroData m_Data;
    
    public readonly Vector2[] One = { Vector2.zero };
    public readonly Vector2[] Two =
    {
        new Vector2(-0.1f, 0.05f), 
        new Vector2(0.1f, -0.1f)
    };
    public readonly Vector2[] Three =
    {
        new Vector2(-0.1f, 0.1f),
        new Vector2(0.1f, -0.05f), 
        new Vector2(-0.15f, -0.15f)
    };

    public Button SellButton, CompositionButton;
    
    private void Start()
    {
        MakeCollider();
        
        SellButton.onClick.AddListener(() => Sell());
        CompositionButton.onClick.AddListener(() => Composition());
    }

    #region  캐릭터 판매

    private void Sell(bool getNavigation = true)
    {
        if (getNavigation)

        {
            UI_Main.Instance.GetNavigation(string.Format("영웅을 판매하였습니다. {0}{1}", 
                Net_Utils.RarityColor(m_Heros[0].HeroRarity), m_Heros[0].HeroName)); 
        }
        
        
        Net_Utils.HostAndClientMethod(
            () => SellServerRpc(Net_Utils.LocalID()),
            () => SellCharacter(Net_Utils.LocalID()));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SellServerRpc(ulong clientId)
    {
        SellCharacter(clientId);
    }

    private void SellCharacter(ulong clientId)
    {
        var hero = m_Heros[m_Heros.Count - 1];
        ulong heroId = hero.NetworkObjectId;
        NetworkObject obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[heroId];
        SellClientRpc(heroId, clientId);
        obj.Despawn();
    }

    [ClientRpc]
    private void SellClientRpc(ulong heroKey, ulong clientId)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[heroKey];
        m_Heros.Remove(obj.GetComponent<Hero>());

        if (m_Heros.Count == 0)
        {
            DestroyServerRpc(clientId);
        }
        CheckGetPosition();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyServerRpc(ulong clientId)
    {
        DestroyClientRpc(clientId);
        
        NetworkObject holderObj = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects[NetworkObjectId];
        holderObj.Despawn();
    }

    [ClientRpc]
    private void DestroyClientRpc(ulong clientId)
    {
        Spawner.Instance.Hero_Holders.Remove(Holder_Part_Name);
        if (Net_Utils.IsClientCheck(clientId))
        {
            Spawner.Player_spawn_List_Array[index] = false;
        }
        else
        {
            Spawner.Other_spawn_List_Array[index] = false;
        }
    }

    #endregion
    
    public void Composition()
    {
        List<Hero_Holder> heroHolders = new();
        
        heroHolders.Add(this);
        
        foreach (var holderData in Spawner.Instance.Hero_Holders)
        {
            if (holderData.Value.Holder_Name == Holder_Name && holderData.Value != this)
            {
                string temp = Net_Utils.LocalID() == (ulong)0 ? "HOST" : "CLIENT";
                if(holderData.Value.Holder_Part_Name.Contains(temp))
                {
                    heroHolders.Add(holderData.Value);
                }
                
            }
        }

        int cnt = 0;
        string[] holderTemp = new string[2];
        bool getBreak = false;
        for (int i = 0; i < heroHolders.Count; i++)
        {
            for (int j = 0; j < heroHolders[i].m_Heros.Count; j++)
            {
                if(heroHolders[i].m_Heros.Count > 0)
                {
                    holderTemp[cnt] = heroHolders[i].Holder_Part_Name;
                    cnt++;
                    if (cnt >= 2)
                    {
                        getBreak = true;
                        break;
                    }
                }
            }

            if (getBreak) break;
        }

        for (int i = 0; i < holderTemp.Length; i++)
        {
            if (holderTemp[i] == "" || holderTemp[i] == null)
            {
                Debug.Log("합성에 필요한 영웅이 부족합니다.");
                return;
            }
        }

        for (int i = 0; i < holderTemp.Length; i++)
        {
            Spawner.Instance.Hero_Holders[holderTemp[i]].Sell(false);
        }
        
        Spawner.Instance.Summon("UnCommon");
    
    }
    public void HeroChange(Hero_Holder holder)
    {
        List<Vector2> poss = new List<Vector2>();
        switch (m_Heros.Count)
        {
            case 1: poss = new List<Vector2>(One);
                break;
            case 2: poss = new List<Vector2>(Two);
                break;
            case 3: poss = new List<Vector2>(Three);
                break;
        }

        for (int i = 0; i < poss.Count; i++)
        {
            Vector2 worldPos = holder.transform.TransformPoint(poss[i]);
            poss[i] = worldPos;
        }

        for (int i = 0; i < m_Heros.Count; i++)
        {
            m_Heros[i].Position_Change(holder, poss, i);
        }
        
    }
    public void G_GetClick(bool active)
    {
        GetClick.gameObject.SetActive(active);
    }

    public void S_SetClick(bool active)
    {
        SetClick.gameObject.SetActive(active);
    }
    
    public void GetRange()
    {
        float range = m_Data.heroRange * 2;
        Circle_Range.localScale = new Vector3(range, range);
        
        Circle_Range.gameObject.SetActive(true);
        CanvasObject.SetActive(true);
    }

    public void ReturnRange()
    {
        Circle_Range.gameObject.SetActive(false);
        Circle_Range.localScale = Vector2.zero;
        
        CanvasObject.SetActive(false);
    }
    private void MakeCollider()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);
    }
    public void SpawnCharacter(HeroData heroData, string rarity)
    {
        Holder_Name = heroData.heroName;
        m_Data = heroData;
        if (IsServer)
        {
            HeroSpawn(Net_Utils.LocalID(), heroData, rarity);
        }
    }
    
    private void HeroSpawn(ulong clientId, HeroData heroData, string rarity)
    {
        var go = Instantiate(_spawnHero);
        
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();
        
        //네트워크 오브젝트는 스폰이 된 이후에 자식 위치를 변겨앻야한다.
        go.transform.parent = this.transform;
        
        ClientSpawnHeroClientRpc(networkObject.NetworkObjectId, clientId, heroData, rarity);
    }

    private void CheckGetPosition()
    {
        for (int i = 0; i < m_Heros.Count; i++)
        {
            m_Heros[i].transform.localPosition = Hero_Vector_Pos(m_Heros.Count)[i];
            m_Heros[i].OrderChange(i+1);
        }
    }

    private Vector2[] Hero_Vector_Pos(int count)
    {
        switch (count)
        {
            case 1: return One;
            case 2: return Two;
            case 3: return Three;
        }

        return null;
    }
    
    [ClientRpc]
    private void ClientSpawnHeroClientRpc(ulong networkId, ulong clientId, HeroData data, string rarity)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId,
                out NetworkObject networkObject))
        {
            Hero hero = networkObject.GetComponent<Hero>();
            
            m_Heros.Add(hero);
            networkObject.GetComponent<Hero>().Initialize(data, this, rarity);
            CheckGetPosition();
        }
    }
}
