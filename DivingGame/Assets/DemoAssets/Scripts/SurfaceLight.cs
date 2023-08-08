using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SurfaceLight : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; 
    [SerializeField] private float MaxLightDepth = 20f;
    [SerializeField] private float MaxEmission = 1f;
    [SerializeField] private float MinEmission = 0.1f;
    [SerializeField] private float _playerOffset = 16f;

    private Transform _thisTransform;
    private Material _emissionMat;
    private int _emissionAmountID;
    
    
    private void Awake()
    {
        _thisTransform = GetComponent<Transform>();
        _emissionMat = GetComponent<SpriteRenderer>().material;
        _emissionAmountID = Shader.PropertyToID("_EmissionAmount");
    }
    
    void Update()
    {
        // Returning if no need to update position
        float _distanceFromPlayer = _thisTransform.position.y - playerTransform.position.y;
        if (_distanceFromPlayer > _playerOffset * 1.5)
            return;
        
        Vector3 newPosition = playerTransform.position;
        float playerDepth = -newPosition.y;
        
        // Keep Y Position above Max Depth
        float newDepth = Mathf.Min(MaxLightDepth, playerDepth);
        newPosition.y = -newDepth + _playerOffset;
        _thisTransform.position = newPosition;
        
        // Emission Level set by player depth
        float currentEmission = Mathf.Lerp(MaxEmission, MinEmission, playerDepth / MaxLightDepth);
        _emissionMat.SetFloat(_emissionAmountID, currentEmission);
    }
}
