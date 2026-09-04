using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSelf : MonoBehaviour
{
    public Vector3 direction = Vector3.back;
    public float speed = 50;
    // Start is called before the first frame update
    private void Start()
    {
        if (direction == Vector3.zero)
            direction = Vector3.back;
    }
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(direction * speed * Time.deltaTime, Space.Self);
    }
}
