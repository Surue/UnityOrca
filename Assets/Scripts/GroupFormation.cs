using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GroupFormation : MonoBehaviour
{
    [Serializable]
    enum Shape {
        SQUARE,
        LINE,
        COLUMN
    }

    enum State {
        FORMING,
        FORMED
    }

    [Header("Group settings")] 
    [SerializeField] private GameObject prefabAgent_;
    [SerializeField] private Shape shape_;
    private State state_ = State.FORMING;
    [SerializeField] private int nbAgent_;
    private List<Agent> agents_;

    [Header("Movement")] 
    [SerializeField] private float speedForming_;
    [SerializeField] private float speedFormed_;
    [SerializeField] private float agentSpacing_ = 1.5f;
    [SerializeField] private float avoidanceSpeed_ = 1.5f;

    private List<Vector3> targetsPositionFormation_;
    private List<Vector3> desiredVelocities_;
    
    // Start is called before the first frame update
    void Start()
    {
        targetsPositionFormation_ = new List<Vector3>();
        agents_ = new List<Agent>();
        desiredVelocities_ = new List<Vector3>();
        
        for (int i = 0; i < nbAgent_; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * 5;
            Vector3 randomPos = new Vector3(randomCircle.x, 3, randomCircle.y);

            GameObject instance = Instantiate(prefabAgent_, randomPos, Quaternion.identity);
            agents_.Add(instance.GetComponent<Agent>());
            
            targetsPositionFormation_.Add(GetTargetPositionByIndex(i));
            desiredVelocities_.Add(Vector3.zero);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
        float speed = 0;
        
        //Update target position
        for (int i = 1; i < nbAgent_; i++)
        {
            targetsPositionFormation_[i] = GetTargetPositionByIndex(i);
        }
        
        //reset desired velocities
        //Update target position
        for (int i = 0; i < nbAgent_; i++)
        {
            desiredVelocities_[i] = Vector3.zero;
        }
        
        switch (state_)
        {
            case State.FORMING:
                for (int i = 0; i < nbAgent_; i++)
                {
                    agents_[i].SetTarget(targetsPositionFormation_[i]);
                }
                break;
            case State.FORMED:
                speed = speedFormed_;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Physics.Raycast(target, target - Camera.main.transform.position, out RaycastHit hit))
            {
                Debug.Log("hit");
                targetsPositionFormation_[0] = hit.point;
            }
        }
    }

    Vector3 GetTargetPositionByIndex(int index)
    {
        switch (shape_)
        {
            case Shape.SQUARE:
                int maxCol = Mathf.FloorToInt(Mathf.Sqrt(nbAgent_));

                int col = index % maxCol;
                int row = index / maxCol;

                if (col % 2 == 0)
                {
                    return agents_[0].transform.position +
                           agents_[0].transform.right * (col / 2 * agentSpacing_) -
                           agents_[0].transform.forward * (row * agentSpacing_);
                }
                else
                {
                    return agents_[0].transform.position - 
                           agents_[0].transform.right * (Mathf.CeilToInt(col / 2.0f) * agentSpacing_) -
                           agents_[0].transform.forward * (row * agentSpacing_);
                }
                break;
            case Shape.LINE:
                if (index % 2 == 0)
                {
                    return agents_[0].transform.position + agents_[0].transform.right * (index / 2.0f * agentSpacing_);
                }
                else
                {
                    return agents_[0].transform.position - agents_[0].transform.right * (Mathf.CeilToInt(index / 2.0f) * agentSpacing_);
                }
            case Shape.COLUMN:
                return agents_[0].transform.position - agents_[0].transform.forward * (index * agentSpacing_);
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (agents_ == null || agents_.Count == 0) return; 
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(agents_[0].GetTarget(), 0.5f);
        
        Gizmos.color = Color.white;
        
        switch (shape_)
        {
            case Shape.SQUARE:
                int maxRow = Mathf.CeilToInt(Mathf.Sqrt(nbAgent_));
                int maxCol = Mathf.FloorToInt(Mathf.Sqrt(nbAgent_));
                
                
                for (int row = 0; row < maxRow; row++)
                {
                    for (int col = 0; col < maxCol; col++)
                    {
                        if (row * maxCol + col >= nbAgent_)
                        {
                            return;
                        }
                        
                        if (col % 2 == 0)
                        {
                            Gizmos.DrawWireSphere(agents_[0].transform.position +
                                                  (agents_[0].transform.right * (col / 2) * agentSpacing_) -
                                                  (agents_[0].transform.forward * row * agentSpacing_),
                                0.5f);
                        }
                        else
                        {
                            Gizmos.DrawWireSphere(agents_[0].transform.position -
                                                  (agents_[0].transform.right * Mathf.CeilToInt(col / 2.0f) * agentSpacing_) -
                                                  (agents_[0].transform.forward * row * agentSpacing_),
                                0.5f);
                        }
                    }
                }
                break;
            case Shape.LINE:
                for (int i = 0; i < nbAgent_; i++)
                {
                    if (i % 2 == 0)
                    {
                        Gizmos.DrawWireSphere(agents_[0].transform.position + agents_[0].transform.right * (i / 2.0f) * agentSpacing_,
                            0.5f);
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(agents_[0].transform.position - agents_[0].transform.right * (Mathf.CeilToInt(i / 2.0f) * agentSpacing_),
                            0.5f);
                    }
                }
                break;
            case Shape.COLUMN:
                for (int i = 0; i < nbAgent_; i++)
                {
                    Gizmos.DrawWireSphere(agents_[0].transform.position - agents_[0].transform.forward * i * agentSpacing_, 0.5f);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
