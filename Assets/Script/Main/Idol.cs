using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQ를 사용하기 위해 필요 (예: OrderBy)

// Idol.cs
// 아이돌 캐릭터의 행동, 상태, 데이터를 관리하는 메인 클래스입니다.
// 다층 계단 이동 로직을 포함합니다.
[RequireComponent(typeof(IdolMovement))]
public class Idol : MonoBehaviour
{
    public enum IdolState
    {
        Idle,                   // 대기 상태
        Wandering,              // 배회 상태 (같은 층 내에서만)
        MovingToScheduledTarget // 스케줄에 따른 목표 지점으로 이동 (층간 이동 포함)
    }

    [Header("아이돌 기본 정보")]
    [Tooltip("아이돌의 현재 층 번호입니다.")]
    public int currentFloorID = 1;

    [Header("아이돌 상태")]
    [Tooltip("현재 아이돌의 상태입니다.")]
    public IdolState currentState = IdolState.Idle;

    [Header("배회(Wandering) 설정")]
    [Tooltip("배회 시 다음 목적지 선택 전 대기 시간 (최소)")]
    public float minIdleTimeBeforeWander = 3f;
    [Tooltip("배회 시 다음 목적지 선택 전 대기 시간 (최대)")]
    public float maxIdleTimeBeforeWander = 7f;

    // 내부 변수
    private IdolMovement idolMovement;
    private float currentTimer = 0f;
    private float timeToWait = 0f;

    // 스케줄 관련
    public NavPoint finalScheduledTarget = null;       // 스케줄 시스템으로부터 받은 최종 목표
    private NavPoint intermediateTarget = null;         // 계단 입구 등 중간 경유지
    // private bool needsToUseStairs = false; // 이 변수는 이제 DeterminePath에서 매번 판단하므로 클래스 멤버일 필요는 없을 수 있음
    private bool isCurrentlyUsingStairsProcess = false; // 현재 계단을 '이용'하는 과정 중인가? (순간이동 처리용)

    // NavPoint 관리 (HubManager가 이상적이지만, 여기서는 간단히 처리)
    private static List<NavPoint> allNavPointsInScene = new List<NavPoint>();

    void Awake()
    {
        idolMovement = GetComponent<IdolMovement>();
        if (idolMovement == null)
        {
            Debug.LogError(gameObject.name + "에서 IdolMovement 컴포넌트를 찾을 수 없습니다!");
            enabled = false; // 스크립트 비활성화
            return;
        }

        idolMovement.OnArrivedAtTargetPoint += HandleArrivalAtNavPoint;

        if (allNavPointsInScene.Count == 0)
        {
            NavPoint[] foundNavPoints = FindObjectsByType<NavPoint>(FindObjectsSortMode.None);
            if (foundNavPoints != null && foundNavPoints.Length > 0)
            {
                allNavPointsInScene.AddRange(foundNavPoints);
                Debug.Log(allNavPointsInScene.Count + "개의 NavPoint를 씬에서 찾았습니다.");
            }
        }
    }

    void Start()
    {
        SetNewWaitTimeForCurrentState();
    }

    void Update()
    {
        if (isCurrentlyUsingStairsProcess)
        {
            return;
        }

        currentTimer += Time.deltaTime;

        switch (currentState)
        {
            case IdolState.Idle:
                HandleIdleState();
                break;
            case IdolState.Wandering:
                HandleWanderingState();
                break;
            case IdolState.MovingToScheduledTarget:
                HandleMovingToScheduledTargetState();
                break;
        }
    }

    void HandleIdleState()
    {
        if (idolMovement.IsCurrentlyMoving()) idolMovement.StopMovement();

        if (currentTimer >= timeToWait)
        {
            ChangeState(IdolState.Wandering);
        }
    }

    void HandleWanderingState()
    {
        if (!idolMovement.IsCurrentlyMoving() && intermediateTarget == null)
        {
            NavPoint wanderTarget = GetRandomNavPointOnCurrentFloor();
            if (wanderTarget != null)
            {
                intermediateTarget = wanderTarget;
                idolMovement.MoveTo(intermediateTarget);
            }
            else
            {
                ChangeState(IdolState.Idle);
            }
        }
    }

    void HandleMovingToScheduledTargetState()
    {
        if (finalScheduledTarget == null)
        {
            ChangeState(IdolState.Idle);
            return;
        }

        if (!idolMovement.IsCurrentlyMoving() && intermediateTarget == null && !isCurrentlyUsingStairsProcess)
        {
            // 최종 목표에 이미 도달했는지 먼저 확인
            if (currentFloorID == finalScheduledTarget.floorID && Vector2.Distance(transform.position, finalScheduledTarget.GetPosition()) < idolMovement.arrivalThreshold)
            {
                Debug.Log(gameObject.name + ": 이미 최종 스케줄 목표(" + finalScheduledTarget.pointName + ")에 매우 가까이 있거나 도착했습니다!");
                HandleArrivalAtFinalTarget();
                return;
            }
            DeterminePathToScheduledTarget();
        }
    }

    void DeterminePathToScheduledTarget()
    {
        if (finalScheduledTarget == null) return;

        if (finalScheduledTarget.floorID == currentFloorID)
        {
            // 목표가 현재 층에 있음
            intermediateTarget = finalScheduledTarget;
            Debug.Log(gameObject.name + ": 최종 목표(" + finalScheduledTarget.pointName + ")가 현재 " + currentFloorID + "층에 있습니다. 이동 시작.");
            idolMovement.MoveTo(intermediateTarget);
        }
        else
        {
            // 목표가 다른 층에 있음: 다음 층으로 가는 계단 탐색
            Debug.Log(gameObject.name + ": 최종 목표(" + finalScheduledTarget.pointName + ")는 " + finalScheduledTarget.floorID + "층. 현재는 " + currentFloorID + "층. 계단 탐색.");
            NavPoint nextStairEntrance = FindNextStairTowardsTargetFloor(finalScheduledTarget.floorID);

            if (nextStairEntrance != null)
            {
                intermediateTarget = nextStairEntrance;
                Debug.Log(gameObject.name + ": 다음 계단 입구(" + intermediateTarget.pointName + ", " + intermediateTarget.floorID + "층)로 이동 시작. (목표 층: " + finalScheduledTarget.floorID + ")");
                idolMovement.MoveTo(intermediateTarget);
            }
            else
            {
                Debug.LogWarning(gameObject.name + ": " + currentFloorID + "층에서 " + finalScheduledTarget.floorID + "층 방향으로 가는 계단을 찾을 수 없습니다! 스케줄 중단. Idle 상태로 전환.");
                finalScheduledTarget = null;
                ChangeState(IdolState.Idle);
            }
        }
    }

    NavPoint FindNextStairTowardsTargetFloor(int ultimateTargetFloorID)
    {
        List<NavPoint> candidateStairs = new List<NavPoint>();
        bool goingUp = ultimateTargetFloorID > currentFloorID;

        foreach (NavPoint navPoint in allNavPointsInScene)
        {
            if (navPoint.floorID == currentFloorID && navPoint.isStair && navPoint.connectedStairPoint != null)
            {
                if (goingUp && (navPoint.stairDirection == NavPoint.StairDirection.Up || navPoint.connectedStairPoint.floorID > currentFloorID))
                {
                    // 올라가는 계단이거나, 연결된 지점이 현재 층보다 위층일 경우
                    candidateStairs.Add(navPoint);
                }
                else if (!goingUp && (navPoint.stairDirection == NavPoint.StairDirection.Down || navPoint.connectedStairPoint.floorID < currentFloorID))
                {
                    // 내려가는 계단이거나, 연결된 지점이 현재 층보다 아래층일 경우
                    candidateStairs.Add(navPoint);
                }
            }
        }

        if (candidateStairs.Count > 0)
        {
            // 가장 가까운 적절한 계단 선택
            return candidateStairs.OrderBy(stair => Vector2.Distance(transform.position, stair.GetPosition())).FirstOrDefault();
        }
        return null;
    }

    void HandleArrivalAtNavPoint(NavPoint arrivedPoint)
    {
        if (arrivedPoint == null) return; // 혹시 모를 null 체크

        // Debug.Log(gameObject.name + "이(가) " + arrivedPoint.pointName + " ("+ arrivedPoint.floorID +"층)에 도착 (Idol.cs에서 알림 받음)");

        if (currentState == IdolState.Wandering)
        {
            intermediateTarget = null;
            ChangeState(IdolState.Idle);
        }
        else if (currentState == IdolState.MovingToScheduledTarget)
        {
            if (arrivedPoint == intermediateTarget) // 현재 설정된 중간 목표에 도착
            {
                if (arrivedPoint.isStair && arrivedPoint.connectedStairPoint != null && arrivedPoint.floorID == currentFloorID) // 현재 층의 계단 입구에 도착
                {
                    // 계단 입구에 도착한 경우: 층 변경 처리
                    Debug.Log(gameObject.name + ": 계단 입구(" + arrivedPoint.pointName + ") 도착. 층 변경 실행.");
                    isCurrentlyUsingStairsProcess = true;

                    NavPoint exitStair = arrivedPoint.connectedStairPoint;
                    transform.position = exitStair.GetPosition();
                    currentFloorID = exitStair.floorID;
                    Debug.Log(gameObject.name + ": " + currentFloorID + "층 (" + exitStair.pointName + ")으로 이동 완료.");

                    intermediateTarget = null; // 중간 목표(계단 입구) 완료
                    isCurrentlyUsingStairsProcess = false;

                    // 새로운 층에 도착했으므로, 다음 경로를 즉시 결정 (Update 루프를 기다리지 않고)
                    // 최종 목표에 도달했는지, 아니면 또 다른 계단을 타야 하는지 등
                    if (finalScheduledTarget != null && currentFloorID == finalScheduledTarget.floorID)
                    {
                        // 최종 목표와 같은 층에 도착했으니, 최종 목표로 바로 이동 설정
                        Debug.Log(gameObject.name + ": 최종 목표와 같은 층에 도착. 최종 목표로 경로 설정.");
                        DeterminePathToScheduledTarget();
                    }
                    else if (finalScheduledTarget != null)
                    {
                        // 아직 최종 목표 층이 아님, 다음 계단 찾아야 함
                        Debug.Log(gameObject.name + ": 아직 최종 목표 층이 아님. 다음 경로 결정.");
                        DeterminePathToScheduledTarget();
                    }
                    else // 최종 목표가 없는 이상한 상황 (스케줄 취소 등)
                    {
                        ChangeState(IdolState.Idle);
                    }
                }
                else if (arrivedPoint == finalScheduledTarget)
                {
                    // 최종 목표 지점에 도착한 경우
                    HandleArrivalAtFinalTarget();
                }
                else
                {
                    // 계단이 아닌 다른 중간 지점 (현재 로직에서는 발생하기 어려움)
                    Debug.LogWarning(gameObject.name + ": 예상치 못한 중간 지점 " + arrivedPoint.pointName + "에 도착. 최종 목표 확인.");
                    intermediateTarget = null;
                    // 경로 재설정 시도
                    if (finalScheduledTarget != null) DeterminePathToScheduledTarget(); else ChangeState(IdolState.Idle);
                }
            }
        }
    }

    void HandleArrivalAtFinalTarget()
    {
        Debug.Log(gameObject.name + ": 최종 스케줄 목표(" + (finalScheduledTarget != null ? finalScheduledTarget.pointName : "알 수 없음") + ")에 도착!");
        finalScheduledTarget = null;
        intermediateTarget = null;
        ChangeState(IdolState.Idle);
    }


    void SetNewWaitTimeForCurrentState()
    {
        currentTimer = 0f;
        if (currentState == IdolState.Idle)
        {
            timeToWait = Random.Range(minIdleTimeBeforeWander, maxIdleTimeBeforeWander);
        }
    }

    void ChangeState(IdolState newState)
    {
        if (currentState == newState && !(newState == IdolState.Idle && finalScheduledTarget != null))
        {
            if (currentState == newState && currentState != IdolState.Idle) return;
        }

        currentState = newState;
        SetNewWaitTimeForCurrentState();

        if (newState == IdolState.Idle || newState == IdolState.Wandering)
        {
            intermediateTarget = null;
        }
    }

    NavPoint GetRandomNavPointOnCurrentFloor()
    {
        List<NavPoint> navPointsOnFloor = allNavPointsInScene.Where(p => p.floorID == currentFloorID && !p.isStair).ToList();
        if (navPointsOnFloor.Count > 0)
        {
            int randomIndex = Random.Range(0, navPointsOnFloor.Count);
            return navPointsOnFloor[randomIndex];
        }
        return null;
    }

    public void AssignScheduledTarget(NavPoint targetNavPoint)
    {
        if (targetNavPoint == null)
        {
            Debug.LogWarning(gameObject.name + ": 할당된 스케줄 목표가 null입니다.");
            finalScheduledTarget = null;
            ChangeState(IdolState.Idle);
            return;
        }

        Debug.Log(gameObject.name + ": 새로운 스케줄 목표 할당됨 - " + targetNavPoint.pointName + " (" + targetNavPoint.floorID + "층)");
        finalScheduledTarget = targetNavPoint;
        intermediateTarget = null;
        ChangeState(IdolState.MovingToScheduledTarget);
    }

    void OnDisable()
    {
        if (idolMovement != null)
        {
            idolMovement.OnArrivedAtTargetPoint -= HandleArrivalAtNavPoint;
        }
    }
}
