using UnityEngine;
using System; // Action을 사용하기 위해 필요

// IdolMovement.cs
// 아이돌 캐릭터의 2D 이동을 담당하는 클래스입니다.
// Rigidbody2D를 사용하여 물리 기반 이동을 처리합니다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class IdolMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("아이돌의 이동 속도입니다.")]
    public float moveSpeed = 3f;

    [Tooltip("목표 지점에 얼마나 가까워져야 도착한 것으로 간주할지 거리입니다.")]
    public float arrivalThreshold = 0.1f;

    // 내부 참조 변수들
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 currentTargetPosition;
    private bool isMoving = false;

    // 목표 지점 도착 시 호출될 액션(콜백)
    public Action<NavPoint> OnArrivedAtTargetPoint;
    private NavPoint currentTargetNavPointObject; // 현재 이동 목표 NavPoint 객체

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            HandleMovement();
        }
        else
        {
            if (rb.linearVelocity != Vector2.zero)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void HandleMovement()
    {
        // currentTargetNavPointObject가 null인데 isMoving이 true인 비정상적인 상황 방지
        if (currentTargetNavPointObject == null && isMoving)
        {
            Debug.LogWarning(gameObject.name + " - IdolMovement: isMoving이 true이지만 currentTargetNavPointObject가 null입니다. 이동을 중지합니다.");
            StopMovement(); // 안전하게 이동 중지
            return;
        }
        if (!isMoving) return; // 이미 isMoving이 false면 더 이상 처리 안 함


        Vector2 directionToTarget = (currentTargetPosition - (Vector2)transform.position);
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > arrivalThreshold)
        {
            Vector2 moveDirection = directionToTarget.normalized;
            rb.linearVelocity = moveDirection * moveSpeed;
            FlipSprite(moveDirection);
        }
        else
        {
            // 도착 판정, ArrivedAtTarget 호출
            ArrivedAtTarget();
        }
    }

    public void MoveTo(NavPoint targetNavPoint)
    {
        // Debug.Log(gameObject.name + " - IdolMovement: MoveTo 호출됨. 목표: " + (targetNavPoint == null ? "NULL" : targetNavPoint.pointName) + ", 현재 isMoving: " + isMoving + ", 현재 currentTargetObj: " + (currentTargetNavPointObject == null ? "NULL" : currentTargetNavPointObject.pointName));
        if (targetNavPoint == null)
        {
            Debug.LogWarning(gameObject.name + ": 목표 NavPoint가 null입니다. 이동할 수 없습니다.");
            // StopMovement(); // 목표가 null이면 이동을 멈추는 것이 안전할 수 있습니다.
            isMoving = false; // 명시적으로 이동 중이 아님을 표시
            return;
        }
        currentTargetNavPointObject = targetNavPoint;
        currentTargetPosition = targetNavPoint.GetPosition();
        isMoving = true; // 이동 시작 플래그

        Vector2 initialDirection = (currentTargetPosition - (Vector2)transform.position).normalized;
        if (initialDirection.sqrMagnitude > 0.01f)
        {
            FlipSprite(initialDirection);
        }
    }

    private void ArrivedAtTarget()
    {
        // 이 함수에 진입했다는 것은 도착했다는 의미
        // Debug.Log(gameObject.name + " - IdolMovement: ArrivedAtTarget() 내부 진입. isMoving: " + isMoving + ", currentTarget: " + (currentTargetNavPointObject == null ? "NULL" : currentTargetNavPointObject.pointName));

        // 가장 먼저 isMoving을 false로 설정하여 중복 호출 및 의도치 않은 추가 이동 방지
        isMoving = false;
        rb.linearVelocity = Vector2.zero; // 물리적 움직임도 즉시 정지

        NavPoint arrivedPoint = currentTargetNavPointObject; // 콜백으로 전달할 NavPoint를 지역 변수에 저장

        // currentTargetNavPointObject를 null로 설정하는 것은 Invoke 이후에,
        // 그리고 Invoke 과정에서 이 값이 변경되지 않았을 경우에만 수행해야 합니다.
        // 하지만 더 안전한 방법은, Invoke를 한 후에는 이 ArrivedAtTarget의 책임은 끝났다고 보고,
        // 새로운 이동은 반드시 MoveTo를 통해 시작되도록 하는 것입니다.
        // 따라서, 이 도착 이벤트에 대한 목표는 이제 완료된 것으로 간주하고 null로 설정합니다.
        currentTargetNavPointObject = null;

        

        if (arrivedPoint != null)
        {
            // Debug.Log(gameObject.name + " - IdolMovement: OnArrivedAtTargetPoint.Invoke 호출 예정 (" + arrivedPoint.pointName + ")");
            OnArrivedAtTargetPoint?.Invoke(arrivedPoint);
        }
        else
        {
            // 이 로그가 찍히면, ArrivedAtTarget이 호출될 때 currentTargetNavPointObject가 이미 null이었다는 의미.
            // 이는 MoveTo가 null로 호출되었거나, 다른 곳에서 currentTargetNavPointObject가 예기치 않게 null이 된 경우.
            // HandleMovement에 방어 코드를 추가했으므로 이 경우는 줄어들어야 함.
            Debug.LogError(gameObject.name + " - IdolMovement: ArrivedAtTarget에서 Invoke 하려 했으나 arrivedPoint가 null입니다!");
            OnArrivedAtTargetPoint?.Invoke(null); // Idol.cs에서 null을 처리하므로 null로 호출
        }
    }

    public void StopMovement()
    {
        isMoving = false;
        rb.linearVelocity = Vector2.zero;
        currentTargetNavPointObject = null; // 이동 중지 시 현재 목표도 초기화하는 것이 안전할 수 있습니다.
         Debug.Log(gameObject.name + " 이동 중지됨.");
    }

    private void FlipSprite(Vector2 moveDirection)
    {
        if (moveDirection.x > 0.01f && spriteRenderer.flipX)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveDirection.x < -0.01f && !spriteRenderer.flipX)
        {
            spriteRenderer.flipX = true;
        }
    }

    public bool IsCurrentlyMoving()
    {
        return isMoving;
    }
}
