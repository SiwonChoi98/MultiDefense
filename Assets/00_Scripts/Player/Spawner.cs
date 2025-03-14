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
    private Hero_Scriptable data;
    
    
    private Coroutine spawn_Monster_Coroutine;
    public void Holder_Position_Set(string value01, string value02)
    {
        Net_Utils.HostAndClientMethod(
            () => GetPositionServerRpc(value01, value02), 
            () => GetPositionSet(value01, value02));
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

        (holder1.Holder_Name, holder2.Holder_Name) = (holder2.Holder_Name, holder1.Holder_Name);
        (holder1.m_Heros, holder2.m_Heros) = (new List<Hero>(holder2.m_Heros), new List<Hero>(holder1.m_Heros));
        (holder1.m_Data, holder2.m_Data) = (holder2.m_Data, holder1.m_Data);

    }
    private void GetPositionSet(string value01, string value02)
    {
        GetPositionSetClientRpc(value01, value02);
    }
    private void Start()
    {
        SetGrid();
        spawn_Monster_Coroutine = StartCoroutine(Spawn_Monster_Coroutine(false));
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

                if (IsServer)
                {
                    StartCoroutine(DelayHeroHolderSpawn(player));
                }
            }
        }

        Host_Client_Value_Index[0] = 0; //Host
        Host_Client_Value_Index[1] = 0; //Client
    }

    private IEnumerator DelayHeroHolderSpawn(bool player)
    {
        var go = Instantiate(_spawnHolder);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();
        
        string temp = player == true ? "HOST" : "CLIENT";
        int value = player == true ? 0 : 1;
        string Organizers = temp + Host_Client_Value_Index[value].ToString();
        Host_Client_Value_Index[value]++;

        yield return new WaitForSeconds(0.5f);
        
        SpawnGridClientRpc(networkObject.NetworkObjectId, Organizers);

    }
    
    #endregion
    
    #region 캐릭터 소환
    
    public void Summon(string rarity, Hero_Scriptable scriptData = null)
    {

        data = scriptData;
        if (scriptData == null)
        {
            data = Data(rarity);
        }
        
        Net_Utils.HostAndClientMethod(
            () => ServerHeroSpawnServerRpc(Net_Utils.LocalID(), rarity, data.name),
            () => HeroSpawn(Net_Utils.LocalID(), rarity, data.name));
        
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerHeroSpawnServerRpc(ulong clientId, string rarity, string dataName)
    {
        HeroSpawn(clientId, rarity, dataName);
    }
    
    public Hero_Scriptable Data(string rarity)
    {
        Hero_Scriptable[] m_Character_Datas = Resources.LoadAll<Hero_Scriptable>("Character_Scriptable/" + rarity);
        var data = m_Character_Datas[UnityEngine.Random.Range(0, m_Character_Datas.Length)];

        return data;
    }
    
    private void HeroSpawn(ulong clientId, string rarity, string dataName)
    {
        data = Resources.Load<Hero_Scriptable>("Character_Scriptable/" + rarity + "/" + dataName);
        string temp = clientId == 0 ? "HOST" : "CLIENT";
        int value = clientId == 0 ? 0 : 1;
        string Organizers = temp + Host_Client_Value_Index[value].ToString();


        var existingHolder = GetExistingHolder(temp, data.Name);

        ClientNavigationClientRpc(clientId, data.Name, data.rare.ToString());
        
        if (existingHolder != null)
        {
            existingHolder.SpawnCharacter(data.GetHeroData(), rarity);
            return;
        }

        var networkObject = Hero_Holders[Organizers].GetComponent<NetworkObject>();
        
        ClientSpawnHeroClientRpc(networkObject.NetworkObjectId, data.GetHeroData(), value, rarity);
        
    }

    public Vector3 HolderPosition(Hero_Scriptable data)
    {
        string temp = Net_Utils.LocalID() == 0 ? "HOST" : "CLIENT";
        int value = Net_Utils.LocalID() == 0 ? 0 : 1;
        string Organizers = temp + Host_Client_Value_Index[value].ToString();
        var existingHolder = GetExistingHolder(temp, data.Name);
        if (existingHolder != null)
        {
            return existingHolder.transform.position;
        }

        return Hero_Holders[Organizers].transform.position;
    }
    private Hero_Holder GetExistingHolder(string clientKey, string heroName)
    {
        foreach (var holder in Hero_Holders)
        {
            if (holder.Key.Contains(clientKey) && holder.Value.m_Heros.Count < 3 && holder.Value.Holder_Name == heroName)
            {
                return holder.Value;
            }
        }

        return null;
    }

    [ClientRpc]
    private void SpawnGridClientRpc(ulong networkId, string organizers)
    {
        if (Net_Utils.TryGetSpawnedObject(networkId, out NetworkObject networkObject))
        {
            bool isPlayer;
            if (organizers.Contains("HOST"))
            {
                isPlayer = Net_Utils.LocalID() == 0 ? true : false;
            }
            else isPlayer = Net_Utils.LocalID() == 0 ? false : true;
            
            Hero_Holder goHolder = networkObject.GetComponent<Hero_Holder>();
            SetPositionHero(networkObject, 
                isPlayer ? Player_spawn_List : Other_spawn_List, 
                isPlayer ? Player_spawn_List_Array : Other_spawn_List_Array);
            
            Hero_Holders.Add(organizers, goHolder);
            goHolder.Holder_Part_Name = organizers;
        }
    }
    [ClientRpc]
    private void ClientSpawnHeroClientRpc(ulong networkId, HeroData data, int value, string rarity)
    {
        if (Net_Utils.TryGetSpawnedObject(networkId, out NetworkObject networkObject))
        {
            Hero_Holder goHolder = networkObject.GetComponent<Hero_Holder>();
            
            Host_Client_Value_Index[value]++;
            goHolder.SpawnCharacter(data, rarity);
        }
    }

    [ClientRpc]
    private void ClientNavigationClientRpc(ulong networkID, string heroName, string rarity)
    {
        if (networkID == Net_Utils.LocalID())
        {
            UI_Main.Instance.GetNavigation(string.Format("영웅을 획득하였습니다. {0}{1}", 
                Net_Utils.RarityColor((Rarity)Enum.Parse(typeof(Rarity), rarity)), heroName));
        }
    }
    private void SetPositionHero(NetworkObject obj, List<Vector2> spawnList, List<bool> spawnArrayList)
    {
        int position_value = spawnArrayList.IndexOf(false);
        if (position_value != -1)
        {
            spawnArrayList[position_value] = true;
            obj.transform.position = spawnList[position_value];
        }
        
        Hero_Holder heroHolder = obj.GetComponent<Hero_Holder>();
        heroHolder.index = position_value;

    }
    #endregion
    
    #region 몬스터 소환

    

    private IEnumerator Spawn_Monster_Coroutine(bool getBoss)
    {
        yield return new WaitForSeconds(getBoss == false ? 1.0f : 0.0f);

        Net_Utils.HostAndClientMethod(
            () => ServerMonsterSpawnServerRpc(Net_Utils.LocalID(), getBoss), 
            () => MonsterSpawn(Net_Utils.LocalID(), getBoss));
        
        if(getBoss) yield break;
        
        spawn_Monster_Coroutine = StartCoroutine(Spawn_Monster_Coroutine(getBoss));
    }

    public void ReMonsterSpawn()
    {
        //예외 처리
        if (spawn_Monster_Coroutine != null)
        {
            StopCoroutine(spawn_Monster_Coroutine);
        }
        
        spawn_Monster_Coroutine = StartCoroutine(Spawn_Monster_Coroutine(false));
    }
    public void BossSpawn()
    {
        if (spawn_Monster_Coroutine != null)
        {
            StopCoroutine(spawn_Monster_Coroutine);
        }

        StartCoroutine(Spawn_Monster_Coroutine(true));
    }
    
    //owner만 소환할 수 있는 것을 클라이언트도 요청할 수 있게끔 변경
    //serverRpc는 서버에서만 동작하게 함
    [ServerRpc(RequireOwnership = false)]
    private void ServerMonsterSpawnServerRpc(ulong clientId, bool getBoss)
    {
        MonsterSpawn(clientId, getBoss);
    }

    private void MonsterSpawn(ulong clientId, bool getBoss)
    {
        Monster go = null;
        if (!getBoss)
        {
            go = Instantiate(_spawn_Monster_Prefab);
        }
        else
        {
            go = Instantiate(Game_Mng.Instance.b_data.BossDatas[(int)(Game_Mng.Instance.Wave / 10) - 1].BossPrefab);
        }
        
        //Game_Mng.Instance.AddMonster(go);

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();

        Game_Mng.Instance.AddMonster(go, getBoss);
        ClientMonsterSetClientRpc(networkObject.NetworkObjectId, clientId);
    }

    [ClientRpc]
    private void ClientMonsterSetClientRpc(ulong networkObjectId, ulong clientId)
    {
        if (Net_Utils.TryGetSpawnedObject(networkObjectId, out NetworkObject monsterNetworkObject))
        {
            var moveList = clientId == Net_Utils.LocalID() ? Player_Move_List : Other_Move_List;
            monsterNetworkObject.transform.position = moveList[0];
            monsterNetworkObject.GetComponent<Monster>().Init(moveList);
        }
            
    }
    
    #endregion
}
