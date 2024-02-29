using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // Singleton
    public static Player singleton;

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] public float secondsWithoutResurface = 10f;
    [SerializeField] private Rope _rope;
    
    private int _currentScore;
    private float secondsLeft;
    private HingeJoint2D _joint;
    private DistanceJoint2D _distanceJoint;

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
        _joint = GetComponent<HingeJoint2D>();
        _distanceJoint = GetComponent<DistanceJoint2D>();
        
        secondsLeft = secondsWithoutResurface;
        _joint.enabled = true;
        _distanceJoint.enabled = true;
    }
    
    private void Update()
    {
        secondsLeft -= Time.deltaTime;
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            _joint.enabled = false;
            _distanceJoint.enabled = false;
            _rope.Detach(gameObject);
        }
    }
    
    public void Resurface()
    {
        secondsLeft = secondsWithoutResurface;
    }

    public float TimeToDeath()
    {
        return secondsLeft;
    }

    // Add to score
    public void AddScore(int score)
    {
        _currentScore += score;
        _scoreText.text = _currentScore.ToString();
    }
}
