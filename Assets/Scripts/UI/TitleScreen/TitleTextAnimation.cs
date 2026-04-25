using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using EditorScript;
#endif

namespace UI.TitleScreen
{
    public class TitleTextAnimation : MonoBehaviour
    {
        public TextMeshPro text;
#if UNITY_EDITOR
        [InspectorButton]
        private void DebugPlay()
        {
            Play();
        }
#endif

        public void Play()
        {
            for (var i = 0; i < text.text.Length; i++)
                LMotion.Create(Vector3.zero, Vector3.zero, 0f).BindToTMPCharScale(text, i);

            for (var i = 0; i < text.text.Length; i++)
            {
                LMotion.Create(Vector3.zero, Vector3.one, 0.5f).WithEase(Ease.OutBounce).WithDelay(i * 0.1f)
                    .BindToTMPCharScale(text, i);
                LMotion.Create(Vector3.down, Vector3.zero, 0.5f).WithEase(Ease.OutBounce).WithDelay(i * 0.1f)
                    .BindToTMPCharPosition(text, i);
            }
        }
    }
}