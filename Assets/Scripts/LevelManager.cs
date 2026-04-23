using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void StartLevel(int level)
    {
        if (level == 1)
        {
            SceneManager.LoadScene("Lv1");
        }
    }
}
