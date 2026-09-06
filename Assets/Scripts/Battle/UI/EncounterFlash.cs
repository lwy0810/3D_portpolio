using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 덮는 섬광. 필드 ↔ 전투 전환의 재배치 프레임을 가린다.
///
/// 원본(섬의궤적)의 조우 연출을 프레임 단위로 뜯어보면, 암전이 아니라
/// 흰 섬광이고 그 0.1~0.2초 사이에 캐릭터 재배치·적 배치·HUD 교체가 전부 끝난다.
/// 밝기를 재보면 전환 순간이 오히려 상승한다 — 페이드아웃이 없다는 뜻이다.
///
/// 캔버스는 코드로 만든다. 씬에 미리 두면 필드 UI 위에 항상 얹혀 있어야 해서
/// 정렬 순서를 신경 써야 하고, 실수로 켜진 채 저장되면 화면이 하얘진다.
/// </summary>
public static class EncounterFlash
{
    private const string CanvasName = "EncounterFlashCanvas";

    private static Canvas _canvas;
    private static Image _image;

    /// <summary>디버깅용. 켜두면 섬광이 걷히지 않는다.</summary>
    public static bool DebugHold = false;

    private static void Ensure()
    {
        if (_image != null && _canvas != null) return;

        GameObject _canvasObj = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler));

        _canvas = _canvasObj.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 전투 UI(기본 0)보다 확실히 위. FloatingCombatText 는 5000 을 쓰므로
        // 데미지 텍스트까지 덮도록 그보다 높게 둔다.
        _canvas.sortingOrder = 6000;

        Object.DontDestroyOnLoad(_canvasObj);

        GameObject _imageObj = new GameObject("Flash", typeof(CanvasRenderer), typeof(Image));
        _imageObj.transform.SetParent(_canvasObj.transform, false);

        _image = _imageObj.GetComponent<Image>();
        _image.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        // 입력을 먹지 않아야 한다. 투명할 때도 클릭을 가로채면 커맨드가 안 눌린다
        _image.raycastTarget = false;

        RectTransform _rt = _imageObj.GetComponent<RectTransform>();
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }

    private static void SetAlpha(Color color, float a)
    {
        Ensure();
        color.a = Mathf.Clamp01(a);
        _image.color = color;
    }

    /// <summary>투명 → 불투명. 이게 끝난 뒤에 재배치해야 전환이 보이지 않는다.</summary>
    public static IEnumerator FadeIn(Color color, float seconds)
    {
        Ensure();

        if (seconds <= 0.0f)
        {
            SetAlpha(color, 1.0f);
            yield break;
        }

        float t = 0.0f;
        while (t < seconds)
        {
            // 전투 중 Time.timeScale 을 건드릴 수 있으므로 unscaled 를 쓴다
            t += Time.unscaledDeltaTime;
            SetAlpha(color, t / seconds);
            yield return null;
        }

        SetAlpha(color, 1.0f);
    }

    /// <summary>불투명 → 투명.</summary>
    public static IEnumerator FadeOut(Color color, float seconds)
    {
        Ensure();

        if (DebugHold)
        {
            Debug.LogWarning("[EncounterFlash] DebugHold 가 켜져 있어 섬광을 유지합니다.");
            yield break;
        }

        if (seconds <= 0.0f)
        {
            SetAlpha(color, 0.0f);
            yield break;
        }

        float t = 0.0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(color, 1.0f - t / seconds);
            yield return null;
        }

        SetAlpha(color, 0.0f);
    }

    /// <summary>즉시 투명. 전환이 실패했을 때 화면이 하얀 채로 남지 않게 한다.</summary>
    public static void Clear()
    {
        if (_image == null) return;
        Color c = _image.color;
        c.a = 0.0f;
        _image.color = c;
    }
}
