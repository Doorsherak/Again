using UnityEngine;

// Unity 6 환경에서 일부 내장 UGUI 스프라이트(UI/Skin/UISprite.psd 등)가 누락되면
// Resources.GetBuiltinResource 호출이 콘솔 에러를 발생시킬 수 있어, 런타임에서 안전한 기본 스프라이트를 제공한다.
static class RuntimeUIFallback
{
    static Sprite s_whiteSprite;

    public static Sprite GetSolidSprite()
    {
        if (s_whiteSprite) return s_whiteSprite;

        var tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        s_whiteSprite.name = "RuntimeUI_WhiteSprite";
        s_whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_whiteSprite;
    }
}

