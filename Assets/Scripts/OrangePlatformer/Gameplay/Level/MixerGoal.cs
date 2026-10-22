using System.Collections;
using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    public class MixerGoal : MonoBehaviour
    {
        [SerializeField] private OrangePlatformerConfig config;
        [SerializeField] private Transform lid;
        [SerializeField] private Collider lidCollider;

        private IGameSession subscribed;
        private Vector3 lidClosedPos;
        private Quaternion lidClosedRot;
        private bool opening;
        private bool opened;

        private void Awake()
        {
            if (lid != null)
            {
                lidClosedPos = lid.localPosition;
                lidClosedRot = lid.localRotation;
            }
        }

        private void OnEnable()
        {
            GameSessionLocator.Registered += OnSessionRegistered;
            if (GameSessionLocator.Current != null) Subscribe(GameSessionLocator.Current);
        }

        private void OnDisable()
        {
            GameSessionLocator.Registered -= OnSessionRegistered;
            Unsubscribe();
        }

        private void OnSessionRegistered(IGameSession session)
        {
            Subscribe(session);
        }

        private void Subscribe(IGameSession session)
        {
            if (subscribed == session) return;
            Unsubscribe();
            subscribed = session;
            subscribed.GoalOpened += OpenLid;
            // 이미 열려 있다면 즉시 열기
            if (subscribed.GoalOpen) OpenLid();
        }

        private void Unsubscribe()
        {
            if (subscribed == null) return;
            subscribed.GoalOpened -= OpenLid;
            subscribed = null;
        }

        private void OpenLid()
        {
            if (opening || opened) return;
            opening = true;
            // 열리기 시작하는 즉시 뚜껑 콜라이더 제거
            if (lidCollider != null) lidCollider.enabled = false;
            if (lid != null && isActiveAndEnabled) StartCoroutine(AnimateLid());
            else opened = true;
        }

        private IEnumerator AnimateLid()
        {
            float duration = config != null ? Mathf.Max(0.0001f, config.lidOpenDuration) : 1f;
            float height = config != null ? config.lidOpenHeight : 0f;
            float angle = config != null ? config.lidOpenAngle : 0f;

            Vector3 endPos = lidClosedPos + Vector3.up * height;
            Quaternion endRot = lidClosedRot * Quaternion.Euler(angle, 0f, 0f); // 로컬 X축 회전

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                // smoothstep 보간
                float k = Mathf.Clamp01(t / duration);
                k = k * k * (3f - 2f * k);
                lid.localPosition = Vector3.LerpUnclamped(lidClosedPos, endPos, k);
                lid.localRotation = Quaternion.SlerpUnclamped(lidClosedRot, endRot, k);
                yield return null;
            }
            lid.localPosition = endPos;
            lid.localRotation = endRot;
            opened = true;
        }
    }
}
