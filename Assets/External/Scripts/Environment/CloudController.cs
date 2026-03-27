using UnityEngine;

public class CloudController : MonoBehaviour
{
    [SerializeField] private float speed = 2.0f;
    private Vector3 moveDirection;
    private float boundsX = 510f; // World Bounds 보다 조금 크게 설정하여 완전히 나간 뒤 파괴
    private float boundsY = 510f;

    public void Initialize(Vector3 dir, float customSpeed)
    {
        moveDirection = dir.normalized;
        speed = customSpeed;
    }

    private void Update()
    {
        // 계획 페이즈(Paused)에서는 구름도 멈추게 하려면 아래 주석 해제
        // if (GameManager.Instance.CurrentPhase == GamePhase.Paused) return;

        transform.position += moveDirection * speed * Time.deltaTime;

        CheckOutOfBounds();
    }

    private void CheckOutOfBounds()
    {
        // 월드맵(50f) 밖으로 완전히 벗어나면 스스로 파괴
        if (Mathf.Abs(transform.position.x) > boundsX || Mathf.Abs(transform.position.y) > boundsY)
        {
            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.RepositionCloud(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}