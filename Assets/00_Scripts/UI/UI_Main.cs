using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UI_Main : MonoBehaviour
{
    public static UI_Main Instance = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("text")]
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

    [Header("##trail")]
    
    [SerializeField] private GameObject TrailPrefabs;
    [UnityEngine.Range(0.0f, 30.0f)] 
    [SerializeField] private float trailSpeed;
    [SerializeField] private float yPosMin, yPosMax;
    [SerializeField] private float xPos;

    [Header("##Upgrade")] 
    [SerializeField] private TextMeshProUGUI u_Money_T;
    [SerializeField] private TextMeshProUGUI[] u_Upgrade_T;
    [SerializeField] private TextMeshProUGUI[] u_Upgrade_Asset_T;

    [Header("##Others")] 
    [SerializeField] private GameObject WavePopUp_Object;
    [SerializeField] private TextMeshProUGUI WaveText_Object;
    [SerializeField] private TextMeshProUGUI WaveBossName;
    
    private static readonly int Boss = Animator.StringToHash("Boss");

    private void Start()
    {
        Game_Mng.Instance.OnMoneyUp += Money_Anim;
        Game_Mng.Instance.OnTimerUp += WavePoint;
        
        SummonButton.onClick.AddListener(() => ClickSummon());
    }

    public void GetWavePopup(bool getBoss)
    {
        WavePopUp_Object.SetActive(true);

        WaveText_Object.text = string.Format("WAVE {0}", Game_Mng.Instance.Wave);

        if (getBoss)
        {
            Animator animator = WavePopUp_Object.GetComponent<Animator>();
            animator.SetTrigger(Boss);
            WaveBossName.text = Game_Mng.Instance.b_data.BossDatas[(int)(Game_Mng.Instance.Wave / 10) - 1].BossName;
        }
    }
    public void UpgradeButton(int value)
    {
        if (Game_Mng.Instance.Money < 30 + Game_Mng.Instance.Upgrade[value])
            return;

        Game_Mng.Instance.Money -= 30 + Game_Mng.Instance.Upgrade[value];
        Game_Mng.Instance.Upgrade[value]++;
    }
    private void Update()
    {
        MonsterCount_T.text = Game_Mng.Instance.MonsterCount + " / 100";
        //MonsterCountImage.fillAmount = (float)Game_Mng.Instance.MonsterCount / 100.0f;
        HeroCount_T.text = UpdateHeroCountText();
        Money_T.text = Game_Mng.Instance.Money.ToString();
        Summon_T.text = Game_Mng.Instance.SummonCount.ToString();
        u_Money_T.text = Game_Mng.Instance.Money.ToString();

        for (int i = 0; i < u_Upgrade_T.Length; i++)
        {
            u_Upgrade_T[i].text = "Lv." + (Game_Mng.Instance.Upgrade[i]+1).ToString();
            u_Upgrade_Asset_T[i].text = (30 + Game_Mng.Instance.Upgrade[i]).ToString();
        }
        
        Summon_T.color = Game_Mng.Instance.Money >= Game_Mng.Instance.SummonCount ? Color.white : Color.red;
        
    }

    private void ClickSummon()
    {
        
        if (Game_Mng.Instance.Money < Game_Mng.Instance.SummonCount)
            return;
        if (Game_Mng.Instance.HeroCount >= Game_Mng.Instance.HeroMaximumCount)
            return;
        
        Game_Mng.Instance.Money -= Game_Mng.Instance.SummonCount;
        Game_Mng.Instance.SummonCount += 2;
        Game_Mng.Instance.HeroCount++;
        
        
        StartCoroutine(SummonCoroutine());
    }

    private Vector2 GenerateRandomControlPoint(Vector3 start, Vector3 end)
    {
        //시작점과 끝점의 중간 위치
        Vector3 midPoint = (start + end) / 2;

        //y축 방향으로 랜덤한 높이를 추가하여 곡선을 만듬
        float randomHeight = Random.Range(1.0f, 3.0f);
        midPoint += Vector3.up * randomHeight;
        
        //x방향으로도 약간의 랜덤 변화를 추가
        midPoint += new Vector3(Random.Range(-1.0f, 1.0f), 0.0f);
        return midPoint;
    }

    private IEnumerator SummonCoroutine()
    {
        var data = Spawner.Instance.Data("Common");

        Vector3 buttonWorldPosition = Camera.main.ScreenToWorldPoint(SummonButton.transform.position);
        GameObject trailInstance = Instantiate(TrailPrefabs);

        trailInstance.transform.position = buttonWorldPosition;

        Vector3 endPos = Spawner.Instance.HolderPosition(data);
        
        Vector3 startPoint = buttonWorldPosition;
        Vector3 endPoint = endPos;

        Vector3 controlPoint = GenerateRandomControlPoint(startPoint, endPoint);

        float elapsedTime = 0.0f;

        while (elapsedTime < trailSpeed)
        {
            float t = elapsedTime / trailSpeed;

            Vector3 curvePosition = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);

            trailInstance.transform.position = new Vector3(curvePosition.x, curvePosition.y, 0.0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(trailInstance);
        Spawner.Instance.Summon("Common", data);
        
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        //베지어 곡선 공식 : (1-t)^2 & p0 + 2 * (1-t) * t * p1 + t^2 * p2
        return Mathf.Pow(1 - t, 2) * p0 + 2
            * (1 -  t) * t * p1 +
            Mathf.Pow(t, 2) * p2;
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
