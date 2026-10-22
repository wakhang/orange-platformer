using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    public class ResultPanelView : SessionView
    {
        CanvasGroup _group;
        Text _title, _time, _oranges;
        Button _restartButton;

        public static ResultPanelView Create(RectTransform root)
        {
            var g = UITheme.MakePanel(root, "ResultPanel", UITheme.Dim);
            var rt = (RectTransform)g.transform;
            var v = rt.gameObject.AddComponent<ResultPanelView>();
            v._group = g;

            v._title = UITheme.MakeText(rt, "TitleText", 100, TextAnchor.MiddleCenter);
            UITheme.Place(v._title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1400, 140));
            v._title.text = UITheme.TitleWin;

            v._time = UITheme.MakeText(rt, "TimeText", 52, TextAnchor.MiddleCenter);
            UITheme.Place(v._time.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1000, 70));

            v._oranges = UITheme.MakeText(rt, "OrangesText", 52, TextAnchor.MiddleCenter);
            UITheme.Place(v._oranges.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1000, 70));

            v._restartButton = UITheme.MakeButton(rt, "RestartButton", UITheme.LabelRestart);
            UITheme.Place((RectTransform)v._restartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(360, 90));
            v._restartButton.onClick.AddListener(v.OnRestartClicked);
            return v;
        }

        void OnRestartClicked() => Session?.Restart();

        protected override void OnAttached(IGameSession s)
        {
            s.RunEnded += OnRunEnded;
            s.StateChanged += OnState;
            OnState(s.State);
        }

        protected override void OnDetached(IGameSession s)
        {
            s.RunEnded -= OnRunEnded;
            s.StateChanged -= OnState;
        }

        // RunEnded 와 StateChanged 의 발생 순서에 무관하게 동작: 내용은 RunEnded, 표시는 State
        void OnRunEnded(RunResult r)
        {
            _title.text = r.Won ? UITheme.TitleWin : UITheme.TitleLose;
            _title.color = r.Won ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.4f, 0.4f);
            _time.text = "Time  " + UITheme.FormatTime(r.ElapsedSeconds);
            _oranges.text = string.Format("Oranges  {0} / {1}", r.OrangesCollected, r.OrangesRequired);
        }

        void OnState(GameState st)
        {
            bool show = st == GameState.Won || st == GameState.GameOver;
            UITheme.SetVisible(_group, show);
            if (show) UITheme.Focus(_restartButton);
        }
    }
}
