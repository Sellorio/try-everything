using IllusionPlugin;
using System;
using TryEverything.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TryEverything
{
    public class Plugin : IPlugin
    {
        private const int MainMenuIndex = 0;

        private static GameObject _hostGameObject;

        public string Name => "Try Everything!";
        public string Version => "0.0.1";

        internal static string[] DifficultiesSetting { get; } = ModPrefs.GetString("TryEverything", "Difficulties", "Easy,Normal,Hard,Expert,ExpertPlus", true).Split(',');
        internal static TryEverythingHost HostInstance { get; private set; }

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
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
            try
            {
                if (level == MainMenuIndex && HostInstance == null)
                {
                    _hostGameObject = new GameObject("TryEverythingHost");

                    HostInstance = _hostGameObject.AddComponent<TryEverythingHost>();
                    _hostGameObject.AddComponent<AcceptRejectInterfaceManager>();
                    _hostGameObject.AddComponent<StatusMessageManager>();
                    _hostGameObject.AddComponent<PendingReviewInterfaceManager>();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public static void Log(string message)
        {
            Console.WriteLine("[Plugins/TryEverything] " + message);
        }
    }
}
