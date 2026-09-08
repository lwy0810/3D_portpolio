using UnityEngine;

/// <summary>
/// 전투가 벌어지는 원형 영역. 회색 테두리로 표시하고, 유닛이 그 밖으로 나가지 못하게 한다.
///
/// 인플레이스 전투는 필드 어디서나 시작되므로 "여기까지가 전장" 이라는 경계가 없으면
/// 공격 이동을 반복하는 사이에 유닛이 지형 밖으로 흘러나간다.
/// 예전 전투 전용 씬에서는 Plane 하나가 그 역할을 암묵적으로 했다.
///
/// 테두리는 LineRenderer 로 그린다. 텍스처가 필요 없고, 지형 경사에 맞춰
/// 각 점의 높이를 따로 잡을 수 있어 원이 땅에 묻히지 않는다.
/// </summary>
public class BattleArea : MonoBehaviour
{
    private const string ObjectName = "BattleArea";
    private const int Segments = 72;

    private static BattleArea _instance;

    [Header("모양")]
    [Tooltip("테두리 굵기(m)")]
    [SerializeField] private float _lineWidth = 0.12f;
    [Tooltip("테두리 색")]
    [SerializeField] private Color _lineColor = new Color(0.72f, 0.72f, 0.72f, 0.85f);
    [Tooltip("지면에서 띄우는 높이(m). 0 이면 땅에 묻혀 끊겨 보인다")]
    [SerializeField] private float _groundLift = 0.06f;

    [Header("지면 추적")]
    [Tooltip("테두리 각 점의 높이를 지형에 맞춘다. 끄면 중심 높이로 평평하게 그린다")]
    [SerializeField] private bool _followGround = true;
    [SerializeField] private float _probeUp = 4.0f;
    [SerializeField] private float _probeDown = 12.0f;

    private LineRenderer _line;
    private Vector3 _center;
    private float _radius;
    private bool _active;

    public static BattleArea Instance => _instance;

    /// <summary>영역이 표시 중인지.</summary>
    public static bool IsActive => _instance != null && _instance._active;

    public static Vector3 Center => _instance != null ? _instance._center : Vector3.zero;
    public static float Radius => _instance != null ? _instance._radius : 0.0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>씬에 없으면 만들어서 돌려준다.</summary>
    public static BattleArea Ensure()
    {
        if (_instance != null) return _instance;

        BattleArea _found = FindFirstObjectByType<BattleArea>(FindObjectsInactive.Include);
        if (_found != null)
        {
            _instance = _found;
            return _instance;
        }

        GameObject _obj = new GameObject(ObjectName);
        _instance = _obj.AddComponent<BattleArea>();
        return _instance;
    }

    /// <summary>전투 영역을 지정한 중심과 반지름으로 표시한다.</summary>
    public static void Show(Vector3 center, float radius)
    {
        BattleArea _area = Ensure();
        if (_area == null) return;

        _area._center = center;
        _area._radius = Mathf.Max(1.0f, radius);
        _area._active = true;
        _area.Redraw();
    }

    public static void Hide()
    {
        if (_instance == null) return;

        _instance._active = false;
        if (_instance._line != null) _instance._line.enabled = false;
    }

    /// <summary>
    /// 영역 밖이면 경계 안쪽으로 끌어당긴 위치를 돌려준다. 영역이 없으면 그대로 통과.
    /// Y 는 건드리지 않는다 — 높이는 중력과 지면 판정이 결정한다.
    /// </summary>
    public static Vector3 ClampToArea(Vector3 pos)
    {
        if (!IsActive) return pos;

        BattleArea _a = _instance;
        return ClampToCircle(pos, _a._center, _a._radius - _a._lineWidth);
    }

    /// <summary>
    /// 원 안으로 당기는 순수 계산. Y 는 그대로 둔다 — 높이는 중력과 지면이 결정한다.
    /// 테두리 두께만큼 안쪽으로 당겨야 선을 밟고 서지 않는다.
    /// </summary>
    public static Vector3 ClampToCircle(Vector3 pos, Vector3 center, float limit)
    {
        if (limit <= 0.0f) limit = 0.0f;

        Vector3 _flat = pos - center;
        _flat.y = 0.0f;

        float _dist = _flat.magnitude;

        // 중심과 정확히 겹치면 방향을 정할 수 없다. 그 자리는 어차피 영역 안이다
        if (_dist <= limit || _dist < 0.0001f) return pos;

        Vector3 _pulled = center + _flat.normalized * limit;
        _pulled.y = pos.y;
        return _pulled;
    }

    /// <summary>영역 안인지.</summary>
    public static bool Contains(Vector3 pos)
    {
        if (!IsActive) return true;

        Vector3 _flat = pos - _instance._center;
        _flat.y = 0.0f;
        return _flat.magnitude <= _instance._radius;
    }

    // ── 그리기 ──────────────────────────────────────────────

    private void Redraw()
    {
        EnsureLine();
        if (_line == null) return;

        _line.enabled = true;
        _line.positionCount = Segments;

        for (int i = 0; i < Segments; i++)
        {
            float _rad = i / (float)Segments * Mathf.PI * 2.0f;
            Vector3 _p = _center + new Vector3(Mathf.Cos(_rad) * _radius, 0.0f, Mathf.Sin(_rad) * _radius);

            _p.y = _followGround ? GroundY(_p, _center.y) : _center.y;
            _p.y += _groundLift;

            _line.SetPosition(i, _p);
        }
    }

    private void EnsureLine()
    {
        if (_line != null) return;

        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.useWorldSpace = true;
        _line.loop = true;
        _line.startWidth = _lineWidth;
        _line.endWidth = _lineWidth;
        _line.startColor = _lineColor;
        _line.endColor = _lineColor;
        _line.numCapVertices = 2;
        _line.numCornerVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;

        _line.material = MakeLineMaterial(_lineColor);
    }

    /// <summary>
    /// URP 프로젝트라 빌트인 셰이더 이름이 없을 수 있다. 있는 것부터 차례로 시도한다.
    /// 하나도 못 찾으면 경고만 남기고 기본 머티리얼로 둔다 — 여기서 예외를 내면
    /// 전투가 시작되지 않는다.
    /// </summary>
    private static Material MakeLineMaterial(Color color)
    {
        string[] _candidates =
        {
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "Unlit/Color",
            "Legacy Shaders/Particles/Alpha Blended"
        };

        for (int i = 0; i < _candidates.Length; i++)
        {
            Shader _shader = Shader.Find(_candidates[i]);
            if (_shader == null) continue;

            Material _mat = new Material(_shader);

            // 프로퍼티 이름이 셰이더마다 달라서 있는 것만 넣는다
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", color);
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", color);

            return _mat;
        }

        Debug.LogWarning("[BattleArea] 테두리에 쓸 셰이더를 찾지 못했습니다. " +
                         "선이 분홍색으로 보이면 LineRenderer 의 Material 을 직접 지정하세요.");
        return null;
    }

    private float GroundY(Vector3 pos, float fallbackY)
    {
        Ray _ray = new Ray(pos + Vector3.up * _probeUp, Vector3.down);
        RaycastHit[] _hits = Physics.RaycastAll(_ray, _probeUp + _probeDown, ~0,
                                                QueryTriggerInteraction.Ignore);

        float _best = 0.0f;
        float _bestDist = float.MaxValue;
        bool _found = false;

        for (int i = 0; i < _hits.Length; i++)
        {
            Collider _col = _hits[i].collider;
            if (_col == null) continue;
            if (_col.GetComponent<CharacterController>() != null) continue;
            if (_col.GetComponentInParent<Unit>() != null) continue;

            if (_hits[i].distance < _bestDist)
            {
                _bestDist = _hits[i].distance;
                _best = _hits[i].point.y;
                _found = true;
            }
        }

        return _found ? _best : fallbackY;
    }
}
