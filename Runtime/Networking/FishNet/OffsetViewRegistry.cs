using System;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    /// <summary>
    /// Maintains a registry of OffsetViews on the server and on clients.
    /// </summary>
    public class OffsetViewRegistry
    {
        private bool logging;
        private LocalOffsetState localOffsetState;

        #region Network Server Only
        private OffsetServer<Scene> server = null;
        #endregion

        public OffsetViewRegistry(LocalOffsetState state, bool logging = true)
        {
            this.logging = logging;
            this.localOffsetState = state;
            if (logging)
                UnityEngine.Debug.Log("(Server/Client) Instantiated OffsetViewRegistry");
        }

        public void RegisterServer(OffsetServer<Scene> server)
        {
            this.server = server;
        }
        public void RegisterView(IOffsetObject<Scene> view, Action<IOffsetObject<Scene>> OnBeforeRegister, bool isOwner)
        {
            if (server != null)
            {
                OnBeforeRegister?.Invoke(view);
                server.RegisterView(view);
                if (logging)
                    Debug.Log($"Registered View {view.GetName()}");
            }
            else if (logging)
                Debug.LogWarning($"Skipped registering {view.GetName()} as server is not initialized on this instance");

            if (isOwner)
            {
                localOffsetState.TrySetMainView(view);
                if (logging)
                    Debug.Log($"{view.GetName()} is Main View on this instance");
            }
        }

        public void UnregisterView(IOffsetObject<Scene> view)
        {
            if (server != null)
                server.UnregisterView(view);
        }
        public int CountRegisteredViews() => server.RegisteredViewCount();
        public int CountViews() => server.ActualViewCount();
    }
}
