using UnityEngine;

public class SpinningSurface : MonoBehaviour
{
    [SerializeField]
    private float spinSpeed = 25;

    void Update()
    {
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
    }
}
