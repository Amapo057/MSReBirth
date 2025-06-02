using UnityEngine;

public class PlayerSpriteController : MonoBehaviour
{
    public PlayerController playerController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CallAttackEnd()
    {
        playerController.AttackEnd();
    }
}
