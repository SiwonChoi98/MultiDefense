
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public partial class Net_Mng : MonoBehaviour
{
    private Lobby _currentLobby;
    private const int maxPlayers = 2;
    private string gamePlaySceneName = "GamePlayScene";
    
    public Button StartMatchButton, JoinMatchButton;
    public InputField fieldText;
    public Text JoinCodeText;
    //게임이 시작되었을 때 
    private async void Start()
    {
        //유니티 서비스 초기화
        await UnityServices.InitializeAsync();
        
        //유니티 게이밍 서비스에 로그인 되어있지 않으면 
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //다시 로그인 처리
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        
        StartMatchButton.onClick.AddListener(() => StartMatchMaking());
        JoinMatchButton.onClick.AddListener(() => JoinGameWithCode(fieldText.text));
    }

    
}
