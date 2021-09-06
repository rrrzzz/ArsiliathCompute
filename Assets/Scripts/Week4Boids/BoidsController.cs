using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidsController : MonoBehaviour
{
    // [SerializeField] private ComputeShader cs;
    [SerializeField] private int boidsCount;
    [SerializeField] private int resolution = 15;
    [SerializeField] private bool cohesionOn;
    [SerializeField, Range(0,1)] private float cohesionStrength;
    [SerializeField] private bool avoidanceOn;
    [SerializeField, Range(0,1)] private float avoidanceStrength;
    [SerializeField] private bool steeringOn;
    [SerializeField, Range(0,1)] private float steeringStrength;
    [SerializeField] private bool wanderingOn;
    [SerializeField] private float speed = 1;
    [SerializeField] private int frameInterval = 1;
    [SerializeField] private float boundsDistance = .1f;
    [SerializeField] private float _visionAngle = .1f;
    [SerializeField] private float _visionRange;
    
    [SerializeField] private GameObject boidPrefab;

    private List<Boid> _boids = new List<Boid>();
    private Boid[] _boidsNewDir;
    
    public static Vector2 Vec3ToPos(Vector3 posIn) => new Vector2(posIn.z, posIn.y);
    public static Vector2 Vec3ToDir(Vector3 dirIn) => new Vector2(dirIn.z, dirIn.y);

    public static Vector3 PosToVec3(Vector2 posIn) => new Vector3(0, posIn.y, posIn.x);
    public static Vector3 DirToVec3(Vector2 dirIn) => new Vector3(0, dirIn.y, dirIn.x);
    
    private void Start()
    {
        ResetSim();
    }

    [Button]
    private void ResetSim()
    {
        if (_boids.Count > boidsCount)
        {
            for (int i = boidsCount; i < _boids.Count; i++)
                Destroy(_boids[i].Go);
            
            _boids = _boids.Take(boidsCount).ToList();
        }
        
        for (int i = 0; i < boidsCount; i++)
            CreateBoid(i);
        
        _boidsNewDir = new Boid[boidsCount];
    }

    private void Update()
    {
        // if (Time.frameCount % frameInterval != 0)
        //     return;

        if (!Input.GetKey(KeyCode.Space)) return;
        
        for (int i = 0; i < _boids.Count; i++)
        {
            var boid = _boids[i];
            var neighbours = GetNeighbours(boid);

            if (neighbours.Count != 0)
            {
                var newDir = GetInteractionResultDir(neighbours, boid);
                boid.Dir = newDir;
            }
            
            _boidsNewDir[i] = boid;
        }
        
        for (int i = 0; i < _boids.Count; i++)
        {
            var currentBoid = _boidsNewDir[i];
            _boids[i] = MoveBoid(currentBoid);
        }
    }
    
    private Vector2 GetInteractionResultDir(List<Boid> neighbours, Boid currentBoid)
    {
        var avoidDir = Vector2.zero;
        var steerDir = Vector2.zero;

        foreach (var boid in neighbours)
        {
            avoidDir += currentBoid.Pos - boid.Pos;
            steerDir += boid.Dir;
        }

        avoidDir.Normalize();
        steerDir.Normalize();

        Vector2 outDir = Vector2.zero;
        if (cohesionOn)
            outDir = Vector2.Lerp(currentBoid.Dir, -avoidDir, cohesionStrength);
        if (avoidanceOn)
            outDir += Vector2.Lerp(currentBoid.Dir, avoidDir, avoidanceStrength);
        if (steeringOn)
            outDir += Vector2.Lerp(currentBoid.Dir, steerDir, steeringStrength);

        return outDir == Vector2.zero ? currentBoid.Dir : outDir.normalized;
    }

    private List<Boid> GetNeighbours(Boid boid)
    {
        var neighbours = new List<Boid>();
        
        foreach (var b in _boids)
        {
            if (b.Go.Equals(boid.Go))
                continue;

            var toNeighbour = b.Pos - boid.Pos;

            if (toNeighbour.magnitude > _visionRange)
                continue;

            if (Vector2.Dot(toNeighbour, boid.Dir) <= _visionAngle)
                continue;

            neighbours.Add(b);
        }

        return neighbours;
    }
    
    private void CreateBoid(int idx)
    {
        var pos = GetRandomPos();
        var rot = GetRandomRot();

        if (idx >= _boids.Count)
        {
            var boid = Instantiate(boidPrefab, GetRandomPos(), GetRandomRot());
            boid.name = "Boid " + idx;
            _boids.Add(new Boid(boid.transform.position, boid.transform.forward, speed, boid));
            return;
        }

        var b = _boids[idx];

        var tr = b.Go.transform;
        tr.position = pos;
        tr.rotation = rot;
        
        b.Dir = Vec3ToDir(tr.forward);
        b.Pos = Vec3ToPos(pos);
        b.Spd = speed;

        _boids[idx] = b;
    }
    
    private Vector3 GetRandomPos() =>
        new Vector3(0, Random.Range(-resolution, (float)resolution), Random.Range(-resolution, (float)resolution));

    private Quaternion GetRandomRot() => Quaternion.Euler(Random.Range(0f, 360), 0, 0);

    private Boid MoveBoid(Boid boid)
    {
        if (Mathf.Abs(boid.Pos.x) >= resolution - boundsDistance)
            boid.Dir.x = Mathf.Sign(boid.Pos.x) == Mathf.Sign(boid.Dir.x) ? -boid.Dir.x : boid.Dir.x;
       
        if (Mathf.Abs(boid.Pos.y) >= resolution - boundsDistance)
            boid.Dir.y = Mathf.Sign(boid.Pos.y) == Mathf.Sign(boid.Dir.y) ? -boid.Dir.y : boid.Dir.y;
        
        boid.Pos += boid.Dir * boid.Spd;
        boid.Go.transform.position = PosToVec3(boid.Pos);

        boid.Go.transform.rotation = Quaternion.LookRotation(DirToVec3(boid.Dir), boid.Go.transform.up);
        return boid;
    }
}

public struct Boid
{
    public Boid(Vector3 pos, Vector3 dir, float spd, GameObject go)
    {
        Pos = BoidsController.Vec3ToPos(pos);
        Dir = BoidsController.Vec3ToDir(dir);
        Spd = spd;
        Go = go;
    }
    
    public Vector2 Pos; // z is x, y is y
    public Vector2 Dir;
    public float Spd;
    public GameObject Go;
}
