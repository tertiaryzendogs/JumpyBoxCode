using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [SerializeField]
    private PlayerControl playerControl;
    private SceneData sceneData;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            sceneData = MainMenuController.Instance.SceneData;
            playerControl = collision.gameObject.GetComponent<PlayerControl>();
            if (playerControl.jumps <= sceneData.Data.scoreBreakpoints[2])
            {
                MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] = 3;
            }
            else if (playerControl.jumps <= sceneData.Data.scoreBreakpoints[1])
            {
                if (MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] < 2)
                {
                    MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] = 2;
                }
            }
            else if (playerControl.jumps <= sceneData.Data.scoreBreakpoints[0])
            {
                if (MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] < 1)
                {
                    MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] = 1;
                }
            }
            else
            {
                if (MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] <= 0)
                {
                    MainMenuController.Instance.starsEarnedPerLevel[sceneData.Data.SceneIndex - 1] = 0;
                }
            }
            MainMenuController.Instance.UpdateEndScreen(true);
        }
    }
}
