using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    public class HudView : SessionView
    {
        const float ToastSeconds = 3f;
        const int HeartCount = 3;

        Text _orangeText, _timerText, _toast;
        Image[] _hearts;
        float _toastEnd;
        int _lastCs = -1;

        public static HudView Create(RectTransform root)
        {
            var rt = UITheme.MakeRect(root, "Hud");
            UITheme.Stretch(rt);
            var v = rt.gameObject.AddComponent<HudView>();

            // 좌상단: 오렌지 아이콘 + 카운트
            var icon = UITheme.MakeRect(rt, "OrangeIcon");
            UITheme.Place(icon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(56, 56));
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.sprite = UITheme.Circle();
            iconImg.color = UITheme.Orange;
            iconImg.raycastTarget = false;

            v._orangeText = UITheme.MakeText(rt, "OrangeCountText", 48, TextAnchor.MiddleLeft);
            UITheme.Place(v._orangeText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -30), new Vector2(300, 56));
            v._orangeText.text = "0 / 8";

            // 우상단: 하트
            v._hearts = new Image[HeartCount];
            for (int i = 0; i < HeartCount; i++)
            {
                var h = UITheme.MakeRect(rt, "Heart" + i);
                UITheme.Place(h, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40 - (HeartCount - 1 - i) * 72, -30), new Vector2(60, 60));
                var img = h.gameObject.AddComponent<Image>();
                img.sprite = UITheme.Heart();
                img.color = UITheme.HeartColor;
                img.raycastTarget = false;
                v._hearts[i] = img;
            }

            // 상단 중앙: 타이머
            v._timerText = UITheme.MakeText(rt, "TimerText", 48, TextAnchor.MiddleCenter);
            UITheme.Place(v._timerText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(400, 56));
            v._timerText.text = "00:00.00";

            // 토스트
            v._toast = UITheme.MakeText(rt, "GoalToastText", 56, TextAnchor.MiddleCenter);
            UITheme.Place(v._toast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1200, 80));
            v._toast.color = new Color(1f, 0.9f, 0.3f, 1f);
            v._toast.text = UITheme.ToastGoalOpen;
            v._toast.enabled = false;
            return v;
        }

        protected override void OnAttached(IGameSession s)
        {
            s.OrangesChanged += OnOranges;
            s.HeartsChanged += OnHearts;
            s.GoalOpened += OnGoalOpened;
            s.RunEnded += OnRunEnded;
            s.StateChanged += OnState;
            OnOranges(s.OrangesCollected, s.OrangesRequired);
            OnHearts(s.Hearts, s.MaxHearts);
            _lastCs = -1;
            _timerText.text = UITheme.FormatTime(s.ElapsedSeconds);
        }

        protected override void OnDetached(IGameSession s)
        {
            s.OrangesChanged -= OnOranges;
            s.HeartsChanged -= OnHearts;
            s.GoalOpened -= OnGoalOpened;
            s.RunEnded -= OnRunEnded;
            s.StateChanged -= OnState;
        }

        void OnOranges(int collected, int required) =>
            _orangeText.text = string.Format("{0} / {1}", collected, required);

        void OnHearts(int current, int max)
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                var c = _hearts[i].color;
                c.a = i < current ? 1f : 0.2f; // 잃은 하트는 알파 0.2
                _hearts[i].color = c;
            }
        }

        void OnGoalOpened()
        {
            _toast.enabled = true;
            _toastEnd = Time.unscaledTime + ToastSeconds;
        }

        void OnState(GameState st)
        {
            if (st == GameState.Intro) _toast.enabled = false;
        }

        void OnRunEnded(RunResult r) => _timerText.text = UITheme.FormatTime(r.ElapsedSeconds);

        void Update()
        {
            if (_toast.enabled && Time.unscaledTime >= _toastEnd) _toast.enabled = false;

            // 타이머는 이벤트가 없으므로 Playing 중에만, 표시값(1/100초)이 바뀔 때만 갱신
            if (Session != null && Session.State == GameState.Playing)
            {
                int cs = (int)(Session.ElapsedSeconds * 100f);
                if (cs != _lastCs)
                {
                    _lastCs = cs;
                    _timerText.text = UITheme.FormatTime(Session.ElapsedSeconds);
                }
            }
        }
    }
}
