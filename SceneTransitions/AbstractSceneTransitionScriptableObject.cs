using SleepHerd.Data;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SleepHerd.Core.CustomSceneManager
{
	public abstract class AbstractSceneTransitionScriptableObject : ScriptableObject
	{
        [SerializeField] private float _animationTime = 0.25f;
        [SerializeField] private TransitionId _transitionId = TransitionId.Fade;
        protected float AnimationTime => _animationTime;
		protected Image AnimatedObject;

        public TransitionId TransitionId => _transitionId;

        public abstract IEnumerator TransitionIn(Canvas parent);
		public abstract IEnumerator TransitionOut(Canvas parent);

		protected virtual Image CreateImage(Canvas parent)
		{
			var child = new GameObject("Transition Image");
			child.transform.SetParent(parent.transform, false);

			return child.AddComponent<Image>();
		}
	}
}
