using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public PlayerController playerScript;
    public TyrController tyrScript;

    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI tyrHpText;
    public Slider playerHpBar;
    public Slider tyrHpBar;


    // Update is called once per frame
    void Update()
    {
        playerHpText.text = "Player HP: " + playerScript.playerHp.ToString("F0");
        tyrHpText.text = "Tyr HP: " + tyrScript.tyrHp.ToString("F0");
        // if (playerScript.playerHp <= 0)
        // {
        //     playerScript.playerSprite.SetTrigger("Die");
        // }
        // hp바 관리

        float currentPlayerHp = playerScript.playerHp / 5f;
        float currentTyrHp = tyrScript.tyrHp / 25f;

        playerHpBar.value = currentPlayerHp;
        tyrHpBar.value = currentTyrHp;


    }
}
