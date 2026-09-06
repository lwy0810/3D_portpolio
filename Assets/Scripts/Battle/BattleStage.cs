using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조우한 그 자리에 전투 대열을 세우고, 전투가 끝나면 필드 상태로 되돌린다.
///
/// 예전에는 이 일을 씬 전환이 대신했다. CommandBattle 씬이 로드되면
/// UnitCreateSystem 이 Vector3.zero 기준 절대 좌표에 유닛을 새로 만들었다.
/// 씬을 갈지 않으므로, 이제 "필드에 이미 있는 것을 옮기고 되돌리는" 일이 필요하다.
///
/// 되돌리기 위해 필드 상태를 기억해 둔다. 리더 위치/회전, 어떤 동료가 꺼져 있었는지,
/// 조우한 필드 몬스터가 무엇인지.
/// </summary>
public class BattleStage : MonoBehaviour
{
    [Header("대열")]
    [Tooltip("같은 편끼리의 좌우 간격(m)")]
    [SerializeField] private float _spacing = BattleFormation.DefaultSpacing;
    [Tooltip("아군 줄과 적군 줄 사이 거리(m)")]
    [SerializeField] private float _gap = BattleFormation.DefaultGap;
    [Tooltip("전투에 등장할 몬스터 수")]
    [SerializeField] private int _battleMonsterCount = 3;

    [Header("지면 맞춤")]
    [Tooltip("대열 위치를 지면에 붙인다. 끄면 조우 시점의 플레이어 높이를 그대로 쓴다")]
    [SerializeField] private bool _snapToGround = true;
    [Tooltip("지면을 찾을 때 위로 올라가 쏘기 시작하는 높이(m)")]
    [SerializeField] private float _probeUp = 3.0f;
    [Tooltip("지면을 찾을 때 아래로 훑는 거리(m)")]
    [SerializeField] private float _probeDown = 12.0f;

    [Header("전투 후")]
    [Tooltip("후퇴·패배로 돌아왔을 때 몬스터에게서 밀어내는 거리(m). 0 이면 즉시 재조우한다")]
    [SerializeField] private float _retreatPushBack = 4.0f;
    [Tooltip("승리로 사라진 필드 몬스터를 다시 만들기까지의 시간(초). 0 이면 재생성하지 않는다")]
    [SerializeField] private float _respawnSeconds = 8.0f;

    // ── 되돌리기용 필드 상태 ────────────────────────────────
    private struct FieldSnapshot
    {
        public GameObject Leader;         // 조우 시점의 선두 캐릭터. 복귀 시 이 오브젝트를 되돌린다
        public Vector3 LeaderPos;
        public Quaternion LeaderRot;
        public bool[] WasActive;          // 조우 전 각 캐릭터의 활성 상태
        public GameObject FieldMonster;   // 조우한 필드 몬스터 (전투 중에는 숨긴다)
        public Vector3 EncounterForward;  // 조우 시 바라본 방향. 후퇴 시 밀어내는 데 쓴다
        public bool Valid;
    }

    private FieldSnapshot _snapshot;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    private BattleFormation.Slot[] _allySlots = new BattleFormation.Slot[8];
    private BattleFormation.Slot[] _enemySlots = new BattleFormation.Slot[8];

    public int BattleMonsterCount => Mathf.Max(1, _battleMonsterCount);

    /// <summary>
    /// 조우 지점에 전투 대열을 세운다.
    /// </summary>
    /// <param name="fieldMonster">접촉한 필드 몬스터. 전투 중에는 숨긴다</param>
    /// <returns>성공 여부. 실패하면 전투를 시작해서는 안 된다</returns>
    public bool BuildBattle(GameObject fieldMonster)
    {
        GameManager _gm = GameManager.GameInstance;
        if (_gm == null)
        {
            Debug.LogError("[BattleStage] GameManager 가 없습니다.");
            return false;
        }

        List<GameObject> _chars = _gm.Characters;
        if (_chars == null || _chars.Count == 0)
        {
            Debug.LogError("[BattleStage] 캐릭터가 없어 전투 대열을 세울 수 없습니다.");
            return false;
        }

        GameObject _leader = FirstAlive(_chars);
        if (_leader == null)
        {
            Debug.LogError("[BattleStage] 살아 있는 캐릭터가 없습니다.");
            return false;
        }

        // ① 필드 상태를 기억한다. 이걸 놓치면 전투 후 필드로 못 돌아온다
        _snapshot = new FieldSnapshot
        {
            Leader = _leader,
            LeaderPos = _leader.transform.position,
            LeaderRot = _leader.transform.rotation,
            WasActive = new bool[_chars.Count],
            FieldMonster = fieldMonster,
            Valid = true
        };
        for (int i = 0; i < _chars.Count; i++)
        {
            _snapshot.WasActive[i] = _chars[i] != null && _chars[i].activeSelf;
        }

        Vector3 _monsterPos = fieldMonster != null
            ? fieldMonster.transform.position
            : _leader.transform.position + _leader.transform.forward * 5.0f;

        _snapshot.EncounterForward =
            BattleFormation.Flatten(_monsterPos - _snapshot.LeaderPos).normalized;

        // ② 대열 계산
        int _allyCount = CountAlive(_chars);
        int _enemyCount = BattleMonsterCount;

        EnsureCapacity(ref _allySlots, _allyCount);
        EnsureCapacity(ref _enemySlots, _enemyCount);

        BattleFormation.Build(_snapshot.LeaderPos, _monsterPos,
                              _allyCount, _enemyCount, _spacing, _gap,
                              _allySlots, _enemySlots);

        SnapSlots(_allySlots, _allyCount, _snapshot.LeaderPos.y);
        SnapSlots(_enemySlots, _enemyCount, _snapshot.LeaderPos.y);

        // ③ 조우한 필드 몬스터는 숨긴다. 전투용 몬스터가 그 자리를 대신한다
        if (fieldMonster != null) fieldMonster.SetActive(false);

        // ④ 캐릭터 배치. 필드에서 꺼져 있던 동료를 켜서 대열에 세운다
        //
        // 목록을 비우지 않으면 두 번째 전투부터 파괴된 이전 전투의 항목이 남는다.
        _gm.Units.Clear();
        _gm.CharacterComponents.Clear();
        _gm.MonsterComponents.Clear();

        int _slot = 0;
        for (int i = 0; i < _chars.Count; i++)
        {
            GameObject _c = _chars[i];
            if (_c == null) continue;

            Character _comp = _c.GetComponent<Character>();
            if (_comp == null || _comp.Stat == null || _comp.Stat.Hp <= 0) continue;

            _c.SetActive(true);
            Teleport(_c, _allySlots[_slot].Position, _allySlots[_slot].Rotation);
            _slot++;

            _gm.CharacterComponents.Add(_comp);
            _gm.Units.Add(_comp);
        }

        // ⑤ 전투용 몬스터 생성
        _spawned.Clear();
        _gm.Monsters.Clear();

        UnitCreateSystem _creator = FindFirstObjectByType<UnitCreateSystem>(FindObjectsInactive.Include);
        if (_creator == null)
        {
            Debug.LogError("[BattleStage] UnitCreateSystem 을 찾지 못했습니다.");
            RestoreField(false);
            return false;
        }

        List<GameObject> _made = _creator.CreateBattleMonstersAt(_enemySlots, _enemyCount);
        if (_made == null || _made.Count == 0)
        {
            Debug.LogError("[BattleStage] 전투용 몬스터를 만들지 못했습니다.");
            RestoreField(false);
            return false;
        }

        _spawned.AddRange(_made);

        Debug.Log($"[BattleStage] 전투 대열 완성 — 아군 {_slot}명 / 적군 {_made.Count}마리, " +
                  $"조우 지점 {_snapshot.LeaderPos}");
        return true;
    }

    /// <summary>
    /// 전투가 끝난 뒤 필드 상태로 되돌린다.
    /// </summary>
    /// <param name="victory">승리면 조우한 필드 몬스터를 없애고, 아니면 되살린다</param>
    public void RestoreField(bool victory)
    {
        GameManager _gm = GameManager.GameInstance;

        // ① 전투용 몬스터 정리
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();

        if (_gm != null)
        {
            _gm.Monsters.Clear();
            _gm.MonsterComponents.Clear();
            _gm.Units.Clear();
        }

        if (!_snapshot.Valid)
        {
            Debug.LogWarning("[BattleStage] 필드 상태 기록이 없어 복구를 건너뜁니다.");
            return;
        }

        // ② 캐릭터 복구. 리더만 필드에 남고 동료는 다시 꺼진다
        if (_gm != null && _gm.Characters != null)
        {
            List<GameObject> _chars = _gm.Characters;

            // 조우 시점의 리더를 그대로 되돌린다.
            // 여기서 다시 FirstAlive 를 부르면, 리더가 전투에서 쓰러진 경우
            // 다른 캐릭터가 필드 위치를 차지하고 원래 리더는 전투 대열에 남는다.
            GameObject _leader = _snapshot.Leader != null ? _snapshot.Leader : FirstAlive(_chars);

            Vector3 _returnPos = _snapshot.LeaderPos;

            // 승리가 아니면 조우 지점에서 뒤로 물러선다.
            // 같은 자리에 두면 되살아난 몬스터의 트리거에 즉시 다시 닿는다
            if (!victory && _retreatPushBack > 0.0f)
            {
                _returnPos -= _snapshot.EncounterForward * _retreatPushBack;
                _returnPos.y = GroundY(_returnPos, _snapshot.LeaderPos.y);
            }

            for (int i = 0; i < _chars.Count; i++)
            {
                GameObject _c = _chars[i];
                if (_c == null) continue;

                if (_c == _leader)
                {
                    Teleport(_c, _returnPos, _snapshot.LeaderRot);
                    _c.SetActive(true);
                    continue;
                }

                // 필드에서 꺼져 있던 동료는 다시 끈다
                bool _wasActive = i < _snapshot.WasActive.Length && _snapshot.WasActive[i];
                _c.SetActive(_wasActive);
            }
        }

        // ③ 조우한 필드 몬스터
        if (_snapshot.FieldMonster != null)
        {
            if (victory)
            {
                // 쓰러뜨린 개체는 없앤다. 예전에는 씬 리로드가 새 몬스터를 만들어 줬으므로,
                // 재생성이 없으면 승리 한 번에 필드에 몬스터가 사라져 다시 시험해 볼 수 없다.
                Vector3 _pos = _snapshot.FieldMonster.transform.position;
                Quaternion _rot = _snapshot.FieldMonster.transform.rotation;

                Destroy(_snapshot.FieldMonster);
                if (_gm != null) _gm.Monster = null;

                if (_respawnSeconds > 0.0f)
                {
                    StartCoroutine(RespawnFieldMonster(_pos, _rot, _respawnSeconds));
                }
            }
            else
            {
                _snapshot.FieldMonster.SetActive(true);
            }
        }

        _snapshot.Valid = false;
        Debug.Log($"[BattleStage] 필드 복구 완료 (승리 {victory})");
    }

    private IEnumerator RespawnFieldMonster(Vector3 pos, Quaternion rot, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 그 사이에 새 전투가 시작됐으면 끼어들지 않는다
        if (!GameFlow.IsField) yield break;

        UnitCreateSystem _creator = FindFirstObjectByType<UnitCreateSystem>(FindObjectsInactive.Include);
        if (_creator == null) yield break;

        GameObject _obj = _creator.CreateFieldMonsterAt(pos, rot);

        if (_obj != null) Debug.Log($"[BattleStage] 필드 몬스터를 {pos} 에 다시 만들었습니다.");
    }

    /// <summary>전투 대열의 중심. 카메라가 바라볼 지점.</summary>
    public Vector3 BattleCenter(int allyCount, int enemyCount)
    {
        Vector3 sum = Vector3.zero;
        int n = 0;
        for (int i = 0; i < allyCount && i < _allySlots.Length; i++) { sum += _allySlots[i].Position; n++; }
        for (int i = 0; i < enemyCount && i < _enemySlots.Length; i++) { sum += _enemySlots[i].Position; n++; }
        return n > 0 ? sum / n : _snapshot.LeaderPos;
    }

    /// <summary>조우 시 플레이어가 바라본 방향. 전투 카메라를 뒤에 두는 데 쓴다.</summary>
    public Vector3 EncounterForward =>
        _snapshot.Valid ? _snapshot.EncounterForward : Vector3.forward;

    // ── 도우미 ──────────────────────────────────────────────

    private void SnapSlots(BattleFormation.Slot[] slots, int count, float fallbackY)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 p = slots[i].Position;
            p.y = GroundY(p, fallbackY);
            slots[i].Position = p;
        }
    }

    /// <summary>
    /// 지면 높이. 유닛의 콜라이더는 지면이 아니므로 걸러낸다.
    /// 예전에 skinWidth 를 더해 캐릭터가 떠 있었던 적이 있어, 여기서는 hit.point 를 그대로 쓴다.
    /// </summary>
    private float GroundY(Vector3 pos, float fallbackY)
    {
        if (!_snapToGround) return fallbackY;

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

            // 사람·몬스터 위에 세우면 안 된다
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

    /// <summary>
    /// CharacterController 는 내부에 위치를 캐시한다. 켜진 상태로 transform 을 옮기면
    /// 다음 Move 에서 원래 자리로 되돌아가거나 벽을 뚫는다. 끄고 옮긴 뒤 다시 켠다.
    /// </summary>
    private void Teleport(GameObject obj, Vector3 pos, Quaternion rot)
    {
        CharacterController _cc = obj.GetComponent<CharacterController>();

        if (_cc != null) _cc.enabled = false;

        obj.transform.position = pos;
        obj.transform.rotation = rot;

        if (_cc != null) _cc.enabled = true;
    }

    private static GameObject FirstAlive(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            Unit _u = list[i].GetComponent<Unit>();
            if (_u != null && _u.Stat != null && _u.Stat.Hp <= 0) continue;
            return list[i];
        }
        return null;
    }

    private static int CountAlive(List<GameObject> list)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            Unit _u = list[i].GetComponent<Unit>();
            if (_u == null || _u.Stat == null) continue;
            if (_u.Stat.Hp <= 0) continue;
            n++;
        }
        return n > 0 ? n : 1;
    }

    private static void EnsureCapacity(ref BattleFormation.Slot[] arr, int need)
    {
        if (arr == null || arr.Length < need) arr = new BattleFormation.Slot[Mathf.Max(need, 8)];
    }
}
