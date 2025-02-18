using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Main : MonoBehaviour
{
    public static UI_Main Instance = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [SerializeField] private TextMeshProUGUI MonsterCount_T;
    [SerializeField] private TextMeshProUGUI Money_T;
    [SerializeField] private TextMeshProUGUI Summon_T;
    [SerializeField] private TextMeshProUGUI Timer_T;
    [SerializeField] private TextMeshProUGUI Wave_T;
    [SerializeField] private TextMeshProUGUI Navigation_T;
    
    [SerializeField] private Transform Navigation_Content;
    [SerializeField] private TextMeshProUGUI HeroCount_T;
    
    //[SerializeField] private Image MonsterCountImage;
    
    [SerializeField] private Animator MoneyAnimation;

    private List<GameObject> NavigationTextList = new();

    [SerializeField] private Button SummonButton;
    private void Start()
    {
        Game_Mng.Instance.OnMoneyUp += Money_Anim;
        Game_Mng.Instance.OnTimerUp += WavePoint;
        
        SummonButton.onClick.AddListener(() => Spawner.Instance.Summon("Common", false));
    }
    private void Update()
    {
        MonsterCount_T.text = Game_Mng.Instance.MonsterCount + " / 100";
        //MonsterCountImage.fillAmount = (float)Game_Mng.Instance.MonsterCount / 100.0f;
        HeroCount_T.text = UpdateHeroCountText();
        Money_T.text = Game_Mng.Instance.Money.ToString();
        Summon_T.text = Game_Mng.Instance.SummonCount.ToString();
        Summon_T.color = Game_Mng.Instance.Money >= Game_Mng.Instance.SummonCount ? Color.white : Color.red;
        
    }

    public void GetNavigation(string temp)
    {
        if (NavigationTextList.Count > 7)
        {
            Destroy(NavigationTextList[0]);
            NavigationTextList.RemoveAt(0);
        }
        var go = Instantiate(Navigation_T, Navigation_Content);
        NavigationTextList.Add(go.gameObject);
        go.gameObject.SetActive(true);
        go.transform.SetAsFirstSibling();
        
        Destroy(go.gameObject, 1.5f);
        go.text = temp;
    }

    public void WavePoint()
    {
        Timer_T.text = UpdateTimerText();
        Wave_T.text = "WAVE : " + Game_Mng.Instance.Wave;
    }

    private string UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(Game_Mng.Instance.Timer / 60);
        int seconds = Mathf.FloorToInt(Game_Mng.Instance.Timer % 60);

        return $"{minutes:00}:{seconds:00}";
    }

    private string UpdateHeroCountText()
    {
        int myCount = Game_Mng.Instance.HeroCount;
        string temp = "";
        if (myCount < 10)
        {
            temp = "0" + myCount;
            
        }
        else
        {
            temp = myCount.ToString();
        }

        return string.Format("{0} / {1}", temp, Game_Mng.Instance.HeroMaximumCount);
    }
    private void Money_Anim()
    {
        MoneyAnimation.SetTrigger("DoGet");
    }
}
