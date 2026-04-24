using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public void BackMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void EnterGame()
    {
        SceneManager.LoadScene("Story");
    }
    public void BackSelectLevel()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
