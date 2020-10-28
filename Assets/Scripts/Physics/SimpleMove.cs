﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMove : MonoBehaviour
{

    public CharacterController c;

    void Update()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        input = input.normalized * Mathf.Min(input.magnitude, 1);
        c.Move(input * Time.deltaTime * 10);
    }
}
