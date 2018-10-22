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

        public AcceptRejectInterfaceManager()
            : this(new BeatSaverService(TryEverythingHost.BeatSaberPath))
        {
        }

        public AcceptRejectInterfaceManager(IBeatSaverService beatSaverService)
        {
            _beatSaverService = beatSaverService;
        }

        public void Awake()
        {
            StartCoroutine(WaitForResultsScreen());
        }

        private IEnumerator WaitForResultsScreen()
        {
            var song = default(CustomSong);

            while (true)
            {
                Plugin.Log("Waiting for results view controller.");

                yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(x => x.difficultyLevel != null));

                Plugin.Log("Found results view controller.");

                var resultsView = Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault(x => x.difficultyLevel != null);

                if (resultsView == null || resultsView.difficultyLevel == null)
                {
                    continue;
                }

                song = null;

                var levelId = resultsView.difficultyLevel.level.levelID;

                Plugin.Log("Completed level: " + levelId + ".");

                if (levelId.Length > 32)
                {
                    Plugin.Log("Retrieving song details for level.");

                    foreach (var yield in _beatSaverService.GetSongFromLevel(levelId))
                    {
                        song = yield;
                        yield return null;
                    }

                    if (song == null) // songs that no longer exist on beatsaver are auto rejected - sorry!
                    {
                        var songTitle = levelId.Substring(33);
                        songTitle = songTitle.Substring(0, songTitle.IndexOf("∎"));

                        if (Plugin.HostInstance.RejectSong(songTitle))
                        {
                            StartCoroutine(Plugin.HostInstance.GetSongBatch());
                            SongLoader.Instance.RemoveSongWithLevelID(levelId);
                        }
                    }
                    else if (Plugin.HostInstance.IsPendingSong(song))
                    {
                        try
                        {
                            CreateButtons(resultsView, song);
                            Plugin.Log("Buttons created.");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log(ex.ToString());
                        }
                    }
                }

                yield return new WaitUntil(() => !Resources.FindObjectsOfTypeAll<ResultsViewController>().Any()); // wait until the results view is gone
            }
        }

        private void CreateButtons(ResultsViewController resultsView, CustomSong song)
        {
            Button acceptButton = null;
            Button rejectButton = null;
            Button blacklistMapperButton = null;

            acceptButton =
                CreateButton(
                    resultsView.rectTransform,
                    "TryEverythingAcceptButton",
                    () =>
                    {
                        AcceptSong(song);
                        acceptButton.interactable = false;
                        Destroy(rejectButton);
                        Destroy(blacklistMapperButton);
                    },
                    "Keep");


            rejectButton =
                CreateButton(
                    resultsView.rectTransform,
                    "TryEverythingRejectButton",
                    () =>
                    {
                        RejectSong(song);
                        Destroy(acceptButton);
                        rejectButton.interactable = false;
                        Destroy(blacklistMapperButton);
                    },
                    "Reject");


            blacklistMapperButton =
                CreateButton(
                    resultsView.rectTransform,
                    "TryEverythingRejectButton",
                    () =>
                    {
                        BlacklistMapper(song);
                        Destroy(acceptButton);
                        Destroy(rejectButton);
                        blacklistMapperButton.interactable = false;
                    },
                    "Blacklist Mapper");

            ((RectTransform)acceptButton.transform).anchoredPosition = new Vector2(-125f, 36f);
            ((RectTransform)rejectButton.transform).anchoredPosition = new Vector2(-125f, 23f);
            ((RectTransform)blacklistMapperButton.transform).anchoredPosition = new Vector2(-125f, 10f);
        }

        private void AcceptSong(CustomSong song)
        {
            try
            {
                if (Plugin.HostInstance.AcceptSong(song))
                {
                    StartCoroutine(Plugin.HostInstance.GetSongBatch());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
            }
        }

        private void RejectSong(CustomSong song)
        {
            try
            {
                if (Plugin.HostInstance.RejectSong(song))
                {
                    StartCoroutine(Plugin.HostInstance.GetSongBatch());
                    SongLoader.Instance.RemoveSongWithLevelID(Resources.FindObjectsOfTypeAll<ResultsViewController>().First().difficultyLevel.level.levelID);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Plugins/TryEverything] " + ex.ToString());
            }
        }

        private void BlacklistMapper(CustomSong song)
        {
            try
            {
                if (Plugin.HostInstance.IgnoreAuthor(song))
                {
                    StartCoroutine(Plugin.HostInstance.GetSongBatch());
                    SongLoader.Instance.RemoveSongWithLevelID(Resources.FindObjectsOfTypeAll<ResultsViewController>().First().difficultyLevel.level.levelID);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log(ex.ToString());
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
