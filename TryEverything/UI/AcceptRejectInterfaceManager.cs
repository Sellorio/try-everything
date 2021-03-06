﻿using SongLoaderPlugin;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using TMPro;
using TryEverything.Data;
using TryEverything.Helpers;
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
                yield return new WaitUntil(() => !Resources.FindObjectsOfTypeAll<ResultsViewController>().Any()); // wait until the results view is gone

                Plugin.Log("Waiting for results view controller.");

                yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(x => ReflectionUtil.GetPrivateField<IDifficultyBeatmap>(x, "_difficultyBeatmap") != null));

                Plugin.Log("Found results view controller.");

                var resultsView = Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault(x => ReflectionUtil.GetPrivateField<IDifficultyBeatmap>(x, "_difficultyBeatmap") != null);

                song = null;

                var levelId = ReflectionUtil.GetPrivateField<IDifficultyBeatmap>(resultsView, "_difficultyBeatmap").level?.levelID ?? string.Empty;

                Plugin.Log("Level Id is " + levelId + ".");

                if (levelId.Length > 32)
                {
                    Button acceptButton = null;
                    Button rejectButton = null;
                    Button blacklistMapperButton = null;
                    Button openInBeastsaberButton = null;

                    acceptButton =
                        CreateButton(
                            resultsView.rectTransform,
                            "TryEverythingAcceptButton",
                            () =>
                            {
                                AcceptSong(song);
                                acceptButton.interactable = false;
                                //rejectButton.gameObject.SetActive(false);
                                //blacklistMapperButton.gameObject.SetActive(false);
                            },
                            "Keep");


                    rejectButton =
                        CreateButton(
                            resultsView.rectTransform,
                            "TryEverythingRejectButton",
                            () =>
                            {
                                RejectSong(song);
                                //acceptButton.gameObject.SetActive(false);
                                rejectButton.interactable = false;
                                //blacklistMapperButton.gameObject.SetActive(false);
                            },
                            "Reject");


                    blacklistMapperButton =
                        CreateButton(
                            resultsView.rectTransform,
                            "TryEverythingRejectButton",
                            () =>
                            {
                                BlacklistMapper(song);
                                //acceptButton.gameObject.SetActive(false);
                                //rejectButton.gameObject.SetActive(false);
                                blacklistMapperButton.interactable = false;
                            },
                            "Blacklist Mapper");


                    openInBeastsaberButton =
                        CreateButton(
                            resultsView.rectTransform,
                            "TryEverythingBeastsaberButton",
                            () =>
                            {
                                try
                                {
                                    Process.Start($"https://bsaber.com/songs/{song.Number}/");
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Log("Failed to open BeastSaber: " + ex.ToString());
                                }

                                openInBeastsaberButton.interactable = false;
                                openInBeastsaberButton.SetText("Done! Check your browser.");
                                StartCoroutine(RestoreOpenInBeastSaberButton(openInBeastsaberButton));
                            },
                            "Open BSaber");

                    ((RectTransform)acceptButton.transform).localPosition = new Vector2(-57, -34);
                    ((RectTransform)rejectButton.transform).localPosition = new Vector2(-57, -23);
                    ((RectTransform)blacklistMapperButton.transform).localPosition = new Vector2(-57, -12);
                    ((RectTransform)openInBeastsaberButton.transform).localPosition = new Vector2(-57, 9);

                    acceptButton.SetText("Checking Song Status...");
                    acceptButton.interactable = false;

                    rejectButton.gameObject.SetActive(false);
                    blacklistMapperButton.gameObject.SetActive(false);

                    System.Collections.Generic.IEnumerable<CustomSong> getSongFromLevelEnumerator;

                    try
                    {
                        getSongFromLevelEnumerator = _beatSaverService.GetSongFromLevel(levelId);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log(ex.ToString());
                        continue;
                    }

                    var enumerator = getSongFromLevelEnumerator.GetEnumerator();

                    while (song == null)
                    {
                        yield return null;

                        try
                        {
                            enumerator.MoveNext();
                            song = enumerator.Current;
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log(ex.ToString());
                            break;
                        }
                    }

                    Plugin.Log("Retrieved song details for " + song.Title + " mapped by " + song.AuthorName + ".");

                    try
                    {
                        if (song == null || !Plugin.HostInstance.IsPendingSong(song.Title))
                        {
                            acceptButton.gameObject.SetActive(false);

                            var songTitle = levelId.Substring(33);
                            var indexOfSeparator = songTitle.IndexOf("∎");

                            if (indexOfSeparator != -1)
                            {
                                songTitle = songTitle.Substring(0, indexOfSeparator);

                                if (Plugin.HostInstance.RejectSong(songTitle))
                                {
                                    StartCoroutine(Plugin.HostInstance.GetSongBatch(true));
                                    SongLoader.Instance.RemoveSongWithLevelID(levelId);
                                }
                            }
                        }
                        else
                        {
                            acceptButton.SetText("Keep");
                            acceptButton.interactable = true;

                            rejectButton.gameObject.SetActive(true);
                            blacklistMapperButton.gameObject.SetActive(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log(ex.ToString());
                    }
                }
            }
        }

        private void AcceptSong(CustomSong song)
        {
            try
            {
                if (Plugin.HostInstance.AcceptSong(song))
                {
                    StartCoroutine(Plugin.HostInstance.GetSongBatch(true));
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
                    StartCoroutine(Plugin.HostInstance.GetSongBatch(true));
                    SongLoader.Instance.RemoveSongWithLevelID(ReflectionUtil.GetPrivateField<IDifficultyBeatmap>(Resources.FindObjectsOfTypeAll<ResultsViewController>().First(), "_difficultyBeatmap").level.levelID);
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
                    StartCoroutine(Plugin.HostInstance.GetSongBatch(true));
                    SongLoader.Instance.RemoveSongWithLevelID(ReflectionUtil.GetPrivateField<IDifficultyBeatmap>(Resources.FindObjectsOfTypeAll<ResultsViewController>().First(), "_difficultyBeatmap").level.levelID);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log(ex.ToString());
            }
        }

        private IEnumerator RestoreOpenInBeastSaberButton(Button button)
        {
            yield return new WaitForSeconds(5);
            button.interactable = true;
            button.SetText("Open In BSaber");
        }

        private Button CreateButton(RectTransform parent, string name, UnityAction onClick, string text)
        {
            var button = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "ResetButton"), parent, false);
            DestroyImmediate(button.GetComponent<SignalOnUIButtonClick>());
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);

            if (text != null)
            {
                button.SetText(text);
            }

            return button;
        }
    }
}
