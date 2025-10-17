using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    GameManager manager;
    SpriteRenderer MySpriteRenderer;
    private void Start()
    {
        manager = FindObjectOfType<GameManager>();
        MySpriteRenderer = GetComponent<SpriteRenderer>();
    }


    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.transform.TryGetComponent<Fruit>(out Fruit fruit))
        {
            if (fruit.MyRigidbody2D.linearVelocity.magnitude < .1f)
            {
                MySpriteRenderer.enabled = true;
                if ((fruit.transform.position.y - fruit.raduis)> transform.position.y)
                {
                    manager.IsGameOver=true;
                    print("Game over");
                }
            }
            else
            {
                MySpriteRenderer.enabled = false;
            }
        }
        else
        {
            MySpriteRenderer.enabled = false;
        }
    }
}
