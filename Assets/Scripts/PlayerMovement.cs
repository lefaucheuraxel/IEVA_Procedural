using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float MovementDirection;
    [SerializeField] float Speed = 7;
    
    
    [SerializeField] Rigidbody2D Rigidbody;
    
    
    void Start()
    {
        MovementDirection = Input.GetAxis("Horizontal");

    }

    // Update is called once per frame
    void Update(){
        MovementDirection = Input.GetAxis("Horizontal");
    }
    
    void FixedUpdate()
    {

        Rigidbody.linearVelocity = new Vector2(MovementDirection * Speed, Rigidbody.linearVelocity.y);
    }
}
