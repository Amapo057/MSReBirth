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
        // �÷��̾� HP ǥ��
        playerHpText.text = "Player HP: " + playerScript.playerHp.ToString("F0");
        // Ƽ�� HP ǥ��
        tyrHpText.text = "Tyr HP: " + tyrScript.tyrHp.ToString("F0");
        // �÷��̾ �׾��� ��
        if (playerScript.playerHp <= 0)
        {
            playerScript.playerSprite.SetTrigger("Die");
            Debug.Log("Player has died.");
        }

    }
}
