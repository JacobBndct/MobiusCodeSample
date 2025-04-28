using System.Collections;
using UnityEngine;

namespace SleepHerd.Core.CustomSceneManager
{
	[CreateAssetMenu(fileName = "Fade", menuName = "Scene Transitions/Fade")]
	public class FadeTransitionScriptableObject : AbstractSceneTransitionScriptableObject
	{
		public override IEnumerator TransitionIn(Canvas parent)
		{
            if (AnimatedObject)
            {
                var time = 0f;
                var startColour = Color.black;
                var endColour = new Color(0, 0, 0, 0);

                while (time < 1f)
                {
                    AnimatedObject.color = Color.Lerp(startColour, endColour, time);
                    yield return null;

                    time += Time.deltaTime / AnimationTime;
                }
                AnimatedObject.color = endColour;

                Destroy(AnimatedObject.gameObject);
            }
		}

        public override IEnumerator TransitionOut(Canvas parent)
		{
			AnimatedObject = CreateImage(parent);
			AnimatedObject.rectTransform.anchorMin = Vector2.zero;
			AnimatedObject.rectTransform.anchorMax = Vector2.one;
			AnimatedObject.rectTransform.sizeDelta = Vector2.zero;

			var time = 0f;
			var startColour = new Color(0, 0, 0, 0);
			var endColour = Color.black;

			while (time < 1f)
			{
				AnimatedObject.color = Color.Lerp(startColour, endColour, time);
				yield return null;

				time += Time.deltaTime / AnimationTime;
			}
            AnimatedObject.color = endColour;
        }
    }
}
