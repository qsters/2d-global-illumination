using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Rope : MonoBehaviour
{
    public Rigidbody2D hook;
    public GameObject prefabRope;
    public int numLinks = 5;

    private HingeJoint2D _lastJoint;

    private void Awake()
    {
        GenerateRope();
    }

    void GenerateRope()
    {
        Rigidbody2D prevBod = hook;
        
        for (int i = 0; i < numLinks; i++)
        {
            GameObject link = Instantiate(prefabRope, transform, true);
            
            HingeJoint2D joint = link.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevBod;
            
            link.GetComponent<DistanceJoint2D>().connectedBody = prevBod;
            
            prevBod = link.GetComponent<Rigidbody2D>();
            
            _lastJoint = joint;
        }
    }

    public void Attach(GameObject connectedBody)
    {
        float spriteBottom = _lastJoint.GetComponent<SpriteRenderer>().bounds.size.y;
        
        var distanceJoint = connectedBody.GetComponent<DistanceJoint2D>();
        distanceJoint.connectedAnchor = new Vector2(0, spriteBottom * -1);
        distanceJoint.connectedBody = _lastJoint.GetComponent<Rigidbody2D>();
        
        var joint = connectedBody.GetComponent<HingeJoint2D>();
        joint.connectedAnchor = new Vector2(0, spriteBottom * -1);
        joint.connectedBody = _lastJoint.GetComponent<Rigidbody2D>();
        
    }
    
    public void Detach(GameObject connectedBody)
    {
        connectedBody.GetComponent<HingeJoint2D>().connectedBody = null;
        connectedBody.GetComponent<DistanceJoint2D>().connectedBody = null;
    }
}
