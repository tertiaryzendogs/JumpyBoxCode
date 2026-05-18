using UnityEngine;

public class SceneData : MonoBehaviour
{
    public SceneDataSO Data;

    private void Awake()
    {
        MainMenuController.Instance.SceneData = this;
    }
}
