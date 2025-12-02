using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToKitFind : MonoBehaviour
{
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("kitFind");
    }
}
