using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour {
    [Header("ORCA")] 
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float timeHorizon_ = 1.0f;
    [SerializeField] private float neighborsDist_ = 3.0f;
    private int maxNeighbors = 7; 
    private List<KeyValuePair<float, Agent>> agentNeighbors_;

    [Header("Movement")]
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float arriveDistance = 1.0f;

    private Rigidbody body_;
    private Vector3 target_;
    private Vector3 desiredVelocity_;

    // Start is called before the first frame update
    void Start()
    {
        agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        
        body_ = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //Check neighbors
        if (maxNeighbors > 0)
        {
            agentNeighbors_.Clear();
            float rangeSqr = neighborsDist_ * neighborsDist_;

            foreach (var agent in FindObjectsOfType<Agent>())
            {
                if (this != agent)
                {
                    Vector3 dir = transform.position - agent.transform.position;
                    float distSqr = Vector3.Dot(dir, dir);

                    //If the other agent is under the minimum range => add it
                    if (distSqr < rangeSqr)
                    {
                        //If there is a free space, add it immediatly
                        if (agentNeighbors_.Count < maxNeighbors)
                        {
                            agentNeighbors_.Add(new KeyValuePair<float, Agent>(distSqr, agent));
                        }
                        
                        //Make sure the list is sorted
                        int i = agentNeighbors_.Count - 1;
                        while (i != 0 && distSqr < agentNeighbors_[i - 1].Key)
                        {
                            agentNeighbors_[i] = agentNeighbors_[i - 1];
                            i--;
                        }
    
                        //Once a spot with a further agent is found, place if 
                        agentNeighbors_[i] = new KeyValuePair<float, Agent>(distSqr, agent);

                        //If the list is full, only check agent nearer than the farrest neighbor.
                        if (agentNeighbors_.Count == maxNeighbors)
                        {
                            rangeSqr = agentNeighbors_[agentNeighbors_.Count - 1].Key;
                        }
                    }
                }
            }
        }
        
        //Update movement
        float dist = Vector3.Distance(transform.position, target_);

        if (dist < stopDistance)
        {
            desiredVelocity_ = Vector3.zero;
        }else if (dist < arriveDistance)
        {
            desiredVelocity_ = (target_ - transform.position).normalized * 5.0f * (dist / arriveDistance);
        }
        else
        {
            desiredVelocity_ = (target_ - transform.position).normalized * 5.0f;
        }
    }

    private void FixedUpdate()
    {
        float yVel = body_.velocity.y;
        body_.velocity = new Vector3(desiredVelocity_.x, yVel, desiredVelocity_.z);
        transform.forward = new Vector3(desiredVelocity_.x, 0, desiredVelocity_.z);
    }

    public void SetTarget(Vector3 newTarget)
    {
        target_ = newTarget;
    }

    private void OnDrawGizmos()
    {
        foreach (var keyValuePair in agentNeighbors_)
        {
            Gizmos.DrawLine(transform.position, keyValuePair.Value.transform.position);
        }
    }
}
