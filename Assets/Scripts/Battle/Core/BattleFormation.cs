using UnityEngine;

/// <summary>
/// 전투 대열 배치 계산. 순수 계산만 하며 씬을 만지지 않는다.
///
/// 왜 분리했는가:
/// 예전 전투 배치는 Vector3.zero 를 기준으로 한 절대 좌표였다.
/// 전투 전용 씬의 원점에서만 성립하는 값이라, 필드에서 조우한 그 자리에서
/// 전투를 시작하려면 "조우 지점 기준 상대 좌표" 로 바뀌어야 한다.
///
/// 계산을 여기로 떼어두면 Unity 없이 값을 검증할 수 있다.
/// 각도나 간격이 어긋나는 건 눈으로 보기 전에 숫자로 잡는 편이 빠르다.
/// </summary>
public static class BattleFormation
{
    public struct Slot
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    /// <summary>기본 좌우 간격(m). 같은 편끼리 이 간격으로 늘어선다.</summary>
    public const float DefaultSpacing = 2.4f;

    /// <summary>기본 진영 간 거리(m). 아군 줄과 적군 줄 사이 간격.</summary>
    public const float DefaultGap = 7.0f;

    /// <summary>
    /// 조우 지점을 기준으로 양쪽 대열을 만든다.
    ///
    /// 정면(forward)은 플레이어 → 몬스터 방향을 XZ 평면에 눕힌 것이다.
    /// 아군은 중심에서 뒤로 gap/2, 적군은 앞으로 gap/2 만큼 떨어져 서고,
    /// 각자 상대를 바라본다.
    /// </summary>
    /// <param name="playerPos">조우 시점의 플레이어 위치</param>
    /// <param name="monsterPos">조우한 몬스터 위치</param>
    /// <param name="allyCount">아군 수 (1 이상)</param>
    /// <param name="enemyCount">적군 수 (1 이상)</param>
    /// <param name="spacing">같은 편 좌우 간격</param>
    /// <param name="gap">진영 간 거리</param>
    /// <param name="allies">결과를 담을 배열. 길이가 allyCount 이상이어야 한다</param>
    /// <param name="enemies">결과를 담을 배열. 길이가 enemyCount 이상이어야 한다</param>
    public static void Build(Vector3 playerPos, Vector3 monsterPos,
                             int allyCount, int enemyCount,
                             float spacing, float gap,
                             Slot[] allies, Slot[] enemies)
    {
        Vector3 forward = Flatten(monsterPos - playerPos);

        // 플레이어와 몬스터가 겹쳐 있으면 방향을 정할 수 없다.
        // 이 경우 월드 +Z 를 쓴다. 조우 판정이 트리거 접촉이라 실제로 일어날 수 있다.
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        else forward = forward.normalized;

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 center = Flatten((playerPos + monsterPos) * 0.5f);
        center.y = playerPos.y;

        Quaternion allyRot = Quaternion.LookRotation(forward, Vector3.up);
        Quaternion enemyRot = Quaternion.LookRotation(-forward, Vector3.up);

        Vector3 allyLine = center - forward * (gap * 0.5f);
        Vector3 enemyLine = center + forward * (gap * 0.5f);

        for (int i = 0; i < allyCount; i++)
        {
            allies[i].Position = allyLine + right * LateralOffset(i, allyCount, spacing);
            allies[i].Rotation = allyRot;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            enemies[i].Position = enemyLine + right * LateralOffset(i, enemyCount, spacing);
            enemies[i].Rotation = enemyRot;
        }
    }

    /// <summary>
    /// n 명을 중앙 정렬로 늘어세울 때 i 번째의 좌우 오프셋.
    /// n 이 홀수면 가운데 한 명이 정중앙에 오고, 짝수면 중앙을 비우고 양쪽으로 갈라선다.
    /// </summary>
    public static float LateralOffset(int i, int n, float spacing)
    {
        if (n <= 1) return 0.0f;
        return (i - (n - 1) * 0.5f) * spacing;
    }

    /// <summary>Y 를 버려 XZ 평면에 눕힌다. 경사면에서 대열이 기울지 않게 한다.</summary>
    public static Vector3 Flatten(Vector3 v)
    {
        v.y = 0.0f;
        return v;
    }
}
