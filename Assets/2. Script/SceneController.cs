using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class SceneController : MonoBehaviour
{
    public TyrController tyr;
    public PlayerAgent player;
    public TextMeshProUGUI playerHP;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        playerHP.SetText("Player HP : {0}", player.playerHP);        
    }
}
