using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;

public class TestMath : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        var normal = new Vector3(0, -1, -1);
        Debug.DrawRay(transform.position, normal, Color.red);
        Debug.DrawRay(transform.position, Vector3.up, Color.green);

        var projectDir = Vector3.Project(normal, Vector3.up);
        Debug.DrawRay(transform.position, projectDir, Color.blue);
        var dot = Vector3.Dot(projectDir, Vector3.up);
        // -1
        Debug.Log(dot);
    }
}
