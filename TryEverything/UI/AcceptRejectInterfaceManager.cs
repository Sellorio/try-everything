using SongLoaderPlugin;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using TryEverything.Data;
using TryEverything.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TryEverything.UI
{
    class AcceptRejectInterfaceManager : MonoBehaviour
    {
        private readonly IBeatSaverService _beatSaverService;
        private CustomSong _song;

        public AcceptRejectInterfaceManager()
            : this(new BeatSaverService(TryEverythingHost.BeatSaberPath))
        {
        }

        public AcceptRejectInterfaceManager(IBeatSaverService beatSaverService)
        {
            _beatSaverService = beatSaverService;
        }

        public void Start()
        {
            StartCoroutine(WaitForResultsScreen());
        }

        private IEnumerator WaitForResultsScreen()
        {
            _song = null;

            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(); });

            var resultsView = Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault();

            if (resultsView == null || resultsView.difficultyLevel == null)
            {
                yield break;
            }

            var levelId = resultsView.difficultyLevel.level.levelID;

            if (levelId.Length > 32)
            {
                foreach (var yield in _beatSaverService.GetSongFromLevel(levelId))
                {
                    _song = yield;
                    yield return null;
                }

                try
                {
                    CreateButtons(resultsView);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
                }
            }
        }

        private void CreateButtons(ResultsViewController resultsView)
        {
            if (Plugin.HostInstance.IsPendingSong(_song))
            {
                var acceptButton = CreateButton(resultsView.rectTransform, "TryEverythingAcceptButton", AcceptSong, "✓");
                ((RectTransform)acceptButton.transform).anchoredPosition = new Vector2(0f, 45f);

                var rejectButton = CreateButton(resultsView.rectTransform, "TryEverythingRejectButton", RejectSong, "X");
                ((RectTransform)acceptButton.transform).anchoredPosition = new Vector2(0f, 26f);
            }
        }

        private void AcceptSong()
        {
            try
            {
                Plugin.HostInstance.AcceptSong(_song);
                StartCoroutine(Plugin.HostInstance.GetSongBatch());
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
            }
        }

        private void RejectSong()
        {
            try
            {
                Plugin.HostInstance.RejectSong(_song);
                StartCoroutine(Plugin.HostInstance.GetSongBatch());
                SongLoader.Instance.RemoveSongWithLevelID(Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault().difficultyLevel.level.levelID);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
            }
        }

        private Button CreateButton(RectTransform parent, string name, UnityAction onClick, string text)
        {
            var button = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => x.name == "SettingsButton"), parent, false);
            DestroyImmediate(button.GetComponent<SignalOnUIButtonClick>());
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);

            if (text != null)
            {
                var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();

                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }

            return button;
        }
    }
}
