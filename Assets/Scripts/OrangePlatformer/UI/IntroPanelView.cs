using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    public class IntroPanelView : SessionView
    {
        CanvasGroup _group;
        Button _startButton;

        public static IntroPanelView Create(RectTransform root)
        {
            var g = UITheme.MakePanel(root, "IntroPanel", UITheme.Dim);
            var rt = (RectTransform)g.transform;
            var v = rt.gameObject.AddComponent<IntroPanelView>();
            v._group = g;

            var title = UITheme.MakeText(rt, "TitleText", 90, TextAnchor.MiddleCenter);
            UITheme.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1400, 120));
            title.color = UITheme.Orange;
            title.text = UITheme.TitleIntro;

            var controls = UITheme.MakeText(rt, "ControlsText", 34, TextAnchor.UpperCenter);
            UITheme.Place(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 230), new Vector2(1400, 400));
            controls.lineSpacing = 1.2f;
            controls.text = UITheme.ControlsText;

            v._startButton = UITheme.MakeButton(rt, "StartButton", UITheme.LabelStart);
            UITheme.Place((RectTransform)v._startButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(360, 90));
            v._startButton.onClick.AddListener(v.OnStartClicked);

            var hint = UITheme.MakeText(rt, "HintText", 28, TextAnchor.MiddleCenter);
            UITheme.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(800, 40));
            hint.text = UITheme.HintEnter;

            UITheme.SetVisible(g, true); // 시작 상태 = Intro
            return v;
        }

        protected override void Start()
        {
            base.Start();
            UITheme.Focus(_startButton);
        }

        void OnStartClicked() => Session?.StartGame();

        protected override void OnAttached(IGameSession s)
        {
            s.StateChanged += OnState;
            OnState(s.State);
        }

        protected override void OnDetached(IGameSession s) => s.StateChanged -= OnState;

        void OnState(GameState st)
        {
            bool show = st == GameState.Intro;
            UITheme.SetVisible(_group, show);
            if (show) UITheme.Focus(_startButton);
        }
    }
}
