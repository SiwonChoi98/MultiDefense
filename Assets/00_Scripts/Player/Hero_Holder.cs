using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host에서만 동작하는 객체
/// </summary>
public class Hero_Holder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    public string Holder_Name;
    public int Character_Count;
    public List<Hero> m_Heros = new();
    private void Start()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);
    }

    public void SpawnCharacter(HeroData heroData)
    {
        Holder_Name = heroData.heroName;
        
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
            m_Heros[i].transform.localPosition = Vector2.zero;
        }
    }
    [ClientRpc]
    private void ClientSpawnHeroClientRpc(ulong networkId, ulong clientId, HeroData data)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId,
                out NetworkObject networkObject))
        {
            Hero hero = networkObject.GetComponent<Hero>();
            
            m_Heros.Add(hero);
            networkObject.GetComponent<Hero>().Initialize(data);
            CheckGetPosition();
        }
    }
    
    private ulong LocalID()
    {
        return Unity.Netcode.NetworkManager.Singleton.LocalClientId;
    }
}
