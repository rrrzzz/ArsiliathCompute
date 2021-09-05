using System.Collections.Generic;
using UnityEngine;

public class BoidsController : MonoBehaviour
{
    // [SerializeField] private ComputeShader cs;
    [SerializeField] private int boidsCount;
    // [SerializeField] private int resolution;
    [SerializeField] private bool cohesionOn;
    [SerializeField] private bool avoidanceOn;
    [SerializeField] private bool steeringOn;
    [SerializeField] private bool wanderingOn;
    [SerializeField] private float visionRange;
    [SerializeField] private float speed = 1;
    
    [SerializeField] private GameObject boidPrefab;
    
    private List<Boid> _boids = new List<Boid>();
    

    private void Start()
    {
        var firstBoid = Instantiate(boidPrefab);
        _boids.Add(new Boid(firstBoid.transform.position, Vector2.up, 1, firstBoid));
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        
        var boid = _boids[0].BoidObject.transform;

            
        Debug.Log("Global " + boid.forward);

    }
}

public struct Boid
{
    public Boid(Vector3 pos, Vector2 dir, float spd, GameObject go)
    {
        Pos = new Vector2(pos.x, pos.z);
        Dir = dir;
        Spd = spd;
        BoidObject = go;
    }
    
    public Vector2 Pos;
    public Vector2 Dir;
    public float Spd;
    public GameObject BoidObject;
}
