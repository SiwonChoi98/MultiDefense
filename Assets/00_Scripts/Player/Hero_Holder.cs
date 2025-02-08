using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Android.Gradle.Manifest;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Host에서만 동작하는 객체
/// </summary>
public class Hero_Holder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    [SerializeField] private Transform Circle_Range;
    [SerializeField] private Transform SetClick;
    [SerializeField] private Transform GetClick;

    
    public string Holder_Part_Name;
    public string Holder_Name;
    public List<Hero> m_Heros = new();
    
    public Vector2 pos;
    private HeroData m_Data;
    
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
    
    private void Start()
    {
        MakeCollider();
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
    }

    public void ReturnRange()
    {
        Circle_Range.gameObject.SetActive(false);
        Circle_Range.localScale = Vector2.zero;
    }
    private void MakeCollider()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);
    }
    public void SpawnCharacter(HeroData heroData)
    {
        Holder_Name = heroData.heroName;
        m_Data = heroData;
        if (IsServer)
        {
            HeroSpawn(LocalID(), heroData);
        }
    }
    
    private void HeroSpawn(ulong clientId, HeroData heroData)
    {
        var go = Instantiate(_spawnHero);
        
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();
        
        //네트워크 오브젝트는 스폰이 된 이후에 자식 위치를 변겨앻야한다.
        go.transform.parent = this.transform;
        
        ClientSpawnHeroClientRpc(networkObject.NetworkObjectId, clientId, heroData);
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
    private void ClientSpawnHeroClientRpc(ulong networkId, ulong clientId, HeroData data)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId,
                out NetworkObject networkObject))
        {
            Hero hero = networkObject.GetComponent<Hero>();
            
            m_Heros.Add(hero);
            networkObject.GetComponent<Hero>().Initialize(data, this);
            CheckGetPosition();
        }
    }
    
    private ulong LocalID()
    {
        return Unity.Netcode.NetworkManager.Singleton.LocalClientId;
    }
}
