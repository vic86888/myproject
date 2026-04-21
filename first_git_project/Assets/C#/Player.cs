using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    Rigidbody rb;
    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Vector3 move = new Vector3(h, 0f, v).normalized * moveSpeed;
        // 將輸入值與角色目前的面向（前後、左右）結合
        Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;

        // 計算最終的移動速度向量
        Vector3 move = moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}