using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.NoteEffects
{
    public class TapJudgeDisplayHandler : MonoBehaviour
    {
        private Animator _animator;
        public float duration = 0.5f;
        
        private readonly List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
        
        public void Awake()
        {
            _animator = GetComponent<Animator>();

            _spriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
        }
        
        private Coroutine _coroutine;

        public void Stop()
        {
            _animator.SetTrigger("Reset");
            
            if (_coroutine != null)
                StopCoroutine(_coroutine);
            
            _animator.enabled = false;
            _spriteRenderers.ForEach(x => x.enabled = false);
        }

        public void Show(string triggerName)
        {
            _animator.enabled = true;
            _spriteRenderers.ForEach(x => x.enabled = true);
            
            _animator.SetTrigger(triggerName);
            
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