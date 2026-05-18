using UnityEngine;

[CreateAssetMenu(menuName = "Scene Data", fileName = "New Scene Data")]
public class SceneDataSO : ScriptableObject
{
    public string UniqueName;
    public string levelDescription;
    public int SceneIndex;
    public int[] scoreBreakpoints = new int[3];
}
