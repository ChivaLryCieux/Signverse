using UnityEngine;

public class Harmful : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerDeath playerDeath = collision.gameObject.GetComponentInParent<PlayerDeath>();
            if (playerDeath != null)
            {
                playerDeath.Die();
            }
        }
    }
}
