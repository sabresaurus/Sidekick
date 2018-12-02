using Sabresaurus.EditorNetworking;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public enum InspectionConnection : byte { LocalEditor, RemotePlayer }

    /// <summary>
    /// Required for Sidekick to connect to a remote. This class is included in builds and auto-instantiates
    /// when the game starts.
    /// </summary>
    public class RuntimeSidekickBridge : MonoBehaviour
    {
        bool wasConnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            // Can't wrap the whole method - see https://stackoverflow.com/questions/44655667/
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            Debug.Log("Initializing Sidekick by auto-instantiating RuntimeSidekickBridge");
            GameObject newGameObject = new GameObject("RuntimeSidekickBridge", typeof(RuntimeSidekickBridge));
            DontDestroyOnLoad(newGameObject);
#endif
        }

        private void Start()
        {
            PlayerMessaging.Start();
            PlayerMessaging.RegisterForRequests(OnRequestReceived);
        }

        void Update()
        {
            PlayerMessaging.Tick();
        }

        byte[] OnRequestReceived(byte[] request)
        {
            byte[] response = SidekickRequestProcessor.Process(request);
            return response;
        }
    }
}