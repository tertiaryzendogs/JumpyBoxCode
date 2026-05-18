using UnityEngine;

public class Collectibles : MonoBehaviour
{
    [SerializeField]
    private PlayerControl player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJumpCollectibleSound();
            }
            if (gameObject.CompareTag("ExtraJump") && player.extraJumps + 1 <= player.maxExtraJumps)
            {
                player.extraJumps++;
            }
            Destroy(gameObject);
        }
    }
}
