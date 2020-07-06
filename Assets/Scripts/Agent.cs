using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour {
    [Header("ORCA")] 
    [SerializeField] private float radius_ = 0.5f;
    [SerializeField] private float timeHorizon_ = 1.0f;
    [SerializeField] private float neighborsDist_ = 3.0f;
    [SerializeField] private int maxNeighbors_ = 10;
    [SerializeField] private float maxSpeed_ = 5.0f;
    private List<KeyValuePair<float, Agent>> agentNeighbors_;
    List <Line> orcaLines_ = new List<Line>();

    [Header("Movement")]
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float arriveDistance = 1.0f;

    private Rigidbody body_;
    private Vector3 target_;
    private Vector3 velocity_;
    private Vector3 desiredVelocity_;
    private Vector3 newVelocity_;

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
        if (maxNeighbors_ > 0)
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
                        if (agentNeighbors_.Count < maxNeighbors_)
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
                        if (agentNeighbors_.Count == maxNeighbors_)
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
            desiredVelocity_ = (target_ - transform.position).normalized * maxSpeed_ * (dist / arriveDistance);
        }
        else
        {
            desiredVelocity_ = (target_ - transform.position).normalized * maxSpeed_;
        }

        if (desiredVelocity_ == Vector3.zero)
        {
            return;
        }
        
        //Update velocity for static obstacles
        int nbObstacleLine = 0;
        
        //Update velocity by looking at other agents
        orcaLines_.Clear();
        float invTimeHorizon = 1.0f / timeHorizon_;
        for (int i = 0; i < agentNeighbors_.Count; ++i)
        {
            Agent otherAgent = agentNeighbors_[i].Value;

            Vector3 relativePosition = otherAgent.transform.position - transform.position;
            Vector3 relativeVelocity = velocity_ - otherAgent.velocity_;
            float distSqr = relativePosition.sqrMagnitude;
            float combinedRadius = radius_ + otherAgent.radius_;
            float combinedRadiusSqr = Mathf.Pow(combinedRadius, 2);

            Line line;
            Vector3 u;

            if (distSqr > combinedRadiusSqr)
            {
                // No Collision
                Vector3 w = relativeVelocity - invTimeHorizon * relativePosition;
                
                // Vector from center to relative velocity
                float wLengthSqr = w.sqrMagnitude;
                float dotProduct1 = Vector3.Dot(w, relativePosition);

                if (dotProduct1 < 0.0f && Mathf.Pow(dotProduct1, 2) > combinedRadiusSqr * wLengthSqr)
                {
                    //Project on circle
                    float wLength = Mathf.Sqrt(wLengthSqr);
                    Vector3 unitW = w / wLength;
                    
                    line.direction = new Vector3(unitW.z, 0, -unitW.x);
                    u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                }
                else
                {
                    //Projection on legs
                    float leg = Mathf.Sqrt(distSqr - combinedRadiusSqr);

                    if (Det(relativePosition, w) > 0.0f)
                    {
                        line.direction = new Vector3(
                            relativePosition.x * leg - relativePosition.z * combinedRadius,
                            0,
                            relativePosition.x * combinedRadius + relativePosition.z * leg) / distSqr;
                    }
                    else
                    {
                        line.direction = -new Vector3(
                            relativePosition.x * leg - relativePosition.z * combinedRadius,
                            0,
                            -relativePosition.x * combinedRadius + relativePosition.z * leg) / distSqr;
                    }

                    float dotProduct2 = Vector3.Dot(relativeVelocity, line.direction);
                    u = dotProduct2 * line.direction - relativeVelocity;
                }
            }
            else
            {
                //Collision
                float invTimeStep = 1.0f / Time.deltaTime;

                Vector3 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = w.magnitude;
                Vector3 wUnit = w / wLength;
                
                line.direction = new Vector3(wUnit.z, 0, -wUnit.x);
                u = (combinedRadius * invTimeStep - wLength) * wUnit;
            }

            line.startingPoint = velocity_ + 0.5f * u;
            orcaLines_.Add(line);
        }
        
        int lineFail = LinearProgram2(orcaLines_, maxSpeed_, desiredVelocity_, false, ref newVelocity_);

        if (lineFail < orcaLines_.Count)
        {
            LinearProgram3(orcaLines_, nbObstacleLine, lineFail, maxSpeed_, ref newVelocity_);
        }
    }

    private void FixedUpdate()
    {
        velocity_ = newVelocity_;
        
        float yVel = body_.velocity.y;
        body_.velocity = new Vector3(velocity_.x, yVel, velocity_.z);
        transform.forward = new Vector3(velocity_.x, 0, velocity_.z);
    }

    private bool LinearProgram1(List<Line> lines, int lineNo, float radius, Vector3 optVelocity, bool directionOpt,
        ref Vector3 result)
    {
        float dotProduct = Vector3.Dot(lines[lineNo].startingPoint, lines[lineNo].direction);
        float discriminant = Mathf.Pow(dotProduct, 2) + Mathf.Pow(radius, 2) - lines[lineNo].startingPoint.sqrMagnitude;

        if (discriminant < 0.0f)
        {
            return false;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i)
        {
            float denominator = Det(lines[lineNo].direction, lines[i].direction);
            float numerator = Det(lines[i].direction, lines[lineNo].startingPoint - lines[i].startingPoint);

            //Check if line lineNo and i are //
            if (Mathf.Abs(denominator) <= 0.00001f)
            {
                if (numerator < 0.0f)
                {
                    return false;
                }
                continue;
            }

            float t = numerator / denominator;

            if (denominator >= 0.0f)
            {
                tRight = Mathf.Min(tRight, t);
            }
            else
            {
                tLeft = Mathf.Max(tLeft, t);
            }

            if (tLeft > tRight)
            {
                return false;
            }
        }

        if (directionOpt)
        {
            if (Vector3.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
            {
                result = lines[lineNo].startingPoint + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].startingPoint + tLeft * lines[lineNo].direction;
            }
        }else
        {
            float t = Vector3.Dot(lines[lineNo].direction, optVelocity - lines[lineNo].startingPoint);

            if (t < tLeft)
            {
                result = lines[lineNo].startingPoint + tLeft * lines[lineNo].direction;
            }else if (t > tRight)
            {
                result = lines[lineNo].startingPoint + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].startingPoint + t * lines[lineNo].direction;
            }
        }

        return true;
    }

    private int LinearProgram2(List<Line> lines, float radius, Vector3 optVelocity, bool directionOpt, ref Vector3 result)
    {
        if (directionOpt)
        {
            result = optVelocity * radius;
        }else if (optVelocity.sqrMagnitude > Mathf.Pow(radius, 2))
        {
            result = optVelocity.normalized * radius;
        }
        else
        {
            result = optVelocity;
        }

        for (int i = 0; i < lines.Count; ++i)
        {
            if (Det(lines[i].direction, lines[i].startingPoint - result) > 0.0f)
            {
                Vector3 tmpResult = result;
                if (!LinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tmpResult;
                    return i;
                }
            }
        }

        return lines.Count;
    }

    private void LinearProgram3(List<Line> lines, int nbObstacleLine, int beginLine, float radius, ref Vector3 result)
    {
        float distance = 0.0f;

        for (int i = beginLine; i < lines.Count; ++i)
        {
            if (Det(lines[i].direction, lines[i].startingPoint - result) > distance)
            {
                List<Line> projectedLines = new List<Line>();
                for (int j = 0; j < nbObstacleLine; j++)
                {
                    projectedLines.Add(lines[j]);
                }

                for (int j = nbObstacleLine; j < i; j++)
                {
                    Line line;

                    float determinant = Det(lines[i].direction, lines[j].direction);

                    if (Mathf.Abs(determinant) <= 0.000001f)
                    {
                        if (Vector3.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                        {
                            //Line i and j are in the same direction
                            continue;
                        }
                        else
                        {
                            line.startingPoint = 0.5f * (lines[i].startingPoint + lines[j].startingPoint);
                        }
                    }
                    else
                    {
                        line.startingPoint = lines[i].startingPoint +
                                             (Det(lines[j].direction, lines[i].startingPoint - lines[j].startingPoint) /
                                              determinant) * lines[i].direction;
                    }

                    line.direction = (lines[j].direction - lines[i].direction).normalized;
                    projectedLines.Add(line);
                }

                Vector3 tmpResult = result;
                if (LinearProgram2(projectedLines, radius, new Vector3(-lines[i].direction.z, 0, lines[i].direction.x),
                    true, ref result) < projectedLines.Count)
                {
                    result = tmpResult;
                }

                distance = Det(lines[i].direction, lines[i].startingPoint - result);
            }
        }
    }

    private float Det(Vector3 v1, Vector3 v2)
    {
        return v1.x * v2.z - v1.z * v2.x;
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
