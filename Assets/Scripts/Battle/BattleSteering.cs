using UnityEngine;

/// <summary>
/// 전투 중 이동 경로에서 다른 유닛을 피해 돌아가는 계산. 순수 계산만 하며 씬을 만지지 않는다.
///
/// 왜 필요한가:
/// 공격 이동은 대상까지 직선으로 간다. 그 직선 위에 동료가 서 있으면
/// CharacterController 가 상대 캡슐의 둥근 면을 타고 올라가, 동료 머리를 밟고
/// 넘어가는 것처럼 보인다. StepOffset 이 0.3 이라 계단처럼 오르는 것은 아니지만
/// 캡슐 측면이 곡면이라 밀고 들어가면 위로 미끄러진다.
///
/// 어떻게 푸는가:
/// 각 유닛 주위에 "들어가면 안 되는 원"(자기 반지름 + 상대 반지름 + 여유)을 두고,
/// 그 원에 접하는 방향(접선)들을 후보로 만든다. 후보 중 모든 유닛을 비켜 가면서
/// 목적지 방향에서 가장 덜 벗어나는 것을 고른다.
///
/// 처음에는 "가장 가까운 하나의 옆을 목표로 삼는" 방식으로 만들었는데,
/// 경로가 곡선이라 코너를 잘라 먹으면서 실제로는 원 안으로 들어갔다
/// (시뮬레이션에서 반지름 1 몬스터와 0.059m 겹침). 동료가 둘 나란히 있으면
/// 서로에게 밀려 목적지에 도달하지 못했다. 접선 방식은 원 밖을 보장한다.
///
/// 계산을 여기로 떼어두면 Unity 없이 경로를 끝까지 걸어보며 검증할 수 있다.
/// </summary>
public static class BattleSteering
{
    public struct Obstacle
    {
        public Vector3 Position;
        public float Radius;
    }

    /// <summary>기본 전방 탐색 거리(m). 이보다 먼 유닛은 아직 신경 쓰지 않는다.</summary>
    public const float DefaultLookAhead = 5.0f;

    /// <summary>기본 여유 간격(m). 두 반지름 합에 이만큼 더 떨어져 지나간다.</summary>
    public const float DefaultClearance = 0.35f;

    /// <summary>후보 방향 최대 개수 = 직진 1 + 유닛당 접선 2개.</summary>
    private const int MaxObstacles = 12;

    private static readonly Vector3[] _candidates = new Vector3[1 + MaxObstacles * 2];

    /// <summary>접선 판정용 허용치(m).</summary>
    private const float ClearEpsilon = 0.001f;

    /// <summary>
    /// 목적지로 향하는 방향. 경로를 막는 유닛이 있으면 비켜 가는 방향을 돌려준다.
    /// </summary>
    /// <returns>정규화된 수평 방향. 목적지가 현재 위치와 같으면 Vector3.zero.</returns>
    public static Vector3 Steer(Vector3 pos, Vector3 destination,
                                Obstacle[] obstacles, int count,
                                float selfRadius, float lookAhead, float clearance)
    {
        Vector3 _toDest = Flatten(destination - pos);

        if (_toDest.sqrMagnitude < 0.000001f) return Vector3.zero;

        Vector3 _desired = _toDest.normalized;

        if (obstacles == null || count <= 0) return _desired;
        if (count > obstacles.Length) count = obstacles.Length;
        if (count > MaxObstacles) count = MaxObstacles;

        // 판정 거리는 목적지까지 또는 탐색 거리 중 짧은 쪽.
        // 목적지 뒤에 있는 유닛은 어차피 그 전에 멈추므로 상관없다
        float _range = Mathf.Min(lookAhead, _toDest.magnitude);

        // ── 후보 방향 모으기 ────────────────────────────────
        int _n = 0;
        _candidates[_n++] = _desired;

        for (int i = 0; i < count; i++)
        {
            Vector3 _rel = Flatten(obstacles[i].Position - pos);
            float _dist = _rel.magnitude;
            float _need = selfRadius + obstacles[i].Radius + clearance;

            if (_dist < 0.0001f) continue;   // 나와 겹쳐 있으면 방향을 정할 수 없다

            Vector3 _base = _rel / _dist;

            if (_dist <= _need)
            {
                // 이미 원 안에 있다. 벗어나는 방향은 접선이 아니라 수직이다
                Vector3 _perp = Vector3.Cross(Vector3.up, _base);
                _candidates[_n++] = _perp;
                _candidates[_n++] = -_perp;
                continue;
            }

            // 원에 접하는 각도. 이 방향으로 가면 원 밖을 스쳐 지난다
            float _theta = Mathf.Asin(_need / _dist);
            _candidates[_n++] = RotateY(_base, _theta);
            _candidates[_n++] = RotateY(_base, -_theta);
        }

        // ── 후보 평가 ───────────────────────────────────────
        //
        // 모든 유닛을 비켜 가는 후보 중 목적지 방향에서 가장 덜 벗어난 것을 고른다.
        // 비켜 갈 수 있는 후보가 없으면(사이가 너무 좁으면) 가장 덜 겹치는 것을 고른다.
        Vector3 _best = _desired;
        float _bestAlign = float.MinValue;
        bool _foundClear = false;

        Vector3 _fallback = _desired;
        float _fallbackClear = float.MinValue;
        float _fallbackAlign = float.MinValue;

        for (int c = 0; c < _n; c++)
        {
            Vector3 _cand = _candidates[c];
            if (_cand.sqrMagnitude < 0.000001f) continue;

            _cand = _cand.normalized;

            // 뒤로 가는 후보는 버린다. 접근이 아니라 후퇴가 된다
            float _align = Vector3.Dot(_cand, _desired);
            if (_align <= 0.0f) continue;

            float _minClear = MinClearance(pos, _cand, obstacles, count, selfRadius, clearance, _range);

            // 접선은 정확히 여유 0 으로 스친다. 부동소수 오차로 음수가 나올 수 있어
            // 아주 작은 허용치를 둔다. 이게 없으면 모든 접선이 "막힘" 으로 판정된다
            if (_minClear >= -ClearEpsilon)
            {
                _foundClear = true;
                if (_align > _bestAlign)
                {
                    _bestAlign = _align;
                    _best = _cand;
                }
            }
            else if (!_foundClear)
            {
                // 겹침이 가장 적은 쪽. 같으면 목적지 방향에 가까운 쪽
                if (_minClear > _fallbackClear ||
                    (Mathf.Abs(_minClear - _fallbackClear) < 0.001f && _align > _fallbackAlign))
                {
                    _fallbackClear = _minClear;
                    _fallbackAlign = _align;
                    _fallback = _cand;
                }
            }
        }

        return _foundClear ? _best : _fallback;
    }

    public static Vector3 Steer(Vector3 pos, Vector3 destination,
                                Obstacle[] obstacles, int count, float selfRadius)
    {
        return Steer(pos, destination, obstacles, count, selfRadius,
                     DefaultLookAhead, DefaultClearance);
    }

    /// <summary>
    /// dir 로 range 만큼 갈 때 유닛들과의 최소 여유. 음수면 그만큼 겹친다.
    /// </summary>
    public static float MinClearance(Vector3 pos, Vector3 dir,
                                     Obstacle[] obstacles, int count,
                                     float selfRadius, float clearance, float range)
    {
        Vector3 _right = Vector3.Cross(Vector3.up, dir);
        float _min = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Vector3 _rel = Flatten(obstacles[i].Position - pos);

            float _ahead = Vector3.Dot(_rel, dir);
            if (_ahead <= 0.0f) continue;      // 뒤에 있다
            if (_ahead > range) continue;      // 이번 구간에서는 만나지 않는다

            float _need = selfRadius + obstacles[i].Radius + clearance;
            float _lateral = Mathf.Abs(Vector3.Dot(_rel, _right));

            float _clear = _lateral - _need;
            if (_clear < _min) _min = _clear;
        }

        return _min == float.MaxValue ? float.MaxValue : _min;
    }

    /// <summary>수평 벡터를 Y 축 기준으로 회전한다.</summary>
    public static Vector3 RotateY(Vector3 v, float radians)
    {
        float _c = Mathf.Cos(radians);
        float _s = Mathf.Sin(radians);
        return new Vector3(v.x * _c + v.z * _s, 0.0f, -v.x * _s + v.z * _c);
    }

    /// <summary>Y 를 버려 수평면에서만 계산한다. 경사에서 방향이 위아래로 기울지 않게 한다.</summary>
    public static Vector3 Flatten(Vector3 v)
    {
        v.y = 0.0f;
        return v;
    }
}
