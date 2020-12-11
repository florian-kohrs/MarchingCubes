using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSimpleCarackterControler : MonoBehaviour
{

    public CharacterController c;

    public float speed;

    void Start()
    {
        if(c == null)
        {
            c = GetComponent<CharacterController>();
        }
    }

    void Update()
    {

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        input = input.normalized * Mathf.Min(input.magnitude, 1);
        input *= speed * Time.deltaTime;

        input = transform.TransformDirection(input);

        c.Move(input);

    }
}
