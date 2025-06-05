using UnityEngine;
using UnityEngine.SceneManagement;

public class NextStage : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        SceneManager.LoadScene("3_Fight");
    }
    public void CallBonFire()
    {
        SceneManager.LoadScene("2_Bonfire");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
