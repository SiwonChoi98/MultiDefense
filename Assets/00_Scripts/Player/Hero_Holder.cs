using System;
using System.Collections.Generic;
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

    //처음 생성된 위치 값
    public int index;
    public string Holder_Name;
    public List<Hero> m_Heros = new();
    
    public Vector2 pos;
    private HeroData m_Data;
    
    private readonly Vector2[] One = { Vector2.zero };
    private readonly Vector2[] Two =
    {
        new Vector2(-0.1f, 0.05f), 
        new Vector2(0.1f, -0.1f)
    };
    private readonly Vector2[] Three =
    {
        new Vector2(-0.1f, 0.1f),
        new Vector2(0.1f, -0.05f), 
        new Vector2(-0.15f, -0.15f)
    };
    
    private void Start()
    {
        MakeCollider();
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

        SetClick.transform.localScale = new Vector2(collider.size.x * 2 , collider.size.y * 2);
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
