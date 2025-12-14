using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExperimentHud : MonoBehaviour
{
    CanvasGroup _cg;
    Text _status;
    Text _objective;
    Text _message;
    Coroutine _messageCo;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void Build()
    {
        var canvasGo = new GameObject("Canvas_ExperimentHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 990;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _cg = canvasGo.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        _status = CreateText(canvasGo.transform, "Status", new Vector2(60, -60), 34);
        _objective = CreateText(canvasGo.transform, "Objective", new Vector2(60, -108), 28);
        _message = CreateText(canvasGo.transform, "Message", new Vector2(60, -170), 30);
        SetAlpha(_message, 0f);
    }

    public void SetVisible(bool visible)
    {
        if (_cg) _cg.alpha = visible ? 1f : 0f;
    }

    Text CreateText(Transform parent, string name, Vector2 anchoredPos, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(1600, 80);

        var txt = go.GetComponent<Text>();
        txt.text = string.Empty;
        txt.fontSize = size;
        txt.color = new Color(0.9f, 0.94f, 0.98f, 1f);
        txt.alignment = TextAnchor.UpperLeft;
        txt.raycastTarget = false;
#if UNITY_6000_0_OR_NEWER
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        return txt;
    }

    public void SetStatus(string text)
    {
        if (_status) _status.text = text;
    }

    public void SetObjective(string text)
    {
        if (_objective) _objective.text = text;
    }

    public void ShowMessage(string text, float seconds)
    {
        if (!_message) return;
        if (_messageCo != null) StopCoroutine(_messageCo);
        _messageCo = StartCoroutine(CoMessage(text, seconds));
    }

    IEnumerator CoMessage(string text, float seconds)
    {
        _message.text = text;
        SetAlpha(_message, 1f);
        yield return new WaitForSecondsRealtime(seconds);
        float dur = 0.25f;
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            SetAlpha(_message, 1f - Mathf.Clamp01(t / dur));
            yield return null;
        }
        SetAlpha(_message, 0f);
        _messageCo = null;
    }

    static void SetAlpha(Text txt, float a)
    {
        if (!txt) return;
        var c = txt.color;
        c.a = a;
        txt.color = c;
    }
}
