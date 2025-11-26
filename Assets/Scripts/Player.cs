using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public CapsuleCollider2D capsuleCollider { get; private set; }
    //public PlayerMovement movement { get; private set; }
    public MarioAgent movement { get; private set; }

    public DeathAnimation deathAnimation { get; private set; }

    public PlayerSpriteRenderer smallRenderer;
    public PlayerSpriteRenderer bigRenderer;
    private PlayerSpriteRenderer activeRenderer;

    public bool big => bigRenderer.enabled;
    public bool dead => deathAnimation.enabled;
    public bool starpower { get; private set; }

    private readonly float maxEpisodeTime = 900f;
    private float episodeTimer = 0f;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        //movement = GetComponent<PlayerMovement>();
        movement = GetComponent<MarioAgent>();
        deathAnimation = GetComponent<DeathAnimation>();
        activeRenderer = smallRenderer;

        episodeTimer = 0f;
    }

    private void Update()
    {
        episodeTimer += Time.deltaTime;

        if (episodeTimer >= maxEpisodeTime)
        {
            Death();
            enabled = false;
        }
    }

    public void Hit()
    {
        if (!dead && !starpower)
        {
            if (big) 
            {
                Shrink();
                movement.AddReward(-1f);
            } 
            else 
            {
                movement.AddReward(-5f);
                Death();
            }
        }
    }

    public void Death()
    {
        smallRenderer.enabled = false;
        bigRenderer.enabled = false;
        deathAnimation.enabled = true;
        movement.EndEpisode();

        GameManager.Instance.ResetLevel(3f);
    }

    public void Grow()
    {
        smallRenderer.enabled = false;
        bigRenderer.enabled = true;
        activeRenderer = bigRenderer;

        capsuleCollider.size = new Vector2(1f, 2f);
        capsuleCollider.offset = new Vector2(0f, 0.5f);

        StartCoroutine(ScaleAnimation());
    }

    public void Shrink()
    {
        smallRenderer.enabled = true;
        bigRenderer.enabled = false;
        activeRenderer = smallRenderer;

        capsuleCollider.size = new Vector2(1f, 1f);
        capsuleCollider.offset = new Vector2(0f, 0f);

        StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Time.frameCount % 4 == 0)
            {
                smallRenderer.enabled = !smallRenderer.enabled;
                bigRenderer.enabled = !smallRenderer.enabled;
            }

            yield return null;
        }

        smallRenderer.enabled = false;
        bigRenderer.enabled = false;
        activeRenderer.enabled = true;
    }

    public void Starpower()
    {
        StartCoroutine(StarpowerAnimation());
    }

    private IEnumerator StarpowerAnimation()
    {
        starpower = true;

        float elapsed = 0f;
        float duration = 10f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Time.frameCount % 4 == 0) {
                activeRenderer.spriteRenderer.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
            }

            yield return null;
        }

        activeRenderer.spriteRenderer.color = Color.white;
        starpower = false;
    }

}
