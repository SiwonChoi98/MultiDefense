using TMPro;
using UnityEngine;

public class UI_Main : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI MonsterCount_T;
    [SerializeField] private TextMeshProUGUI Money_T;
    [SerializeField] private TextMeshProUGUI Summon_T;

    [SerializeField] private Animator MoneyAnimation;

    private void Start()
    {
        Game_Mng.Instance.OnMoneyUp += Money_Anim;
    }
    private void Update()
    {
        MonsterCount_T.text = Game_Mng.Instance.Monsters.Count.ToString() + " / 100";
        Money_T.text = Game_Mng.Instance.Money.ToString();
        Summon_T.text = Game_Mng.Instance.SummonCount.ToString();
        Summon_T.color = Game_Mng.Instance.Money >= Game_Mng.Instance.SummonCount ? Color.white : Color.red;
        
    }

    private void Money_Anim()
    {
        MoneyAnimation.SetTrigger("DoGet");
    }
}
