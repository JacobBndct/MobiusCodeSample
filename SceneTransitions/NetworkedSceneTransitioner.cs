using System.Linq;
using SleepHerd.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SleepHerd.Core.CustomSceneManager
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedSceneTransitioner : SessionManagedNetworkBehaviour
    {
        private int _clientsTransitioned = 0;
        private string _sceneName = SceneName.Boot.ToString();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (SceneTransitioner.Instance != null)
            {
                SceneTransitioner.Instance.OnTriggerNetworkedTransition += TriggerNetworkedTransition;
                SceneTransitioner.Instance.OnSetNetworkedTransition += SetNetworkedTransition;
                SceneTransitioner.Instance.OnTransitionedOut += ClientTransitionComplete;
            }

            foreach (var state in Core.Instance.States.Cast<BaseGameState>())
            {
                state.PostLoad += HandleLoaded;
            }

            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            CleanupSubscriptions();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            CleanupSubscriptions();
        }

        private void CleanupSubscriptions()
        {
            if (SceneTransitioner.Instance != null)
            {
                SceneTransitioner.Instance.OnTriggerNetworkedTransition -= TriggerNetworkedTransition;
                SceneTransitioner.Instance.OnSetNetworkedTransition -= SetNetworkedTransition;
                SceneTransitioner.Instance.OnTransitionedOut -= ClientTransitionComplete;
            }

            foreach (var state in Core.Instance.States.Cast<BaseGameState>())
            {
                state.PostLoad -= HandleLoaded;
            }
        }

        private void SetNetworkedTransition(int index)
        {
            SetTransitionServerRpc(index);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTransitionServerRpc(int index)
        {
            SetTransitionClientRpc(index);
        }

        [ClientRpc]
        private void SetTransitionClientRpc(int index)
        {
            SceneTransitioner.Instance?.SetSceneTransitionLocal(index);
        }

        private void TriggerNetworkedTransition(string sceneName)
        {
            TriggerTransitionServerRPC(sceneName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TriggerTransitionServerRPC(string sceneName)
        {
            _sceneName = sceneName;
            _clientsTransitioned = 0;
            TriggerTransitionClientRpc();
        }

        [ClientRpc]
        private void TriggerTransitionClientRpc()
        {
            SceneTransitioner.Instance?.StartTransition(true);
        }

        private void ClientTransitionComplete()
        {
            NotifyTransitionedOutServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyTransitionedOutServerRpc()
        {
            _clientsTransitioned++;
            var totalClients = NetworkManager.Singleton.ConnectedClientsList.Count;

            if (_clientsTransitioned == totalClients)
            {
                ServerLoadScene(_sceneName);
            }
        }

        private void ServerLoadScene(string sceneName)
        {
            if (NetworkManager.Singleton?.SceneManager != null)
            {
                var sceneLoadStatus = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

                if (sceneLoadStatus != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"Failed to start loading scene {sceneName} with status: {sceneLoadStatus}");
                    return;
                }

                Debug.Log("Scene loading started on server.");
            }
        }

        private void HandleLoaded()
        {
            if (IsServer)
            {
                Debug.Log($"Scene loaded and synchronized on all clients.");
                NotifySceneLoadedServerRpc();
            }
        }

        [ServerRpc]
        private void NotifySceneLoadedServerRpc()
        {
            NotifySceneLoadedClientRpc();
        }

        [ClientRpc]
        private void NotifySceneLoadedClientRpc()
        {
            SceneTransitioner.Instance?.OnServerSceneLoaded();
        }
    }
}