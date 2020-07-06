using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RectObstacle : MonoBehaviour
{
    void Start()
    {
        ObstacleManager manager = FindObjectOfType<ObstacleManager>();

        manager.AddObstacle(GetRectVertices(GetComponent<BoxCollider>()));
    }

    private List<Vector2> GetRectVertices(BoxCollider collider)
    {
        List<Vector2> vertices = new List<Vector2>();
        
        Vector2 center = new Vector2(transform.position.x + collider.center.x, transform.position.z + collider.center.z);
        Vector2 scale = new Vector2(transform.localScale.x, transform.localScale.z) * 0.5f;
        
        //Bottom left
        vertices.Add(center + new Vector2(-collider.size.x, -collider.size.z) * scale);
        //Bottom right
        vertices.Add(center + new Vector2(collider.size.x, -collider.size.z) * scale);
        //Top right
        vertices.Add(center + new Vector2(collider.size.x, collider.size.z) * scale);
        //Top Left
        vertices.Add(center + new Vector2(-collider.size.x, collider.size.z) * scale);

        return vertices;
    }

    private void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();

        List<Vector2> vertices = GetRectVertices(collider);

        for (int i = 0; i < vertices.Count; i++)
        {
            Gizmos.DrawLine(new Vector3(
                vertices[i].x, 0, vertices[i].y), 
                new Vector3(
                    vertices[(i + 1) % vertices.Count].x, 0, vertices[(i + 1) % vertices.Count].y));
        }
    }
}
