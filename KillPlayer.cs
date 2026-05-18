using UnityEngine;
using UnityEngine.UIElements;

public class KillPlayer : MonoBehaviour
{
    [SerializeField]
    private PlayerControl playerControl;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerControl = collision.gameObject.GetComponent<PlayerControl>();

            // Play death sound
            AudioManager.Instance.PlayDeathSound();

            playerControl.gameObject.SetActive(false);
            MainMenuController.Instance.ChangeGameScreen(MainMenuController.Instance.gameMenu, MainMenuController.Instance.levelExitMenu, true);
            MainMenuController.Instance.quitToMainMenuButton.style.display = DisplayStyle.Flex;
            MainMenuController.Instance.retryLevelButton1.style.display = DisplayStyle.Flex;
            Time.timeScale = 0f;
            MainMenuController.Instance.inputActions.FindActionMap("Player").Disable();
            MainMenuController.Instance.UpdateEndScreen(false);
        }
    }
}
