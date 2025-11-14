using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    // Koppel deze methode aan de Button OnClick()
    public void StartGame()
    {
        SceneManager.LoadScene("OutsideMap", LoadSceneMode.Single);
    }
}
