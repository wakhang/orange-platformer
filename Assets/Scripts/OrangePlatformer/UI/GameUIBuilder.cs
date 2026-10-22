using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OrangePlatformer.UI
{
    /// <summary>Awake에서 Canvas/EventSystem을 만들고 HUD·패널을 코드로 생성한다.</summary>
    public class GameUIBuilder : MonoBehaviour
    {
        void Awake()
        {
            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var root = (RectTransform)canvasGo.transform;
            // 생성 순서 = 그리기 순서 (HUD 아래, 패널 위)
            HudView.Create(root);
            IntroPanelView.Create(root);
            PausePanelView.Create(root);
            ResultPanelView.Create(root);
        }
    }
}
