using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class Spawner : NetworkBehaviour
{
    public static Spawner Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [SerializeField] private GameObject _spawnHolder;
    [SerializeField] private Monster _spawn_Monster_Prefab;

    public List<Vector2> Player_Move_List = new List<Vector2>();
    public List<Vector2> Other_Move_List = new List<Vector2>();
    
    private static List<Vector2> Player_spawn_List = new List<Vector2>();
    private static List<Vector2> Other_spawn_List = new List<Vector2>();
    
    public static List<bool> Player_spawn_List_Array = new List<bool>();
    public static List<bool>Other_spawn_List_Array = new List<bool>();

    public Dictionary<string, Hero_Holder> Hero_Holders = new();
    private int[] Host_Client_Value_Index = new int[2];
    
    public static float xValue, yValue;
    
    public void Holder_Position_Set(string value01, string value02)
    {
        if (IsServer)
        {
            GetPositionSet(value01, value02);
        }
        else if(IsClient)
        {
            GetPositionServerRpc(value01, value02);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetPositionServerRpc(string value01, string value02)
    {
        GetPositionSet(value01, value02);
    }

    [ClientRpc]
    private void GetPositionSetClientRpc(string value01, string value02)
    {
        Hero_Holder holder1 = Hero_Holders[value01];
        Hero_Holder holder2 = Hero_Holders[value02];
        
        //위치 변경 *position
        holder1.HeroChange(holder2);
        holder2.HeroChange(holder1);

        List<Hero> Heros01 = new List<Hero>(holder1.m_Heros);
        List<Hero> Heros02 = new List<Hero>(holder2.m_Heros);

        (holder1.Holder_Name, holder2.Holder_Name) = (holder2.Holder_Name, holder1.Holder_Name);

        holder1.m_Heros = new List<Hero>(Heros02);
        holder2.m_Heros = new List<Hero>(Heros01);
    }
    private void GetPositionSet(string value01, string value02)
    {
        GetPositionSetClientRpc(value01, value02);
    }
    private void Start()
    {
        SetGrid();

        StartCoroutine(Spawn_Monster_Coroutine());
    }

    private void SetGrid()
    {
        Grid_Start(transform.GetChild(0), true);
        Grid_Start(transform.GetChild(1), false);
        
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            Player_Move_List.Add(transform.GetChild(0).GetChild(i).position);
        }
        
        for (int i = 0; i < transform.GetChild(1).childCount; i++)
        {
            Other_Move_List.Add(transform.GetChild(1).GetChild(i).position);
        }
    }

    #region Make_Grid

    private void Grid_Start(Transform tt, bool player)
    {
        SpriteRenderer parentSprite = tt.GetComponent<SpriteRenderer>();
        float parentWidth = parentSprite.bounds.size.x;
        float parentHeight = parentSprite.bounds.size.y;
        
        float xCount = tt.localScale.x / 6;
        float yCount = tt.localScale.y / 3;

        xValue = xCount;
        yValue = yCount;
        
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                float xPos = (-parentWidth / 2) + (col * xCount) + (xCount / 2);
                float yPos = ((player ? parentHeight : -parentHeight) / 2) + (player ? -1 : 1) * (row * yCount) + (yCount / 2);

                switch (player)
                {
                    case true:
                        Player_spawn_List.Add(new Vector2(xPos, yPos + tt.localPosition.y - yCount));
                        Player_spawn_List_Array.Add(false);
                        break;
                    case false:
                        Other_spawn_List.Add(new Vector2(xPos, yPos + tt.localPosition.y));
                        Other_spawn_List_Array.Add(false);
                        break;
                }
                
            }
        }
    }

    #endregion
    
    #region 캐릭터 소환

    public void Summon(string rarity)
    {
        // if (Game_Mng.Instance.Money < Game_Mng.Instance.SummonCount)
        //     return;
        //
        // Game_Mng.Instance.Money -= Game_Mng.Instance.SummonCount;
        // Game_Mng.Instance.SummonCount += 2;

        if (IsClient)
        {
            ServerHeroSpawnServerRpc(LocalID(), rarity);
        }
        else if(IsServer)
        {
            HeroSpawn(LocalID(), rarity);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerHeroSpawnServerRpc(ulong clientId, string rarity)
    {
        HeroSpawn(clientId, rarity);
    }
    private void HeroSpawn(ulong clientId, string rarity)
    {
        Hero_Scriptable[] m_Character_Datas = Resources.LoadAll<Hero_Scriptable>("Character_Scriptable/" + rarity);
        var data = m_Character_Datas[UnityEngine.Random.Range(0, m_Character_Datas.Length)];

        bool getHero = false;
        string temp = clientId == 0 ? "HOST" : "CLIENT";
        int value = clientId == 0 ? 0 : 1;
        string Organizers = temp + Host_Client_Value_Index[value].ToString();
        foreach (var dd in Hero_Holders)
        {
            if (dd.Key.Contains(temp))
            {
                if (dd.Value.m_Heros.Count < 3 && dd.Value.Holder_Name == data.Name)
                {
                    dd.Value.SpawnCharacter(data.GetHeroData(), rarity);
                    getHero = true;
                    break;
                }
            }
            
        }

        if (getHero == false)
        {
            var go = Instantiate(_spawnHolder);
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
            
            ClientSpawnHeroClientRpc(networkObject.NetworkObjectId, clientId, data.GetHeroData(), Organizers, value, rarity);
        }
        
    }

    [ClientRpc]
    private void ClientSpawnHeroClientRpc(ulong networkId, ulong clientId, HeroData data, string Organizers, int value, string rarity)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId,
                out NetworkObject networkObject))
        {
            if (clientId == LocalID())
            {
                SetPositionHero(networkObject, true);
            }
            else
            {
                SetPositionHero(networkObject, false);
            }
            
            Hero_Holder goHolder = networkObject.GetComponent<Hero_Holder>();
            Hero_Holders.Add(Organizers, goHolder);
            Host_Client_Value_Index[value]++;
            
            goHolder.Holder_Part_Name = Organizers;
            
            networkObject.GetComponent<Hero_Holder>().SpawnCharacter(data, rarity);
        }
    }

    private void SetPositionHero(NetworkObject obj, bool player)
    {
        List<bool> spawnListArray = player ? Player_spawn_List_Array : Other_spawn_List_Array;
        List<Vector2> spawnList = player ? Player_spawn_List : Other_spawn_List;
        
        int position_value = -1;
        for (int i = 0; i < spawnListArray.Count; i++)
        {
            if (spawnListArray[i] == false)
            {
                position_value = i;
                spawnListArray[i] = true;
                break;
            }
        }
    
        obj.transform.position = spawnList[position_value];
        Hero_Holder heroHolder = obj.GetComponent<Hero_Holder>();
        //heroHolder.pos = spawnList[position_value];
        heroHolder.index = position_value;

    }
    #endregion
    
    #region 몬스터 소환

    private IEnumerator Spawn_Monster_Coroutine()
    {
        yield return new WaitForSeconds(1.0f);
        
        if (IsClient)
        {
            ServerMonsterSpawnServerRpc(LocalID());
        }
        else if (IsServer)
        {
            MonsterSpawn(LocalID());
        }
        

        StartCoroutine(Spawn_Monster_Coroutine());
    }

    //owner만 소환할 수 있는 것을 클라이언트도 요청할 수 있게끔 변경
    //serverRpc는 서버에서만 동작하게 함
    [ServerRpc(RequireOwnership = false)]
    private void ServerMonsterSpawnServerRpc(ulong clientId)
    {
        MonsterSpawn(clientId);
    }

    private void MonsterSpawn(ulong clientId)
    {
        var go = Instantiate(_spawn_Monster_Prefab);
        //Game_Mng.Instance.AddMonster(go);

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();

        Game_Mng.Instance.AddMonster(go);
        ClientMonsterSetClientRpc(networkObject.NetworkObjectId, clientId);
    }

    [ClientRpc]
    private void ClientMonsterSetClientRpc(ulong networkObjectId, ulong clientId)
    {
        if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId,
                out NetworkObject monsterNetworkObject))
        {
            if (clientId == LocalID())
            {
                monsterNetworkObject.transform.position = Player_Move_List[0];
                monsterNetworkObject.GetComponent<Monster>().Init(Player_Move_List);
            }
            else
            {
                monsterNetworkObject.transform.position = Other_Move_List[0];
                monsterNetworkObject.GetComponent<Monster>().Init(Other_Move_List);
            }
        }
            
    }

    private ulong LocalID()
    {
        return Unity.Netcode.NetworkManager.Singleton.LocalClientId;
    }
    #endregion
}
