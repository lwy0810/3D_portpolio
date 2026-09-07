using UnityEngine;

/// <summary>
/// 붙은 RectTransform 을 기준 위치 좌우로 주기적으로 흔든다.
///
/// 조작 가능한 요소임을 알리는 연출이다. PrevSelectButton 과 NextSelectButton 은
/// 각각 바깥쪽으로, SelectMarker 는 가리키는 방향으로 향하게 두어 시선을 유도한다.
///
/// 왜 LateUpdate 인가:
/// SelectMarker 의 위치는 ItemMenuView 가 선택이 바뀔 때마다 다시 계산한다.
/// Update 에서 흔들면 같은 프레임에 위치가 덮여 흔들림이 사라지거나 떨린다.
/// 위치 계산이 모두 끝난 뒤에 오프셋만 더하도록 LateUpdate 에서 처리한다.
///
/// 왜 unscaledDeltaTime 인가:
/// 메뉴는 Time.timeScale 을 0 으로 두고 여는 경우가 있다. scaled 시간을 쓰면
/// 그때 연출이 멈춘다.
/// </summary>
public class ItemMenuNudge : MonoBehaviour
{
    private RectTransform _rect;

    private Vector2 _base;
    private Vector2 _dir = Vector2.right;

    private float _amplitude;
    private float _period;
    private float _pause;
    private float _time;

    private bool _ready;

    /// <summary>
    /// direction 은 흔들리는 축이다. amplitude 는 픽셀, period 는 1왕복 시간(초),
    /// pause 는 왕복 사이 정지 시간(초)이며 0 이면 쉬지 않고 이어진다.
    /// </summary>
    public void Bind(Vector2 direction, float amplitude, float period, float pause)
    {
        _rect = GetComponent<RectTransform>();

        if (_rect == null)
        {
            Debug.LogWarning("[ItemMenuNudge] RectTransform 이 없어 동작하지 않습니다.");
            enabled = false;
            return;
        }

        _base = _rect.anchoredPosition;

        _dir = direction.sqrMagnitude < 0.0001f ? Vector2.right : direction.normalized;

        _amplitude = amplitude;
        _period = period;
        _pause = Mathf.Max(0.0f, pause);

        // 진폭이나 주기가 0 이면 계산이 무의미하고 0 나눗셈이 된다
        if (_amplitude <= 0.0f || _period <= 0.0f)
        {
            enabled = false;
            return;
        }

        _ready = true;
    }

    /// <summary>
    /// 기준 위치를 다시 정한다. 선택 줄이 바뀌어 표식을 옮길 때처럼,
    /// 외부에서 위치를 바꿀 경우 anchoredPosition 을 직접 쓰지 말고 이 함수를 쓴다.
    /// 직접 쓰면 흔들린 오프셋이 포함된 값이 기준으로 굳어 위치가 점점 밀린다.
    /// </summary>
    public void SetBase(Vector2 basePos)
    {
        _base = basePos;

        if (_rect != null && !_ready) _rect.anchoredPosition = basePos;
    }

    public Vector2 BasePosition => _base;

    void LateUpdate()
    {
        if (!_ready) return;

        _time += Time.unscaledDeltaTime;

        float _cycle = _period + _pause;

        // 프레임이 크게 튀어 한 주기를 넘겨도 한 번에 정리되도록 나머지를 쓴다.
        // 뺄셈 1회로는 _time 이 주기보다 큰 상태로 남아 몇 프레임 정지한다
        if (_time >= _cycle) _time %= _cycle;

        _rect.anchoredPosition = _base + _dir * (_amplitude * Wave(_time));
    }

    /// <summary>
    /// 왕복 구간에서는 사인 1주기, 정지 구간에서는 0 을 돌려준다.
    /// 사인 1주기이므로 기준 위치에서 시작하여 한쪽으로 갔다가 반대쪽까지 지나
    /// 다시 기준 위치로 돌아온다. 정지 구간의 경계에서 값이 0 이라 튐이 없다.
    /// </summary>
    private float Wave(float t)
    {
        if (t >= _period) return 0.0f;

        return Mathf.Sin(Mathf.PI * 2.0f * (t / _period));
    }
}
