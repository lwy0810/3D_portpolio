using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 피격 지점 위에 잠깐 떠오르는 전투 문자. 데미지 수치, AVOID, 회복량 등에 쓴다.
///
/// 화면 겹치기(Screen Space Overlay) 캔버스에 UI 문자를 만들고, 대상의 월드 좌표를
/// 매 프레임 화면 좌표로 변환해서 따라붙는다. 월드 공간 문자와 달리
///   - 캐릭터나 지형에 절대 가려지지 않고
///   - 카메라 각도와 무관하게 항상 정면으로 보이고
///   - 폰트 크기가 픽셀 단위라 화면에서 보이는 크기가 예측 가능하다.
///
/// 캔버스는 처음 호출될 때 자동으로 만들어지므로 씬 수정이나 프리팹이 필요 없다.
/// </summary>
public class FloatingCombatText : MonoBehaviour
{
    // 다른 UI 위에 그리기 위한 정렬 순서. 커맨드 UI 보다 항상 위에 온다
    private const int CanvasSortingOrder = 5000;

    private const string CanvasName = "FloatingCombatTextCanvas";

    // 화면 밖으로 나갔을 때 억지로 끌어들이는 여유 픽셀
    private const float ScreenMargin = 60f;

    private static Canvas _canvas;
    private static RectTransform _canvasRect;

    /// <summary>
    /// true 면 문자가 사라지지 않고 화면에 남는다. Hierarchy 에서 눈으로 확인할 때만 쓴다.
    /// </summary>
    public static bool DebugHold = false;

    /// <summary>
    /// true 면 생성될 때마다 좌표와 폰트 상태를 Console 에 찍는다.
    /// </summary>
    public static bool DebugLog = true;

    /// <summary>
    /// true 면 문자 뒤에 반투명 빨간 상자를 깔아 캔버스가 그려지는지 확인한다.
    /// </summary>
    public static bool DebugBackground = false;

    // 대상 하나당 문자 하나. 이미 떠 있으면 새로 만들지 않고 그 문자의 값을 바꾼다
    private static readonly Dictionary<Transform, FloatingCombatText> _active =
        new Dictionary<Transform, FloatingCombatText>();

    private RectTransform _rect;
    private TextMeshProUGUI _text;
    private Camera _camera;

    // 대상이 죽어서 사라져도 문자가 남도록 좌표를 받아둔다
    private Vector3 _worldAnchor;

    // 재사용 판단과 좌표 갱신에 쓰는 대상
    private Transform _target;

    private float _duration = 1.0f;
    private float _risePixels = 60f;
    private float _elapsed;

    /// <summary>
    /// 대상 위에 문자를 띄운다.
    /// </summary>
    /// <param name="target">피격 대상. 이 오브젝트의 경계 위에 표시된다</param>
    /// <param name="body">표시할 문자</param>
    /// <param name="color">문자 색</param>
    /// <param name="fontSizePx">폰트 크기(픽셀)</param>
    /// <param name="duration">표시 시간(초)</param>
    /// <param name="risePixels">표시되는 동안 위로 떠오르는 거리(픽셀)</param>
    public static FloatingCombatText Show(Transform target, string body, Color color,
                                          float fontSizePx = 40f, float duration = 1.0f,
                                          float risePixels = 60f)
    {
        if (target == null || string.IsNullOrEmpty(body))
        {
            Debug.LogWarning("[FloatingCombatText] 대상이나 문자가 비어 있어 표시하지 않는다");
            return null;
        }

        Camera cam = FindCamera();
        if (cam == null)
        {
            Debug.LogWarning("[FloatingCombatText] 활성 카메라를 찾을 수 없어 표시하지 못했다");
            return null;
        }

        Canvas canvas = EnsureCanvas();

        // 이미 이 대상 위에 문자가 떠 있으면 새로 만들지 않고 값만 갈아끼운다.
        // 회피 → 데미지, 데미지 수치 변화, 데미지 → 회피 모두 이 경로로 바뀐다
        FloatingCombatText live;
        if (_active.TryGetValue(target, out live) && live != null)
        {
            live.Refresh(cam, target, body, color, fontSizePx, duration, risePixels);
            return live;
        }

        // RectTransform, CanvasRenderer, 문자 컴포넌트를 한 번에 붙여서 만든다.
        // 이 순서가 UI 문자를 런타임에 만드는 정석이다
        GameObject obj = new GameObject("FloatingCombatText",
                                        typeof(RectTransform),
                                        typeof(CanvasRenderer),
                                        typeof(TextMeshProUGUI));

        obj.transform.SetParent(canvas.transform, false);
        obj.layer = canvas.gameObject.layer;

        FloatingCombatText fct = obj.AddComponent<FloatingCombatText>();
        fct.Setup(cam, target, body, color, fontSizePx, duration, risePixels);

        _active[target] = fct;

        return fct;
    }

    /// <summary>
    /// 문자를 담을 캔버스. 없으면 만든다. 씬이 바뀌면 다시 만든다.
    /// </summary>
    private static Canvas EnsureCanvas()
    {
        if (_canvas != null) return _canvas;

        GameObject found = GameObject.Find(CanvasName);
        if (found != null)
        {
            _canvas = found.GetComponent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.GetComponent<RectTransform>();
                ApplyTmpShaderChannels(_canvas);
                return _canvas;
            }
        }

        // 씬이 바뀌어 캔버스를 새로 만드는 상황이면 이전 씬의 문자 기록은 버린다
        _active.Clear();

        // CanvasScaler 는 붙이지 않는다. 기본 캔버스가 이미 화면 픽셀 1:1 이다
        GameObject obj = new GameObject(CanvasName, typeof(Canvas));

        _canvas = obj.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CanvasSortingOrder;
        _canvas.pixelPerfect = false;

        ApplyTmpShaderChannels(_canvas);

        _canvasRect = obj.GetComponent<RectTransform>();

        return _canvas;
    }

    /// <summary>
    /// TMP 문자는 정점에 추가 데이터(TexCoord1 등)를 실어 보내는데, 코드로 만든 캔버스는
    /// 이 통로가 닫혀 있어 문자가 아예 그려지지 않는다. 열어준다.
    /// 에디터에서 만든 캔버스는 TMP 가 알아서 열어주기 때문에 이 문제가 드러나지 않는다.
    /// </summary>
    private static void ApplyTmpShaderChannels(Canvas canvas)
    {
        if (canvas == null) return;

        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
    }

    /// <summary>
    /// MainCamera 태그가 없는 씬에서도 동작하도록 카메라를 넓게 찾는다.
    /// </summary>
    private static Camera FindCamera()
    {
        if (Camera.main != null) return Camera.main;

        Camera[] all = Camera.allCameras;
        if (all != null && all.Length > 0) return all[0];

        return Object.FindAnyObjectByType<Camera>();
    }

    /// <summary>
    /// 대상 머리 위. Renderer 가 있으면 경계 상단, 없으면 LookPos, 그것도 없으면 +2m.
    /// </summary>
    private static Vector3 AnchorPosition(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        Bounds bounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            // 타깃 표시용 스프라이트처럼 바닥에 깔린 것은 제외한다
            if (renderers[i] is SpriteRenderer) continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (hasBounds)
        {
            return new Vector3(bounds.center.x, bounds.max.y + 0.4f, bounds.center.z);
        }

        Transform lookPos = target.Find("LookPos");
        if (lookPos != null) return lookPos.position + Vector3.up * 0.6f;

        return target.position + Vector3.up * 2.0f;
    }

    private void Setup(Camera cam, Transform target, string body, Color color,
                       float fontSizePx, float duration, float risePixels)
    {
        _camera = cam;
        _target = target;
        _worldAnchor = AnchorPosition(target);
        _duration = Mathf.Max(0.1f, duration);
        _risePixels = risePixels;

        _rect = GetComponent<RectTransform>();

        // 앵커를 좌하단에 두면 anchoredPosition 이 화면 픽셀 좌표와 같아진다
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(0f, 0f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = new Vector2(400f, 120f);
        _rect.localScale = Vector3.one;

        _text = GetComponent<TextMeshProUGUI>();
        _text.text = body;
        _text.color = color;
        _text.fontSize = fontSizePx;
        _text.enableAutoSizing = false;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.raycastTarget = false;

        // 기본 폰트가 비어 있으면 TMP 패키지 기본 폰트를 직접 찾아 넣는다
        if (_text.font == null)
        {
            _text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (_text.font == null)
            {
                Debug.LogWarning("[FloatingCombatText] TMP 폰트를 찾지 못했다. " +
                                 "Window > TextMeshPro > Import TMP Essential Resources 를 실행하라");
            }
        }

        // 폰트를 코드로 넣은 경우 재질이 비어 있을 수 있다
        if (_text.fontSharedMaterial == null && _text.font != null)
        {
            _text.fontSharedMaterial = _text.font.material;
        }

        _text.ForceMeshUpdate();

        UpdateScreenPosition(0f);

        if (DebugBackground) AddDebugBackground();

        if (DebugLog)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_worldAnchor);

            Debug.Log($"[FloatingCombatText] \"{body}\" 생성 / " +
                      $"카메라 {_camera.name} / 월드 {_worldAnchor} / 화면 {screen} / " +
                      $"화면크기 {Screen.width}x{Screen.height} / " +
                      $"캔버스 {(_canvasRect != null ? _canvasRect.rect.size.ToString() : "없음")} / " +
                      $"위치 {_rect.anchoredPosition} / 폰트 {(_text.font != null ? _text.font.name : "없음")} / " +
                      $"크기 {_text.fontSize} / 색 {_text.color}");
        }

        StartCoroutine(Play());
    }

    /// <summary>
    /// 이미 떠 있는 문자의 값을 바꾸고 표시 시간을 처음부터 다시 센다.
    /// 문자를 지우고 새로 만들지 않으므로 대상 위에 항상 하나만 남는다.
    /// </summary>
    private void Refresh(Camera cam, Transform target, string body, Color color,
                         float fontSizePx, float duration, float risePixels)
    {
        _camera = cam;
        _target = target;
        _duration = Mathf.Max(0.1f, duration);
        _risePixels = risePixels;

        // 대상이 움직였을 수 있으므로 좌표를 다시 잡는다
        if (target != null) _worldAnchor = AnchorPosition(target);

        if (_text != null)
        {
            _text.text = body;
            _text.color = color;
            _text.fontSize = fontSizePx;
            _text.alpha = 1f;
            _text.ForceMeshUpdate();
        }

        // 처음부터 다시 세면 팝 연출과 사라짐이 함께 되돌아간다
        _elapsed = 0f;

        UpdateScreenPosition(0f);

        if (DebugLog)
        {
            Debug.Log($"[FloatingCombatText] \"{body}\" 로 갱신 " +
                      $"(대상 {(target != null ? target.name : "없음")})");
        }
    }

    void OnDestroy()
    {
        // 대상이 이미 파괴되어 키로 찾을 수 없는 경우까지 확실히 지운다
        Transform key = null;
        bool found = false;

        foreach (KeyValuePair<Transform, FloatingCombatText> pair in _active)
        {
            if (pair.Value == this)
            {
                key = pair.Key;
                found = true;
                break;
            }
        }

        // 파괴된 대상은 null 비교가 참이 되므로 찾았는지 여부로 판단한다
        if (found) _active.Remove(key);
    }

    /// <summary>
    /// 확인용 배경 상자. 상자는 보이는데 문자가 안 보이면 폰트/셰이더 문제,
    /// 상자도 안 보이면 캔버스 문제로 원인이 갈린다.
    /// </summary>
    private void AddDebugBackground()
    {
        GameObject box = new GameObject("DebugBackground",
                                        typeof(RectTransform),
                                        typeof(CanvasRenderer),
                                        typeof(Image));

        box.transform.SetParent(transform, false);
        box.transform.SetAsFirstSibling();

        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0f);
        boxRect.anchorMax = new Vector2(1f, 1f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        Image img = box.GetComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.5f);
        img.raycastTarget = false;
    }

    /// <summary>
    /// 월드 좌표를 화면 좌표로 바꿔 위치를 잡는다. 카메라가 움직여도 대상 위에 붙어 있다.
    /// 화면 밖으로 나가면 가장자리 안쪽으로 끌어들여 최소한 보이게 한다.
    /// </summary>
    private void UpdateScreenPosition(float rise)
    {
        if (_camera == null) _camera = FindCamera();
        if (_camera == null || _rect == null) return;

        // 대상이 살아 있으면 좌표를 계속 따라간다. 죽어 사라졌으면 마지막 좌표에 남는다
        if (_target != null) _worldAnchor = AnchorPosition(_target);

        Vector3 screen = _camera.WorldToScreenPoint(_worldAnchor);

        float x = screen.x;
        float y = screen.y + rise;

        // 카메라 뒤쪽이면 화면 가운데에 띄운다
        if (screen.z <= 0f)
        {
            x = Screen.width * 0.5f;
            y = Screen.height * 0.5f;
        }

        x = Mathf.Clamp(x, ScreenMargin, Mathf.Max(ScreenMargin, Screen.width - ScreenMargin));
        y = Mathf.Clamp(y, ScreenMargin, Mathf.Max(ScreenMargin, Screen.height - ScreenMargin));

        _rect.anchoredPosition = new Vector2(x, y);
    }

    private IEnumerator Play()
    {
        // 처음 20% 구간에서 살짝 커졌다 돌아오는 팝 연출
        const float popPortion = 0.2f;

        // 확인용 유지 모드에서는 사라지지 않는다
        if (DebugHold)
        {
            while (true)
            {
                UpdateScreenPosition(_risePixels);
                yield return null;
            }
        }

        while (_elapsed < _duration)
        {
            // 시간 정지 중에도 흐르도록 unscaled 시간을 쓴다
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            UpdateScreenPosition(_risePixels * t);

            float scale = t < popPortion
                          ? Mathf.Lerp(0.6f, 1.15f, t / popPortion)
                          : Mathf.Lerp(1.15f, 1.0f, (t - popPortion) / (1f - popPortion));
            if (_rect != null) _rect.localScale = Vector3.one * scale;

            // 후반 40% 구간에서 사라진다
            if (_text != null)
            {
                _text.alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
