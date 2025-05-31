using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public PlayerController playerScript;
    public TyrController tyrScript;

    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI tyrHpText;


    // Update is called once per frame
    void Update()
    {
        // 플레이어 HP 표시
        playerHpText.text = "Player HP: " + playerScript.playerHp.ToString("F0");
        // 티르 HP 표시
        tyrHpText.text = "Tyr HP: " + tyrScript.tyrHP.ToString("F0");
        // 플레이어가 죽었을 때
        if (playerScript.playerHp <= 0)
        {
            playerScript.playerSprite.SetTrigger("Die");
            Debug.Log("Player has died.");
        }

    }
}
