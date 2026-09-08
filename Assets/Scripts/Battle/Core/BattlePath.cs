using UnityEngine;

/// <summary>
/// 이동 전에 미리 계산하는 경로. 순수 계산만 하며 씬을 만지지 않는다.
///
/// 왜 프레임마다 방향을 고르는 방식을 버렸는가:
/// 처음에는 매 프레임 "지금 이 순간 어느 쪽으로 틀어야 하나" 를 계산했다.
/// 각 프레임의 판단은 옳아도 그 결과가 모이면 경로가 곡선이 되어 코너를 잘라먹고,
/// 목적지가 매 프레임 재계산되면서 방향이 계속 미세하게 바뀐다. 결국 유닛에
/// 몸을 비비며 밀고 들어가게 된다.
///
/// 그래서 출발 전에 경로 전체를 확정한다. 시작점에서 목적지까지의 선분을 막는
/// 유닛을 찾고, 그 옆을 지나는 경유지를 넣고, 잘린 두 구간에 같은 판단을 다시 적용한다.
/// 결과는 꺾인 직선들(폴리라인)이고, 각 직선은 모든 유닛의 "진입 금지 원" 밖에 있다.
/// 이동은 그 직선을 순서대로 따라가기만 한다.
///
/// 계산을 여기로 떼어두면 Unity 없이 경로를 끝까지 걸어보며 최소 여유를 재서 검증할 수 있다.
/// </summary>
public static class BattlePath
{
    public struct Obstacle
    {
        public Vector3 Position;
        public float Radius;
    }

    /// <summary>기본 여유 간격(m). 두 반지름 합에 이만큼 더 떨어져 지나간다.</summary>
    public const float DefaultClearance = 0.45f;

    /// <summary>
    /// 경유지를 몇 번까지 쪼개 넣을지.
    /// 깊이 d 는 최대 2^d 개를 쓰므로, 결과 배열 크기(8)에 맞춰 3 으로 둔다.
    /// </summary>
    private const int MaxDepth = 3;

    /// <summary>선분이 원을 스치는지 판정할 때의 허용치(m).</summary>
    private const float BlockEpsilon = 0.001f;

    private const int MaxObstacles = 16;

    /// <summary>
    /// 계획에 실제로 쓰는 유닛 목록. 매번 새로 할당하지 않도록 재사용한다.
    /// 재귀 중에는 바꾸지 않으므로 공유해도 안전하다.
    /// </summary>
    private static readonly Obstacle[] _effective = new Obstacle[MaxObstacles];

    /// <summary>
    /// start 에서 goal 까지의 경로를 계산한다.
    /// </summary>
    /// <param name="outWaypoints">결과를 담을 배열. start 는 포함하지 않고, 마지막이 goal 이다</param>
    /// <returns>기록된 경유지 수. 최소 1 (goal)</returns>
    public static int Plan(Vector3 start, Vector3 goal,
                           Obstacle[] obstacles, int count,
                           float selfRadius, float clearance,
                           Vector3[] outWaypoints)
    {
        if (outWaypoints == null || outWaypoints.Length == 0) return 0;

        if (obstacles == null) count = 0;
        else if (count > obstacles.Length) count = obstacles.Length;

        // 출발점이나 목적지를 이미 품고 있는 유닛은 피할 방법이 없으므로 제외한다.
        //
        // 이 판정을 재귀 안에서 하면 안 된다. 중간 경유지도 "목적지" 로 취급되어,
        // 다른 유닛의 원 안에 놓인 경유지가 "막힘 없음" 으로 통과했다.
        // (동료 둘이 나란히 선 경우 경유지가 한쪽 원 안에 박혀 0.732m 겹침)
        int _count = 0;

        for (int i = 0; i < count && _count < _effective.Length; i++)
        {
            float _need = selfRadius + obstacles[i].Radius + clearance;

            if (Flatten(obstacles[i].Position - start).magnitude < _need) continue;
            if (Flatten(obstacles[i].Position - goal).magnitude < _need) continue;

            _effective[_count++] = obstacles[i];
        }

        int _n = PlanInto(start, goal, _effective, _count, selfRadius, clearance,
                          outWaypoints, 0, MaxDepth);

        // 어떤 경우에도 목적지가 마지막이어야 한다.
        //
        // 재귀가 배열을 다 쓰고 잘리면 마지막이 경유지로 끝날 수 있다.
        // 그 상태로 이동하면 목적지에 도달하지 못한 채 멈춘다.
        if (_n == 0)
        {
            outWaypoints[0] = goal;
            _n = 1;
        }
        else if (Flatten(outWaypoints[_n - 1] - goal).sqrMagnitude > 0.0001f)
        {
            if (_n < outWaypoints.Length) outWaypoints[_n++] = goal;
            else outWaypoints[_n - 1] = goal;
        }

        return _n;
    }

    private static int PlanInto(Vector3 a, Vector3 b,
                                Obstacle[] obs, int count,
                                float selfR, float clear,
                                Vector3[] outW, int written, int depth)
    {
        if (written >= outW.Length) return written;

        // 더 쪼갤 수 없으면 직선으로 간다. 여기까지 오면 좁은 틈을 지나는 경우다
        if (depth <= 0)
        {
            outW[written++] = b;
            return written;
        }

        int _block = FirstBlocker(a, b, obs, count, selfR, clear);

        if (_block < 0)
        {
            outW[written++] = b;
            return written;
        }

        // 막는 유닛의 양옆 두 지점을 후보로 삼는다
        Vector3 _w1 = Detour(a, b, obs[_block], selfR, clear, 1.0f);
        Vector3 _w2 = Detour(a, b, obs[_block], selfR, clear, -1.0f);

        // 남은 막힘이 적은 쪽을 먼저 본다. 같으면 짧은 쪽.
        //
        // 길이만으로 고르면, 동료 둘 사이의 좁은 틈으로 들어가는 쪽이 짧아서
        // 그쪽을 택한다. 그러면 둘 다에게 끼인다. 막힘 수를 먼저 보면 바깥으로 돈다.
        int _b1 = BlockCount(a, _w1, obs, count, selfR, clear)
                + BlockCount(_w1, b, obs, count, selfR, clear);
        int _b2 = BlockCount(a, _w2, obs, count, selfR, clear)
                + BlockCount(_w2, b, obs, count, selfR, clear);

        Vector3 _w;

        if (_b1 != _b2)
        {
            _w = _b1 < _b2 ? _w1 : _w2;
        }
        else
        {
            float _len1 = Flatten(_w1 - a).magnitude + Flatten(b - _w1).magnitude;
            float _len2 = Flatten(_w2 - a).magnitude + Flatten(b - _w2).magnitude;
            _w = _len1 <= _len2 ? _w1 : _w2;
        }

        written = PlanInto(a, _w, obs, count, selfR, clear, outW, written, depth - 1);
        if (written >= outW.Length) return written;

        return PlanInto(_w, b, obs, count, selfR, clear, outW, written, depth - 1);
    }

    /// <summary>
    /// a→b 선분을 막는 유닛 중 a 에 가장 가까운 것의 인덱스. 없으면 -1.
    /// </summary>
    public static int FirstBlocker(Vector3 a, Vector3 b,
                                   Obstacle[] obs, int count,
                                   float selfR, float clear)
    {
        Vector3 _ab = Flatten(b - a);
        float _abLen = _ab.magnitude;

        if (_abLen < 0.0001f) return -1;

        Vector3 _dir = _ab / _abLen;

        int _best = -1;
        float _bestAlong = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            float _need = selfR + obs[i].Radius + clear;
            Vector3 _rel = Flatten(obs[i].Position - a);

            float _along = Vector3.Dot(_rel, _dir);

            // 선분 범위로 가둔 뒤 거리를 잰다. 선분 밖의 유닛은 막는 것이 아니다
            float _clamped = Mathf.Clamp(_along, 0.0f, _abLen);
            Vector3 _closest = _dir * _clamped;

            float _dist = Flatten(_rel - _closest).magnitude;

            if (_dist >= _need - BlockEpsilon) continue;

            if (_clamped < _bestAlong)
            {
                _bestAlong = _clamped;
                _best = i;
            }
        }

        return _best;
    }

    /// <summary>a→b 선분을 막는 유닛 수.</summary>
    public static int BlockCount(Vector3 a, Vector3 b,
                                 Obstacle[] obs, int count,
                                 float selfR, float clear)
    {
        Vector3 _ab = Flatten(b - a);
        float _abLen = _ab.magnitude;

        if (_abLen < 0.0001f) return 0;

        Vector3 _dir = _ab / _abLen;
        int _n = 0;

        for (int i = 0; i < count; i++)
        {
            float _need = selfR + obs[i].Radius + clear;
            Vector3 _rel = Flatten(obs[i].Position - a);

            float _clamped = Mathf.Clamp(Vector3.Dot(_rel, _dir), 0.0f, _abLen);
            float _dist = Flatten(_rel - _dir * _clamped).magnitude;

            if (_dist < _need - BlockEpsilon) _n++;
        }

        return _n;
    }

    /// <summary>
    /// 막는 유닛을 비켜 가는 경유지.
    ///
    /// 진입 금지 원에 접하는 직선(접선)을 따라, 접점을 지나 원 반대편까지 나간 지점이다.
    ///
    /// 처음에는 진행 방향에 수직으로 "필요 거리" 만큼 떨어진 지점을 썼다.
    /// 그 지점 자체는 원 밖이지만, 출발점에서 거기까지 가는 직선이 원을 파고든다.
    /// (동료 둘이 나란히 선 경우 시뮬레이션에서 0.979m 겹침) 접선을 쓰면
    /// 출발점에서 경유지까지의 직선이 원에 닿기만 하고 들어가지 않는다.
    ///
    /// 접점보다 반지름만큼 더 나가는 이유는, 접선 위에서 원과 가장 가까운 점이 접점이고
    /// 그보다 더 가면 멀어지기 때문이다. 경유지를 접점에 두면 다음 구간이 다시
    /// 원 경계에서 시작해 판정이 애매해진다.
    ///
    /// side 가 +1 이면 진행 방향 오른쪽, -1 이면 왼쪽.
    /// </summary>
    public static Vector3 Detour(Vector3 a, Vector3 b, Obstacle o,
                                 float selfR, float clear, float side)
    {
        float _need = selfR + o.Radius + clear;

        Vector3 _rel = Flatten(o.Position - a);
        float _dist = _rel.magnitude;

        // 출발점이 이미 원 안이면 접선이 없다. 진행 방향에 수직으로 물러난다
        if (_dist <= _need + 0.0001f)
        {
            Vector3 _ab = Flatten(b - a);
            Vector3 _dir0 = _ab.sqrMagnitude > 0.0001f ? _ab.normalized : Vector3.forward;
            Vector3 _perp = Vector3.Cross(Vector3.up, _dir0);

            Vector3 _out = Flatten(o.Position) + _perp * (side * _need);
            _out.y = a.y;
            return _out;
        }

        Vector3 _toCenter = _rel / _dist;

        // 접선이 중심 방향과 이루는 각
        float _theta = Mathf.Asin(Mathf.Clamp(_need / _dist, -1.0f, 1.0f));
        Vector3 _tangentDir = RotateY(_toCenter, side * _theta);

        // 접점까지의 거리 + 원을 완전히 지나칠 여유
        float _reach = Mathf.Sqrt(Mathf.Max(0.0f, _dist * _dist - _need * _need)) + _need;

        Vector3 _p = a + _tangentDir * _reach;
        _p.y = a.y;
        return _p;
    }

    /// <summary>수평 벡터를 Y 축 기준으로 회전한다.</summary>
    public static Vector3 RotateY(Vector3 v, float radians)
    {
        float _c = Mathf.Cos(radians);
        float _s = Mathf.Sin(radians);
        return new Vector3(v.x * _c + v.z * _s, 0.0f, -v.x * _s + v.z * _c);
    }

    /// <summary>Y 를 버려 수평면에서만 계산한다.</summary>
    public static Vector3 Flatten(Vector3 v)
    {
        v.y = 0.0f;
        return v;
    }
}
