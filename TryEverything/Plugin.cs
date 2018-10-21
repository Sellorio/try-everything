using IllusionPlugin;
using System;
using TryEverything.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TryEverything
{
    public class Plugin : IPlugin
    {
        private const int MainMenuIndex = 1;

        public string Name => "TryEverything";
        public string Version => "0.0.1";

        internal static TryEverythingHost HostInstance { get; private set; }

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            try
            {
                if (arg1.buildIndex == MainMenuIndex && HostInstance == null)
                {
                    var hostGameObject = new GameObject("TryEverythingHost")
                    {
                        isStatic = true
                    };

                    HostInstance = hostGameObject.AddComponent<TryEverythingHost>();
                    hostGameObject.AddComponent<AcceptRejectInterfaceManager>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
