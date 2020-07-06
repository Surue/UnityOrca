using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour {
    private List<Obstacle> obstacles_ = new List<Obstacle>();

    public int AddObstacle(List<Vector2> vertices)
    {
        if (vertices.Count < 2)
        {
            return -1;
        }

        int obstacleNb = obstacles_.Count;

        for (int i = 0; i < vertices.Count; ++i)
        {
            Obstacle obstacle = new Obstacle();
            obstacle.line.point = vertices[i];

            if (i != 0)
            {
                obstacle.previous = obstacles_[obstacles_.Count - 1];
                obstacle.previous.next = obstacle;
            }

            if (i == vertices.Count - 1)
            {
                obstacle.next = obstacles_[obstacleNb];
                obstacle.next.previous = obstacle;
            }

            obstacle.line.direction = (vertices[(i == vertices.Count - 1 ? 0 : i + 1)] - vertices[i]).normalized;

            if (vertices.Count == 2)
            {
                obstacle.convex = true;
            }
            else
            {
                obstacle.convex = (LeftOf(
                    vertices[i == 0 ? vertices.Count - 1 : i - 1],
                    vertices[i],
                    vertices[(i == vertices.Count - 1 ? 0 : i + 1)]) >= 0.0f);
            }

            obstacle.id = obstacles_.Count;
            obstacles_.Add(obstacle);
        }

        return obstacleNb;
    }

    public List<Obstacle> GetObstacles()
    {
        return obstacles_;
    }
    
    static float Det(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    static float LeftOf(Vector2 a, Vector2 b, Vector2 c)
    {
        return Det(a - c, b - a);
    }

    private void OnDrawGizmos()
    {
        if (obstacles_ == null || obstacles_.Count == 0) return;

        foreach (var obstacle in obstacles_)
        {
            Gizmos.DrawLine(Vector3.zero, new Vector3(obstacle.line.point.x, 0, obstacle.line.point.y));
        }
    }
}
