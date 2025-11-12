using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Type
    {
        Coin,
        ExtraLife,
        MagicMushroom,
        Starpower,
    }

    public Type type;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out Player player)) {
            other.gameObject.GetComponent<MarioAgent>().AddReward(0.5f);
            Collect(player);
        }
    }

    private void Collect(Player player)
    {
        switch (type)
        {
            case Type.Coin:
                GameManager.Instance.AddCoin();
                break;

            case Type.ExtraLife:
                GameManager.Instance.AddLife();
                break;

            case Type.MagicMushroom:
                player.Grow();
                break;

            case Type.Starpower:
                player.Starpower();
                break;
        }

        Destroy(gameObject);
    }

}
