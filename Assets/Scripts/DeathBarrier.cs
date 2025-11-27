using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DeathBarrier : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.gameObject.TryGetComponent(out MarioAgent agent))
            {
                agent.AddReward(-5f);
                agent.EndEpisode();
            }
            other.gameObject.SetActive(false);
            GameManager.Instance.ResetLevel(3f);
        }
        else
        {
            Destroy(other.gameObject);
        }
    }

}
