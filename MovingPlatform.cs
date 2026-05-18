using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    #region variables
    [SerializeField]
    private float speed = 1;
    [SerializeField]
    private Transform pointA;
    [SerializeField]
    private Transform pointB;
    [SerializeField]
    private bool waitAfterMove = false;
    [SerializeField]
    private float waitTime = 1f;
    [SerializeField]
    private bool parent = true;
    private bool waiting = false;
    private Vector3 nextPosition;
    #endregion

    private void Start()
    {
        nextPosition = pointB.position;
    }

    void Update()
    {
        if (waiting) return;
        transform.position = Vector3.MoveTowards(transform.position, nextPosition, speed * Time.deltaTime);
        if (transform.position == nextPosition)
        {
            if (waitAfterMove && transform.position == pointB.position)
            {
                StartCoroutine(WaitBeforeMoving());
            }
            else
            {
                nextPosition = (nextPosition == pointA.position) ? pointB.position : pointA.position;
            }
        }
    }

    IEnumerator WaitBeforeMoving()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        waiting = false;
        nextPosition = (nextPosition == pointA.position) ? pointB.position : pointA.position;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && parent)
        {
            collision.gameObject.transform.parent = transform;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && parent)
        {
            collision.gameObject.transform.parent = null;
        }
    }
}
