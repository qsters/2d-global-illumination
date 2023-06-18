using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceLight : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;

    private Transform thisTransform;

    private void Awake()
    {
        thisTransform = GetComponent<Transform>();
    }

    // Setting the x Co-ordinate the same as the players;
    void Update()
    {
        Vector3 currentPosition = thisTransform.position;
        currentPosition.x = playerTransform.position.x;

        thisTransform.position = currentPosition;
    }
}
