using NUnit.Framework;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Rigidbody rig;
    public Collider col;

    private void Start()
    {
        rig = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
}