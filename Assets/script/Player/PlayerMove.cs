using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
public class PlayerMove : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;

    Rigidbody2D rigid;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 nextvec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextvec);
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }
}
