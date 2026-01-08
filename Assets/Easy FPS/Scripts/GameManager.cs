using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
      public static GameManager instance;
    public int highScore;
    public int currentScore;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*void Update()
    {   
        if(currentScore > highScore)
        {
            highScore = currentScore;
        }

        CheckGameOver(); 
    }

    void CheckGameOver()
    {
        if(currentScore >= 10) 
        {
            LoadGameOverScene();
        }
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("GameOver"); 
        
    }*/

}