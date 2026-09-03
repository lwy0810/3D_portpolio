using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 피격 지점 위에 잠깐 떠오르는 전투 문자. 데미지 수치, AVOID, 회복량 등에 쓴다.
///
/// 월드 공간 TextMeshPro 를 런타임에 만들므로 캔버스나 프리팹이 필요하지 않다.
/// 카메라를 향해 돌고, 위로 떠오르며, 서서히 사라진 뒤 스스로 파괴된다.
///
/// 표시 높이는 대상의 Renderer 경계 상단을 기준으로 잡는다. 캐릭터와 몬스터의
/// 덩치가 달라도 머리 위에 뜬다.
/// </summary>
public class FloatingCombatText : MonoBehaviour
{
    private TextMeshPro _text;
    private Camera _camera;

    private Vector3 _startPos;
    private float _duration = 1.0f;
    private float _rise = 1.2f;
    private float _elapsed;

    /// <summary>
    /// 대상 위에 문자를 띄운다.
    /// </summary>
    /// <param name="target">피격 대상. 이 오브젝트의 경계 위에 표시된다</param>
    /// <param name="body">표시할 문자</param>
    /// <param name="color">문자 색</param>
    /// <param name="fontSize">월드 공간 폰트 크기</param>
    /// <param name="duration">표시 시간(초)</param>
    /// <param name="rise">이 거리만큼 위로 떠오른다</param>
    public static FloatingCombatText Show(Transform target, string body, Color color,
                                          float fontSize = 5f, float duration = 1.0f,
                                          float rise = 1.2f)
    {
        if (target == null || string.IsNullOrEmpty(body)) return null;

        GameObject obj = new GameObject("FloatingCombatText");
        obj.transform.position = AnchorPosition(target);

        FloatingCombatText fct = obj.AddComponent<FloatingCombatText>();
        fct.Setup(body, color, fontSize, duration, rise);

        return fct;
    }

    /// <summary>
    /// 대상 머리 위. Renderer 가 있으면 경계 상단, 없으면 LookPos, 그것도 없으면 +2m.
    /// </summary>
    private static Vector3 AnchorPosition(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                // 타깃 표시용 스프라이트처럼 바닥에 깔린 것은 제외한다
                if (renderers[i] is SpriteRenderer) continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y + 0.4f, bounds.center.z);
        }

        Transform lookPos = target.Find("LookPos");
        if (lookPos != null) return lookPos.position + Vector3.up * 0.6f;

        return target.position + Vector3.up * 2.0f;
    }

    private void Setup(string body, Color color, float fontSize, float duration, float rise)
    {
        _duration = Mathf.Max(0.1f, duration);
        _rise = rise;
        _startPos = transform.position;
        _camera = Camera.main;

        _text = gameObject.AddComponent<TextMeshPro>();
        _text.text = body;
        _text.color = color;
        _text.fontSize = fontSize;
        _text.alignment = TextAlignmentOptions.Center;
        _text.enableWordWrapping = false;
        _text.raycastTarget = false;

        // 지오메트리보다 앞에 그려 캐릭터에 가려지지 않게 한다
        if (_text.fontSharedMaterial != null)
        {
            _text.fontMaterial.renderQueue = 4000;
        }

        if (_text.font == null)
        {
            Debug.LogWarning("[FloatingCombatText] TMP 기본 폰트가 없습니다. " +
                             "Window > TextMeshPro > Import TMP Essential Resources 를 실행하세요.");
        }

        StartCoroutine(Play());
    }

    void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        // 항상 카메라를 마주보게 (빌보드)
        transform.rotation = _camera.transform.rotation;
    }

    private IEnumerator Play()
    {
        // 처음 20% 구간에서 살짝 커졌다 돌아오는 팝 연출
        const float popPortion = 0.2f;

        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            transform.position = _startPos + Vector3.up * (_rise * t);

            float scale = t < popPortion
                          ? Mathf.Lerp(0.6f, 1.1f, t / popPortion)
                          : Mathf.Lerp(1.1f, 1.0f, (t - popPortion) / (1f - popPortion));
            transform.localScale = Vector3.one * scale;

            // 후반 40% 구간에서 사라진다
            _text.alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);

            yield return null;
        }

        Destroy(gameObject);
    }
}
