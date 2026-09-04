# 액션 바 · 딜레이 UI 적용 안내

세 가지를 처리했습니다.

1. CSV 한글 깨짐 — 인코딩 수정
2. 타깃 지정 중 현재 캐릭터 슬롯 오른쪽에 딜레이 표시
3. 턴이 끝나도 다음 액션 바 슬롯이 붙지 않던 결함 수정

---

## 1. CSV 한글 깨짐

**원인** — CSV 5장을 **UTF-8 (BOM 없음)** 으로 썼습니다.
Windows 의 Excel 과 메모장은 BOM 이 없으면 파일을 CP949 로 오해하고,
그때 한글이 깨져 보입니다. Unity 는 BOM 없는 UTF-8 도 읽지만
편집이 안 되면 작업이 불가능하니 맞춰야 합니다.

**수정** — 5장 모두 **UTF-8 with BOM + CRLF** 로 다시 썼습니다.

| 파일 | 한글 |
|---|---:|
`SkillData.csv` | 242자 |
`SkillEffect.csv` | 64자 |
`CharacterStatus.csv` | 0자 |
`MonsterStatus.csv` | 0자 |

`CsvTable.Parse` 는 첫 글자가 BOM 이면 잘라내므로 파싱에는 영향이 없습니다.

> 앞으로 CSV 를 직접 편집할 때도 **UTF-8 with BOM** 으로 저장하세요.
> VS Code 는 우하단 인코딩 → `UTF-8 with BOM`,
> 메모장은 다른 이름으로 저장 → 인코딩 `UTF-8 (BOM)`.

---

## 2. 딜레이 표시

타깃을 지정하는 동안 현재 행동 캐릭터 슬롯 오른쪽에 이렇게 나옵니다.

```
▶ Zangbi          +40 AT
                   4번째
  Ubi
  Gwanwoo
  Bear A
  ░ Zangbi ░   ← 행동 후 들어갈 자리 (반투명)
```

- `+40 AT` = `floor(100 × BaseDelay / SPD)` = `floor(100 × 20 / 50)`
- `4번째` = 행동 후 순서에서 몇 번째가 되는지
- 반투명 슬롯 = 그 자리를 미리 보여줌 (`_showProjectedSlot` 으로 끌 수 있음)

배지는 **런타임에 생성**되므로 프리팹을 고칠 필요가 없습니다.
TMP 기본 폰트가 없으면 콘솔에 경고가 뜹니다 — 그때는
`Window > TextMeshPro > Import TMP Essential Resources` 를 실행하세요.

### 인스펙터 설정

`ActionBar` 컴포넌트

| 항목 | 기본값 | 설명 |
|---|---:|---|
`Factory` | 자동 | 비워두면 같은 오브젝트 → 씬 전체에서 찾음 |
`Visible Count` | 12 | 바에 보여줄 슬롯 수 |
`Badge Offset X` | 96 | 슬롯 오른쪽으로 띄울 거리 |
`Badge Font Size` | 20 | |
`Badge Color` | 주황 | |
`Show Projected Slot` | ✓ | 반투명 예상 위치 슬롯 |

`CreateCommandActionMemberSystem` 컴포넌트

| 항목 | 기본값 | 설명 |
|---|---:|---|
`Slot Height` | 70 | 기존 코드의 `i * -70f` 와 동일 |
`Slot Offset X` | 90 | 기존 코드의 `90f` 와 동일 |
`Current Scale` | 1.12 | 현재 차례 슬롯 확대 |

### BattleManager 연결 — 2줄

`CommandUISet()` 의 두 분기에 한 줄씩 넣습니다.

```csharp
[SerializeField] private ActionBar _actionBar;          // 필드 추가
private SkillData _pendingSkill;                        // 스킬 선택 시 담아둔다

void CommandUISet()
{
    switch (_commandState)
    {
        case CommandState.Select:
            // ... 기존 코드
            _actionBar.HideDelayPreview();               // ← 추가
            break;

        case CommandState.Targeting:
            // ... 기존 코드
            _actionBar.ShowDelayPreview(CurrentActor(), PendingBaseDelay());   // ← 추가
            break;
    }
}

int PendingBaseDelay()
{
    // 통상공격 20, 스킬은 SkillData.csv 의 baseDelay
    return _pendingSkill != null ? _pendingSkill.BaseDelay : AT.BaseAttack;
}

Unit CurrentActor()
{
    if (BattleFlow.Instance != null && BattleFlow.Instance.Current != null)
        return BattleFlow.Instance.Current.Unit;

    return GameManager.GameInstance.Units[attackIndex];   // BattleFlow 미적용 상태
}
```

`ShowDelayPreview` 는 오버로드가 두 개입니다.

- `ShowDelayPreview(BattleUnit, int)` — BattleFlow 사용 시. 버프 반영 SPD + 예상 순서까지 표시
- `ShowDelayPreview(Unit, int)` — BattleFlow 없이도 동작. `Stat.Speed` 로 계산하고 `+N AT` 만 표시

---

## 3. 다음 액션 바가 붙지 않던 결함

**원인** — 슬롯 위치를 `CreateCommandActionMember` **코루틴 안에서만** 계산했습니다.

```csharp
// 기존 : 전투 개시 때만 위치가 잡힌다
for (int i = 0; i < _units.Count; i++)
{
    CommandActionMemberAdd(_units[i], _actionMemberBarTransform);
    _actionMemberlist[i].GetComponent<RectTransform>().localPosition
        += new Vector3(90f, i * -70f, 0f);       // ← 여기서만
}
```

턴이 끝나면 `TrunMove()` → `CommandActionMemberAdd()` 로 슬롯을 추가하지만
이 경로에는 위치 계산이 없어서, 새 슬롯이 프리팹 원래 좌표에 그대로 남아
바에 나타나지 않았습니다.

**수정** — 목록이 바뀔 때마다 `Reposition()` 이 전체를 다시 배치합니다.
추가 · 제거 · 큐 재정렬 어느 경로든 위치가 맞습니다.

```csharp
public void Reposition()
{
    int row = 0;
    for (int i = 0; i < _actionMemberlist.Count; i++)
    {
        RectTransform rt = _actionMemberlist[i].GetComponent<RectTransform>();
        rt.anchoredPosition = _basePos + new Vector2(_slotOffsetX, -row * _slotHeight);
        rt.localScale = Vector3.one * (row == 0 ? _currentScale : 1f);
        rt.SetSiblingIndex(row);
        row++;
    }
}
```

기준점 `_basePos` 는 첫 슬롯을 만들 때 프리팹이 들고 있던 좌표를 그대로 캡처하므로
바 위치가 밀리지 않습니다.

### 함께 정리할 것 — BattleManager

`Reposition()` 이 위치와 스케일을 담당하므로 아래 줄들은 지웁니다.

```csharp
// Battle() 안 — ClearAll 이 파괴까지 하므로 불필요.
// 게다가 Clear() 만 하면 GameObject 가 남아 누적된다
if (_actionMemberlist.Count > 0) { _actionMemberlist.Clear(); }   // ← 삭제

// TrunMove() 안 — Reposition 이 현재 슬롯을 확대한다
_createCommandActionMemberSystem.ActionMemberlist[0]
    .GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 0.8f);   // ← 삭제
```

`CommandActionMemberAdd(Unit, Transform)` 의 시그니처는 그대로이므로
기존 호출부는 고치지 않아도 컴파일됩니다.

### BattleFlow 를 붙이면

`ActionBar` 가 `BattleFlow.OnQueueChanged` 를 구독해 **정렬된 AT 큐를 그대로**
렌더합니다. 이때부터

- 다음 12턴이 미리 보입니다 (`Peek(12)`)
- 행동 종류에 따라 순서가 실제로 바뀝니다
- 사망 유닛이 큐에서 빠지면 바에서도 사라집니다
- 슬롯을 재사용하므로 매 턴 Destroy / Instantiate 가 반복되지 않습니다

프리팹에 `AtText` 라는 이름의 `TextMeshProUGUI` 를 넣어두면 슬롯마다 AT 값도 표시됩니다.
없으면 그냥 건너뜁니다.
