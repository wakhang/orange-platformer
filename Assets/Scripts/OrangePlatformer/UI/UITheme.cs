using System;
using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    /// <summary>UI 공통: 폰트/색/절차적 스프라이트/위젯 팩토리 + 모든 UI 문자열(한 곳).</summary>
    public static class UITheme
    {
        // ---- 플레이어에게 보이는 모든 문자열 ----
        public const string ToastGoalOpen = "The mixer is open! Jump in!";
        public const string TitleIntro = "ORANGE PLATFORMER";
        public const string TitleWin = "STAGE CLEAR!";
        public const string TitleLose = "GAME OVER";
        public const string TitlePause = "PAUSED";
        public const string LabelStart = "START";
        public const string LabelResume = "RESUME";
        public const string LabelRestart = "RESTART";
        public const string ControlsText =
            "Move: WASD / Left Stick\n" +
            "Look: Mouse / Right Stick\n" +
            "Jump: Space (press again in the air to double jump)\n" +
            "Crouch: Shift / C  (Crouch + Jump = high jump)\n" +
            "Dive: E / Left Mouse (in the air)\n" +
            "Ground Pound: Crouch in the air\n" +
            "Wall Jump: jump off the teal walls\n" +
            "Pause: Esc      Confirm: Enter";
        public const string HintEnter = "Press Enter to start";

        // ---- 색 ----
        public static readonly Color Orange = new Color(1f, 0.6f, 0.1f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color ButtonNormal = new Color(0.95f, 0.55f, 0.1f, 1f);
        public static readonly Color ButtonHighlight = new Color(1f, 0.75f, 0.3f, 1f);
        public static readonly Color ButtonPressed = new Color(0.75f, 0.4f, 0.05f, 1f);
        public static readonly Color HeartColor = new Color(0.95f, 0.2f, 0.3f, 1f);

        static Font _font;
        static Sprite _circle, _heart;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    byte a = (byte)(Mathf.Clamp01(r - d) * 255f); // 1px 안티앨리어싱
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _circle;
        }

        public static Sprite Heart()
        {
            if (_heart != null) return _heart;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // 하트 음함수 (x²+y²-1)³ - x²y³ <= 0 ; 2x2 슈퍼샘플로 가장자리 완화
                    int hit = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float fx = ((x + 0.25f + sx * 0.5f) / n * 2f - 1f) * 1.25f;
                            float fy = ((y + 0.25f + sy * 0.5f) / n * 2f - 1f) * 1.25f + 0.1f;
                            float a = fx * fx + fy * fy - 1f;
                            if (a * a * a - fx * fx * fy * fy * fy <= 0f) hit++;
                        }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(hit * 255 / 4));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _heart = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _heart;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static RectTransform MakeRect(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Text MakeText(RectTransform parent, string name, int size, TextAnchor a)
        {
            var rt = MakeRect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = a;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // 비상호작용 그래픽은 레이캐스트 제외
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.6f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button MakeButton(RectTransform parent, string name, string label)
        {
            var rt = MakeRect(parent, name);
            rt.sizeDelta = new Vector2(360f, 80f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = ButtonNormal;
            cb.highlightedColor = ButtonHighlight;
            cb.selectedColor = ButtonHighlight; // 키보드/패드 포커스 표시
            cb.pressedColor = ButtonPressed;
            btn.colors = cb;
            var t = MakeText(rt, "Label", 36, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            t.text = label;
            return btn;
        }

        /// <summary>전체 화면 패널(배경 + CanvasGroup). 기본은 숨김.</summary>
        public static CanvasGroup MakePanel(RectTransform root, string name, Color bg)
        {
            var rt = MakeRect(root, name);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            SetVisible(g, false);
            return g;
        }

        public static void SetVisible(CanvasGroup g, bool visible)
        {
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
        }

        /// <summary>패널이 열릴 때 기본 버튼에 포커스(키보드/패드 내비게이션).</summary>
        public static void Focus(Selectable s)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int cs = (int)(seconds * 100f);
            return string.Format("{0:00}:{1:00}.{2:00}", cs / 6000, cs / 100 % 60, cs % 100);
        }
    }

    /// <summary>IGameSession에 (재)바인딩하는 뷰 공통 베이스. Locator의 Current 또는 Registered를 사용.</summary>
    public abstract class SessionView : MonoBehaviour
    {
        protected IGameSession Session { get; private set; }

        protected virtual void Start()
        {
            if (GameSessionLocator.Current != null) Attach(GameSessionLocator.Current);
            GameSessionLocator.Registered += Attach;
        }

        protected virtual void OnDestroy()
        {
            GameSessionLocator.Registered -= Attach;
            if (Session != null) OnDetached(Session);
            Session = null;
        }

        void Attach(IGameSession s)
        {
            if (s == null || s == Session) return;
            if (Session != null) OnDetached(Session);
            Session = s;
            OnAttached(s);
        }

        protected abstract void OnAttached(IGameSession s);
        protected abstract void OnDetached(IGameSession s);
    }
}
