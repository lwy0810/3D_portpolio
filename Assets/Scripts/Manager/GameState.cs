using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>게임의 진행 상태. 필드와 전투는 서로 다른 씬이 아니라 서로 다른 상태다.</summary>
public enum GameState
{
    Intro,
    Field,
    Battle,
}

/// <summary>
/// 필드 / 전투를 "씬"이 아니라 "상태"로 다루기 위한 단일 창구.
///
/// 왜 필요한가:
/// 지금까지 "지금 전투 중인가?" 를 묻는 코드가 전부
/// SceneManager.GetActiveScene().name == "CommandBattle" 로 되어 있었다.
/// 판단 근거가 코드 12곳에 흩어져 있어서, 전투를 같은 씬 안에서 시작하도록
/// 바꾸려면 12곳을 동시에 고쳐야 했다. 그 근거를 여기 한 곳으로 모은다.
///
/// 지금 단계에서의 동작:
/// SetState 가 한 번도 호출되지 않은 동안에는 활성 씬 이름에서 상태를 유도한다.
/// 즉 기존 계산식을 그대로 재현한다. 그래서 이 단계는 동작 변화가 없다.
/// 인플레이스 전환이 들어오면 SetState 가 호출되기 시작하고,
/// 그 시점부터 씬 이름은 더 이상 상태의 근거가 아니다.
/// </summary>
public static class GameFlow
{
    public const string IntroSceneName = "Intro";
    public const string FieldSceneName = "Field";
    public const string BattleSceneName = "CommandBattle";

    private static GameState _state = GameState.Intro;

    /// <summary>SetState 가 한 번이라도 호출됐는지. false 면 씬 이름에서 상태를 유도한다.</summary>
    private static bool _explicit = false;

    /// <summary>(이전 상태, 새 상태). 상태가 실제로 달라질 때만 호출된다.</summary>
    public static event System.Action<GameState, GameState> OnStateChanged;

    public static GameState State
    {
        get
        {
            if (_explicit) return _state;
            return FromSceneName(SceneManager.GetActiveScene().name);
        }
    }

    public static bool IsIntro => State == GameState.Intro;
    public static bool IsField => State == GameState.Field;
    public static bool IsBattle => State == GameState.Battle;

    public static void SetState(GameState next)
    {
        GameState prev = State;

        _state = next;
        _explicit = true;

        if (prev == next) return;

        Debug.Log($"[GameFlow] 상태 전환 {prev} → {next}");

        if (OnStateChanged != null) OnStateChanged(prev, next);
    }

    public static GameState FromSceneName(string sceneName)
    {
        if (sceneName == BattleSceneName) return GameState.Battle;
        if (sceneName == FieldSceneName) return GameState.Field;
        return GameState.Intro;
    }

    /// <summary>
    /// 씬 이름 유도 방식으로 되돌린다.
    /// 씬을 다시 불러왔을 때 이전 상태가 남아 있으면 안 되는 경우에 쓴다.
    /// 구독자는 유지된다.
    /// </summary>
    public static void ResetToSceneDerived()
    {
        _explicit = false;
        _state = GameState.Intro;
    }
}
