using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Agent : MonoBehaviour {
    [Header("ORCA")] 
    [SerializeField] private float radius_ = 0.5f;
    [SerializeField] private float timeHorizon_ = 1.0f;
    [SerializeField] private float neighborsDist_ = 3.0f;
    [SerializeField] private int maxNeighbors_ = 10;
    [SerializeField] private float maxSpeed_ = 5.0f;
    private List<KeyValuePair<float, Agent>> agentNeighbors_;
    private List<KeyValuePair<float, Obstacle>> obstacleNeighbors_;
    List <Line> orcaLines_ = new List<Line>();
    private ObstacleManager obstacleManager_;

    [Header("Movement")]
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float arriveDistance = 1.0f;

    private Rigidbody body_;
    private Vector3 target_;
    private Vector2 velocity_;
    private Vector2 desiredVelocity_;
    private Vector2 newVelocity_;

    // Start is called before the first frame update
    void Start()
    {
        agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();

        obstacleManager_ = FindObjectOfType<ObstacleManager>();
        
        body_ = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float rangeSqr = neighborsDist_ * neighborsDist_;
        Vector2 position2D = new Vector2(transform.position.x, transform.position.z);
        
        //Check neighbors
        if (maxNeighbors_ > 0)
        {
            agentNeighbors_.Clear();

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
        
        //Find obstacles
        obstacleNeighbors_.Clear();
        foreach (var obstacle in obstacleManager_.GetObstacles())
        {
            Obstacle nextObstacle = obstacle.next;

            float distSqr = DistSqrPointLine(
                obstacle.line.point, 
                nextObstacle.line.point,
                position2D);

            //If the other agent is under the minimum range => add it
            if (distSqr < rangeSqr)
            {
                obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSqr, obstacle));

                int i = obstacleNeighbors_.Count - 1;

                while (i != 0 && distSqr < obstacleNeighbors_[i - 1].Key)
                {
                    obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                    i--;
                }
                
                obstacleNeighbors_[i] = new KeyValuePair<float, Obstacle>(distSqr, obstacle);
            }
        }

        //Update movement
        float dist = Vector3.Distance(transform.position, target_);

        if (dist < stopDistance)
        {
            desiredVelocity_ = Vector2.zero;
        }else if (dist < arriveDistance)
        {
            Vector3 vel = (target_ - transform.position).normalized * (maxSpeed_ * (dist / arriveDistance));
            desiredVelocity_ = new Vector2(vel.x, vel.z);
        }
        else
        {
            Vector3 vel = (target_ - transform.position).normalized * maxSpeed_;
            desiredVelocity_ = new Vector2(vel.x, vel.z);
        }

        // if (desiredVelocity_ == Vector2.zero)
        // {
        //     return;
        // }
        
        //Update velocity for static obstacles
        orcaLines_.Clear();
        float invTimeHorizon = 1.0f / timeHorizon_;

        for (int i = 0; i < obstacleNeighbors_.Count; ++i)
        {
            Obstacle obstacle1 = obstacleNeighbors_[i].Value;
            Obstacle obstacle2 = obstacle1.next;

            Vector2 relativePosition1 = obstacle1.line.point - position2D;
            
            Vector2 relativePosition2 = obstacle2.line.point - position2D;

            bool alreadyCovered = false;
            
            for(int j = 0; j < orcaLines_.Count; ++j)
            {
                if (Det(invTimeHorizon * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) -
                    invTimeHorizon * radius_ >= -0.0000001f &&
                    Det(invTimeHorizon * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) -
                    invTimeHorizon * radius_ >= -0.0000001f)
                {
                    alreadyCovered = true;
                    break;
                }
            }
            
            if(alreadyCovered) continue;
            
            //Check for collision
            float distSqr1 = relativePosition1.sqrMagnitude;
            float distSqr2 = relativePosition2.sqrMagnitude;

            float radiusSqr = Mathf.Pow(radius_, 2);
            Vector2 obstacleVector = obstacle2.line.point - obstacle1.line.point;
            float s = -Vector2.Dot(relativePosition1, obstacleVector) / obstacleVector.sqrMagnitude;
            float distSqrLine = (-relativePosition1 - s * obstacleVector).sqrMagnitude;

            Line line;

            if (s < 0.0f && distSqr1 <= radiusSqr)
            {
                //Collision with left
                if (obstacle1.convex)
                {
                    line.point = Vector2.zero;
                    line.direction = new Vector2(-relativePosition1.y, relativePosition1.x).normalized;
                    orcaLines_.Add(line);
                }
                
                continue;
            }else if (s > 1.0f && distSqr2 <= radiusSqr)
            {
                //Collision with right vertex
                if (obstacle2.convex && Det(relativePosition2, obstacle2.line.direction) >= 0.0f)
                {
                    line.point = Vector2.zero;
                    line.direction = new Vector2(-relativePosition2.y, relativePosition2.x).normalized;
                    orcaLines_.Add(line);
                }
                
                continue;
            }else if (s > 0.0f && s < 1.0f && distSqrLine <= radiusSqr)
            {
                //Collision with obstacle segement
                line.point = Vector2.zero;
                line.direction = -obstacle1.line.direction;
                orcaLines_.Add(line);

                continue;
            }
            
            //No collision => Compute legs
            Vector2 leftLegDirection, rightLegDirection;

            if (s < 0.0f && distSqrLine <= radiusSqr)
            {
                if(!obstacle1.convex) continue;

                obstacle2 = obstacle1;

                float leg1 = Mathf.Sqrt(distSqr1 - radiusSqr);
                leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSqr1;
                rightLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, -relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSqr1;
            }else if (s > 1.0f && distSqrLine <= radiusSqr)
            {
                if(!obstacle2.convex) continue;

                obstacle1 = obstacle2;
                
                float leg1 = Mathf.Sqrt(distSqr2 - radiusSqr);
                leftLegDirection = new Vector2(relativePosition2.x * leg1 - relativePosition2.y * radius_, relativePosition2.x * radius_ + relativePosition2.y * leg1) / distSqr2;
                rightLegDirection = new Vector2(relativePosition2.x * leg1 - relativePosition2.y * radius_, -relativePosition2.x * radius_ + relativePosition2.y * leg1) / distSqr2;
            }
            else
            {
                if (obstacle1.convex)
                {
                    float leg1 = Mathf.Sqrt(distSqr1 - radiusSqr);
                    leftLegDirection =  new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSqr1;
                }
                else
                {
                    leftLegDirection = -obstacle1.line.direction;
                }
                
                if (obstacle2.convex)
                {
                    float leg1 = Mathf.Sqrt(distSqr2 - radiusSqr);
                    rightLegDirection =  new Vector2(relativePosition2.x * leg1 - relativePosition2.y * radius_, relativePosition2.x * radius_ + relativePosition2.y * leg1) / distSqr2;
                }
                else
                {
                    rightLegDirection = obstacle1.line.direction;
                }
            }
            
            //Make sure leg doesn't go throught other leg
            Obstacle leftNeighbor = obstacle1.previous;

            bool isLeftLegForeign = false;
            bool isRightLegForeign = false;

            if (obstacle1.convex && Det(leftLegDirection, -leftNeighbor.line.direction) >= 0.0f)
            {
                leftLegDirection = -leftNeighbor.line.direction;
                isLeftLegForeign = true;
            }
            
            if (obstacle2.convex && Det(rightLegDirection, obstacle2.line.direction) <= 0.0f)
            {
                rightLegDirection = obstacle2.line.direction;
                isRightLegForeign = true;
            }
            
            //Compute cut-off centers
            Vector2 leftCutOff = invTimeHorizon * (obstacle1.line.point - position2D);
            Vector2 rightCutOff = invTimeHorizon * (obstacle2.line.point - position2D);
            Vector2 cutOffVector = rightCutOff - leftCutOff;
            
            //Check if current velocity if projected on cutoff circle
            float t = obstacle1 == obstacle2
                ? 0.5f
                : Vector2.Dot((velocity_ - leftCutOff), cutOffVector) / cutOffVector.sqrMagnitude;
            float tLeft = Vector2.Dot((velocity_ - leftCutOff), leftLegDirection);
            float tRight = Vector2.Dot((velocity_ - rightCutOff), rightLegDirection);

            if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
            {
                Vector2 unitW = (velocity_ - leftCutOff).normalized;

                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = leftCutOff + radius_ * invTimeHorizon * unitW;
                orcaLines_.Add(line);
                
                continue;
            }else if (t > 1.0f && tRight < 0.0f)
            {
                Vector2 unitW = (velocity_ - rightCutOff).normalized;
                
                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = rightCutOff + radius_ * invTimeHorizon * unitW;
                orcaLines_.Add(line);
                
                continue;
            }

            float distSqrCutoff = t < 0.0f || t > 1.0f || obstacle1 == obstacle2
                ? float.PositiveInfinity
                : (velocity_ - (leftCutOff + t * cutOffVector)).sqrMagnitude;
            float distSqrLeft = tLeft < 0.0f
                ? float.PositiveInfinity
                : (velocity_ - (leftCutOff + tLeft * leftLegDirection)).sqrMagnitude;
            float distSqrRight = tRight < 0.0f
                ? float.PositiveInfinity
                : (velocity_ - (rightCutOff + tRight * rightLegDirection)).sqrMagnitude;

            if (distSqrCutoff <= distSqrLeft && distSqrCutoff <= distSqrRight)
            {
                line.direction = -obstacle1.line.direction;
                line.point =
                    leftCutOff + radius_ * invTimeHorizon * new Vector2(-line.direction.y, line.direction.x);
                orcaLines_.Add(line);
                
                continue;
            }

            if (distSqrLeft <= distSqrRight)
            {
                if (isLeftLegForeign) continue;

                line.direction = leftLegDirection;
                line.point =
                    leftCutOff + radius_ * invTimeHorizon * new Vector2(-line.direction.y, line.direction.x);
                orcaLines_.Add(line);

                continue;
            }

            if (isRightLegForeign) continue;
            
            line.direction = -rightLegDirection;
            line.point =
                rightCutOff + radius_ * invTimeHorizon * new Vector2(-line.direction.y, line.direction.x);
            orcaLines_.Add(line);
        }
        
        int nbObstacleLine = orcaLines_.Count;
        
        //Update velocity by looking at other agents
        foreach (var pair in agentNeighbors_)
        {
            Agent otherAgent = pair.Value;

            Vector3 relPos = otherAgent.transform.position - transform.position;
            Vector2 relativePosition = new Vector2(relPos.x, relPos.z);
            Vector2 relativeVelocity = velocity_ - otherAgent.velocity_;
            float distSqr = relativePosition.sqrMagnitude;
            float combinedRadius = radius_ + otherAgent.radius_;
            float combinedRadiusSqr = Mathf.Pow(combinedRadius, 2);

            Line line;
            Vector2 u;

            if (distSqr > combinedRadiusSqr)
            {
                // No Collision
                Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;
                
                // Vector from center to relative velocity
                float wLengthSqr = w.sqrMagnitude;
                float dotProduct1 = Vector2.Dot(w, relativePosition);

                if (dotProduct1 < 0.0f && Mathf.Pow(dotProduct1, 2) > combinedRadiusSqr * wLengthSqr)
                {
                    //Project on circle
                    float wLength = Mathf.Sqrt(wLengthSqr);
                    Vector2 unitW = w / wLength;
                    
                    line.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                }
                else
                {
                    //Projection on legs
                    float leg = Mathf.Sqrt(distSqr - combinedRadiusSqr);

                    if (Det(relativePosition, w) > 0.0f)
                    {
                        line.direction = new Vector2(
                            relativePosition.x * leg - relativePosition.y * combinedRadius,
                            relativePosition.x * combinedRadius + relativePosition.y * leg) / distSqr;
                    }
                    else
                    {
                        line.direction = -new Vector2(
                            relativePosition.x * leg - relativePosition.y * combinedRadius,
                            -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSqr;
                    }

                    float dotProduct2 = Vector2.Dot(relativeVelocity, line.direction);
                    u = dotProduct2 * line.direction - relativeVelocity;
                }
            }
            else
            {
                //Collision
                float invTimeStep = 1.0f / Time.deltaTime;

                Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = w.magnitude;
                Vector2 wUnit = w / wLength;
                
                line.direction = new Vector2(wUnit.y, -wUnit.x);
                u = (combinedRadius * invTimeStep - wLength) * wUnit;
            }

            line.point = velocity_ + 0.5f * u;
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
        body_.velocity = new Vector3(velocity_.x, yVel, velocity_.y);
        if (velocity_ != Vector2.zero)
        {
            transform.forward = new Vector3(velocity_.x, 0, velocity_.y);
        }
    }

    private bool LinearProgram1(List<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt,
        ref Vector2 result)
    {
        float dotProduct = Vector2.Dot(lines[lineNo].point, lines[lineNo].direction);
        float discriminant = Mathf.Pow(dotProduct, 2) + Mathf.Pow(radius, 2) - lines[lineNo].point.sqrMagnitude;

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
            float numerator = Det(lines[i].direction, lines[lineNo].point - lines[i].point);

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
            if (Vector2.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
        }else
        {
            float t = Vector2.Dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

            if (t < tLeft)
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }else if (t > tRight)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + t * lines[lineNo].direction;
            }
        }

        return true;
    }

    private int LinearProgram2(List<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
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
            if (Det(lines[i].direction, lines[i].point - result) > 0.0f)
            {
                Vector2 tmpResult = result;
                if (!LinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tmpResult;
                    return i;
                }
            }
        }

        return lines.Count;
    }

    private void LinearProgram3(List<Line> lines, int nbObstacleLine, int beginLine, float radius, ref Vector2 result)
    {
        float distance = 0.0f;

        for (int i = beginLine; i < lines.Count; ++i)
        {
            if (Det(lines[i].direction, lines[i].point - result) > distance)
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
                        if (Vector2.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                        {
                            //Line i and j are in the same direction
                            continue;
                        }

                        line.point = 0.5f * (lines[i].point + lines[j].point);
                    }
                    else
                    {
                        line.point = lines[i].point +
                                             (Det(lines[j].direction, lines[i].point - lines[j].point) /
                                              determinant) * lines[i].direction;
                    }

                    line.direction = (lines[j].direction - lines[i].direction).normalized;
                    projectedLines.Add(line);
                }

                Vector2 tmpResult = result;
                if (LinearProgram2(projectedLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x),
                    true, ref result) < projectedLines.Count)
                {
                    result = tmpResult;
                }

                distance = Det(lines[i].direction, lines[i].point - result);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="v1">First point of segment</param>
    /// <param name="v2">Second point of the segment</param>
    /// <param name="v3">Point to calculated the sqr distance from</param>
    /// <returns></returns>
    private float DistSqrPointLine(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float r = Vector2.Dot(v3 - v1, v2 - v1) / (v2 - v1).sqrMagnitude;

        if (r < 0.0f)
        {
            return (v3 - v1).sqrMagnitude;
        }

        if (r > 1.0f)
        {
            return (v3 - v2).sqrMagnitude;
        }

        return (v3 - (v1 + r * (v2 - v1))).sqrMagnitude;
    }

    private float Det(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    public void SetTarget(Vector3 newTarget)
    {
        target_ = newTarget;
    }

    public Vector3 GetTarget()
    {
        return target_;
    }

    private void OnDrawGizmos()
    {
        // Gizmos.color = Color.white;
        // foreach (var keyValuePair in agentNeighbors_)
        // {
        //     Gizmos.DrawLine(transform.position, keyValuePair.Value.transform.position);
        // }
        //
        // Gizmos.color = Color.red;
        // foreach (var keyValuePair in obstacleNeighbors_)
        // {
        //     Gizmos.DrawLine(transform.position, new Vector3(keyValuePair.Value.line.point.x, 0, keyValuePair.Value.line.point.y));
        // }
    }
}
