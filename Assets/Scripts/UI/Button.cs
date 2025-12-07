using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Button : UIScriptWithAnimation
    {
        private static readonly List<Button> Buttons = new();
        public int laneIndex;

        [SerializeField] private Image image;

        private bool _hidden = true;

        private Vector3 _hidePosition;

        private Vector3 _initialScale;
        private Vector3 _showPosition;

        private void Awake()
        {
            Buttons.Add(this);
            _initialScale = transform.localScale;

            var eulerAngles = new Vector3(0, 0, -22.5f - laneIndex * 45f);

            var normalizedVector = Quaternion.Euler(eulerAngles) * transform.up;

            _hidePosition = transform.position + 2 * normalizedVector;
            _showPosition = transform.position;

            transform.position = _hidePosition;
        }

        public static Button GetButton(int laneIndex)
        {
            return Buttons.Find(x => x.laneIndex == laneIndex);
        }

        public void Show(bool clearList = true)
        {
            if (!_hidden)
                return;

            _hidden = false;

            if (clearList)
                ClearMotion(true);
            AddMotionHandle(
                LMotion.Create(_hidePosition, _showPosition, 0.5f).WithEase(Ease.OutExpo).BindToPosition(transform)
                , false);
        }

        public static void HideAll(bool clearList = true, Action callback = null)
        {
            var i = 0;
            foreach (var button in Buttons)
            {
                if (!button._hidden)
                    i++;
                button.Hide(clearList, i == 1 ? () => { callback?.Invoke(); } : null);
            }
        }

        public void Hide(bool clearList = true, Action callback = null)
        {
            if (_hidden)
                return;

            _hidden = true;

            if (clearList)
                ClearMotion(true);

            AddMotionHandle(
                LMotion.Create(_showPosition, _hidePosition, 0.5f).WithOnComplete(() => { callback?.Invoke(); }
                ).WithEase(Ease.InExpo).BindToPosition(transform)
                , false);
        }

        public void ChangeIcon(Sprite sprite)
        {
            image.sprite = sprite;
        }

        public void Press()
        {
            var targetScale = _initialScale * 1.5f;

            ClearMotion(true);
            AddMotionHandle(LSequence.Create().Append(LMotion.Create(_initialScale, targetScale, 0.05f)
                .WithEase(Ease.OutSine)
                .BindToLocalScale(transform)).Append(LMotion.Create(targetScale, _initialScale, 0.2f)
                .WithEase(Ease.OutSine)
                .BindToLocalScale(transform)).Run());
        }

        public void ChangeInteractivity(bool toActive)
        {
        }
    }
}