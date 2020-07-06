using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle {
    public Obstacle next;
    public Obstacle previous;
    public Line line;
    public int id;
    public bool convex;
}
