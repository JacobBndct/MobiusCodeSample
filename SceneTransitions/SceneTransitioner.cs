using SleepHerd.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SleepHerd.Core.CustomSceneManager
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Canvas))]
    public class SceneTransitioner : MonoBehaviour
    {
        public event Action<string> OnTriggerNetworkedTransition;
        public event Action<int> OnSetNetworkedTransition;
        public event Action OnTransitionedOut;

        private static SceneTransitioner _instance;

        public static SceneTransitioner Instance => _instance;

        [SerializeField] private Canvas _transitionCanvas;
        [SerializeField] private List<AbstractSceneTransitionScriptableObject> _transitionList = new();
        private AbstractSceneTransitionScriptableObject _activeTransition;

        private AsyncOperation _loadLevelOperation;

        void Awake()
        {
            if (_instance != null)
            {
                Debug.LogWarning($"Duplicate SceneTransitioner instance detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _activeTransition = _transitionList[0];
            SceneManager.activeSceneChanged += HandleSceneChange;

            _transitionCanvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            SceneManager.activeSceneChanged -= HandleSceneChange;
            _loadLevelOperation = null;
        }

        #region LoadScene

        public void LoadSceneWithNetworkTransition(string sceneName)
        {
            OnTriggerNetworkedTransition?.Invoke(sceneName);
        }

        public void LoadScene(string scene)
        {
            var mode = LoadSceneMode.Single;
            _loadLevelOperation = SceneManager.LoadSceneAsync(scene, mode);
            _loadLevelOperation.allowSceneActivation = false;
            StartTransition(false);
        }

        public void LoadNextBuildIndexScene()
        {
            var mode = LoadSceneMode.Single;
            var currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            _loadLevelOperation = SceneManager.LoadSceneAsync(currentBuildIndex + 1, mode);
            _loadLevelOperation.allowSceneActivation = false;
            StartTransition(false);
        }

        public void ReloadCurrentScene()
        {
            var mode = LoadSceneMode.Single;
            var currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            _loadLevelOperation = SceneManager.LoadSceneAsync(currentBuildIndex, mode);
            _loadLevelOperation.allowSceneActivation = false;
            StartTransition(false);
        }

        #endregion

        #region Transition

        public void SetSceneTransitionLocal(AbstractSceneTransitionScriptableObject animationSO) 
        { 
            _activeTransition = animationSO; 
        }

        public void SetSceneTransitionLocal(int index)
        {
            var transition = GetTransition(index);
            SetSceneTransitionLocal(transition);
        }

        public void SetSceneTransitionNetworked(TransitionId transitionId)
        {
            var index = GetTransitionIndex(transitionId);
            OnSetNetworkedTransition?.Invoke(index);
        }

        private AbstractSceneTransitionScriptableObject GetTransition(int transitionIndex)
        {
            if (transitionIndex < _transitionList.Count && transitionIndex >= 0)
            {
                return _transitionList[transitionIndex];
            }

            return null;
        }

        private int GetTransitionIndex(TransitionId transitionId)
        {
            var transitionIndex = _transitionList.FindIndex(transition => transition.TransitionId == transitionId);
            if (transitionIndex < 0)
            {
                Debug.LogWarning("Cutscene not found. Check to make sure the index is correct and that it matches an existing cutscene in the cutscene manager.");
            }
            return transitionIndex;
        }

        public void StartTransition(bool isNetworked)
        {
            _transitionCanvas.enabled = true;
            StartCoroutine(TransitionOut(isNetworked));
        }

        public void OnServerSceneLoaded()
        {
            StartCoroutine(TransitionIn());
        }

        private IEnumerator TransitionOut(bool isNetworked)
        {
            yield return StartCoroutine(_activeTransition.TransitionOut(_transitionCanvas));

            if (!isNetworked)
            {
                _loadLevelOperation.allowSceneActivation = true;
            }
            else
            {
                OnTransitionedOut?.Invoke();
            }
        }

        private IEnumerator TransitionIn()
        {
            yield return StartCoroutine(_activeTransition.TransitionIn(_transitionCanvas));

            _loadLevelOperation = null;
            _transitionCanvas.enabled = false;
        }

        #endregion

        private void HandleSceneChange(Scene oldScene, Scene newScene)
        {
            if (_activeTransition != null && _loadLevelOperation != null)
            {
                StartCoroutine(TransitionIn());
            }
        }
    }

}
