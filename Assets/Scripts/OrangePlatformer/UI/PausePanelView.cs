using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    public class PausePanelView : SessionView
    {
        CanvasGroup _group;
        Button _resumeButton, _restartButton;

        public static PausePanelView Create(RectTransform root)
        {
            var g = UITheme.MakePanel(root, "PausePanel", UITheme.Dim);
            var rt = (RectTransform)g.transform;
            var v = rt.gameObject.AddComponent<PausePanelView>();
            v._group = g;

            var title = UITheme.MakeText(rt, "TitleText", 90, TextAnchor.MiddleCenter);
            UITheme.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1000, 120));
            title.text = UITheme.TitlePause;

            v._resumeButton = UITheme.MakeButton(rt, "ResumeButton", UITheme.LabelResume);
            UITheme.Place((RectTransform)v._resumeButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(360, 90));
            v._resumeButton.onClick.AddListener(v.OnResumeClicked);

            v._restartButton = UITheme.MakeButton(rt, "RestartButton", UITheme.LabelRestart);
            UITheme.Place((RectTransform)v._restartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(360, 90));
            v._restartButton.onClick.AddListener(v.OnRestartClicked);
            return v;
        }

        void OnResumeClicked() => Session?.Resume();
        void OnRestartClicked() => Session?.Restart();

        protected override void OnAttached(IGameSession s)
        {
            s.StateChanged += OnState;
            OnState(s.State);
        }

        protected override void OnDetached(IGameSession s) => s.StateChanged -= OnState;

        void OnState(GameState st)
        {
            bool show = st == GameState.Paused;
            UITheme.SetVisible(_group, show);
            if (show) UITheme.Focus(_resumeButton);
        }
    }
}
