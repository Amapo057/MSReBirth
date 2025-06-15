using UnityEngine;

public class PlayerSpriteController : MonoBehaviour
{
    public PlayerController playerController;
    public PlayerAgent PlayerAgent;
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
        // playerController.AttackEnd();
        PlayerAgent.AttackEnd();
    }
    public void CallHitEnd()
    {
        // playerController.HitEnd();
        PlayerAgent.HitEnd();
    }
}
