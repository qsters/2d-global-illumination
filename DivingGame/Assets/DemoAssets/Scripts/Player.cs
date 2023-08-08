using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    // Singleton
    public static Player singleton;

    [SerializeField] private TextMeshProUGUI _scoreText;
    
    private int _currentScore;
    

    private void Start()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _scoreText.text = _currentScore.ToString();
    }

    // Add to score
    public void AddScore(int score)
    {
        _currentScore += score;
        _scoreText.text = _currentScore.ToString();
    }
}
