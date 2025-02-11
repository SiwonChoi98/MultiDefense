
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Relay;

public partial class Net_Mng : MonoBehaviour
{
    //코드를 통해 접근
    public async void JoinGameWithCode(string inputJoinCode)
    {
        if (string.IsNullOrEmpty(inputJoinCode))
        {
            Debug.Log("유효하지 않은  join Code 입니다.");
        }

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(inputJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );
            
            StartClient();
            Debug.Log("join code로 게임에 접속 성송");
        }
        catch (RelayServiceException e)
        {
            Debug.Log("게임에 접속 실패" + e);
        }
    }

    //Button 랜덤 매칭
    public async void StartMatchMaking()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("로그인되지 않았습니다.");
            return;
        }

        Matching_Object.SetActive(true);
        
        //현재 로비를 찾기
        _currentLobby = await FindAvailableLobby();
        
        if (_currentLobby == null)
        {
            await CreateNewLobby();
        }
        else
        {
            await JoinLobby(_currentLobby.Id);
        }
        
    }

    private async Task<Lobby> FindAvailableLobby()
    {
        //예외처리
        try
        {
            var queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            if (queryResponse.Results.Count > 0)
            {
                return queryResponse.Results[0];
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("로비 찾기 실패" + e);
        }

        return null;
    }

    //로비 제거
    private async void DestoryLobby(string lobbyId)
    {
        try
        {
            if (!string.IsNullOrEmpty(lobbyId))
            {
                //로비 파괴
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log("Lobby Destroy : " + lobbyId);
            }

            if (NetworkManager.Singleton.IsHost)
            {
                //호스트 연결 끊기
                NetworkManager.Singleton.Shutdown();
                Matching_Object.SetActive(false);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Failed to destroy lobby : " + e.Message);
        }
    }
    private async Task CreateNewLobby()
    {
        try
        {
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("랜덤 매칭방", maxPlayers);
            Debug.Log("새로운 방 생성됨 : " + _currentLobby.Id);

            await AllocateRelayServerAndJoin(_currentLobby);
            
            CancelButton.onClick.AddListener(() => DestoryLobby(_currentLobby.Id));
            
            StartHost();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("로비 생성 실패" + e);
        }
    }

    private async Task JoinLobby(string lobbyId)
    {
        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("방에 접속 되었습니다." + _currentLobby.Id);
            StartClient();
        }
        catch(LobbyServiceException e)
        {
            Debug.Log("로비 참가 실패" + e);
        }
    }
    private async Task AllocateRelayServerAndJoin(Lobby lobby)
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(lobby.MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            JoinCodeText.text = joinCode;
            
            Debug.Log("Relay 서버 할당 완료 join Code : " + joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Relay 서버 할당 실패" + e);
            throw;
        }
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("호스트가 시작되었습니다.");

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnHostDisconnected;

    }

    private void OnClientConnected(ulong clientId)
    {
        OnPlayerJoined();
    }

    private void OnHostDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnHostDisconnected;
        }
    }
    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("클라이언트가 시작되었습니다.");
    }
}
