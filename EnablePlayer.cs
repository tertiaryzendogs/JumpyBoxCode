using UnityEngine;

public class EnablePlayer : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            MainMenuController.Instance.inputActions.FindActionMap("Player").Enable();
        }
    }
}
