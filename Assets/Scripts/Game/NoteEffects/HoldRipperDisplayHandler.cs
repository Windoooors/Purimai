using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.NoteEffects
{
    public class HoldRipperDisplayHandler : MonoBehaviour
    {
        public float duration = 0.1f;

        private readonly List<SpriteRenderer> _spriteRenderers = new();
        private Animator _animator;

        private Coroutine _coroutine;

        public void Awake()
        {
            _animator = GetComponent<Animator>();

            _spriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
        }

        public void Show(string triggerName)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _animator.enabled = true;
            _spriteRenderers.ForEach(x => x.enabled = true);

            _animator.SetTrigger(triggerName);
        }

        public void Hide()
        {
            _animator.SetTrigger("Reset");

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(WaitAndDisable());

            return;

            IEnumerator WaitAndDisable()
            {
                yield return new WaitForSeconds(duration);

                _animator.enabled = false;
                _spriteRenderers.ForEach(x => x.enabled = false);
            }
        }
    }
}