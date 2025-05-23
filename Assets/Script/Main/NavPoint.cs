using UnityEngine;

// NavPoint.cs
// 허브 공간 내 특정 지점을 나타내는 클래스입니다.
// 아이돌이 이동하거나 상호작용할 목표 지점으로 사용됩니다.
public class NavPoint : MonoBehaviour
{
    public enum PointType
    {
        General,
        Desk,
        Sofa,
        PracticeVocal,
        PracticeDance,
        RestingSpot,
        StressRelief,
        RuleManifestSpot,
        Stair // 계단 타입 추가
    }

    [Header("NavPoint 기본 설정")]
    [Tooltip("이 NavPoint의 이름입니다. (에디터에서 식별용)")]
    public string pointName = "새로운 지점";

    [Tooltip("이 NavPoint의 타입입니다.")]
    public PointType type = PointType.General;

    [Tooltip("이 NavPoint가 속한 층 번호입니다. (예: 1층, 2층)")]
    public int floorID = 1;

    [Header("계단 설정 (Type이 Stair일 경우)")]
    [Tooltip("이 NavPoint가 계단인지 여부입니다. type을 Stair로 설정하면 자동으로 true가 되도록 할 수 있습니다.")]
    public bool isStair = false; // Type이 Stair면 true로 간주

    [Tooltip("연결된 반대편 계단 NavPoint입니다. (이 계단을 통해 도달하는 다른 층의 계단 지점)")]
    public NavPoint connectedStairPoint;

    // (선택적) 올라가는 계단인지 내려가는 계단인지
    public enum StairDirection { None, Up, Down }
    [Tooltip("계단의 방향입니다 (올라가는 계단 / 내려가는 계단).")]
    public StairDirection stairDirection = StairDirection.None;


    void OnValidate()
    {
        // Inspector에서 Type을 Stair로 변경하면 isStair를 true로 자동 설정 (편의 기능)
        if (type == PointType.Stair)
        {
            isStair = true;
        }
        else
        {
            isStair = false;
            // 계단이 아니면 연결된 계단 포인트나 방향은 의미 없음
            connectedStairPoint = null;
            stairDirection = StairDirection.None;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.3f);
        if (isStair && connectedStairPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, connectedStairPoint.transform.position);
        }
    }

    void OnDrawGizmos()
    {
        switch (type)
        {
            case PointType.Stair:
                Gizmos.color = Color.magenta;
                break;
            case PointType.RestingSpot:
                Gizmos.color = Color.green;
                break;
            case PointType.RuleManifestSpot:
                Gizmos.color = Color.red;
                break;
            default:
                Gizmos.color = Color.gray;
                break;
        }
        Gizmos.DrawSphere(transform.position, 0.2f);
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }
}
