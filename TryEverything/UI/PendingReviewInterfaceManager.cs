using SongLoaderPlugin;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TryEverything.UI
{
    class PendingReviewInterfaceManager : MonoBehaviour
    {
        private TextMeshProUGUI _pendingReviewTextControl;

        public void Awake()
        {
            StartCoroutine(Loop());
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any());

                try
                {
                    var mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                    var mainMenuRectTransform = mainMenuViewController.transform as RectTransform;

                    var standardLevelListViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultyViewController>().First();
                    standardLevelListViewController.didSelectDifficultyEvent += OnSelectLevel;

                    if (standardLevelListViewController.selectedDifficultyBeatmap != null)
                    {
                        UpdateDetailsUI(standardLevelListViewController.selectedDifficultyBeatmap);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log("Failed to handle main menu setup for Pending Review indicator: " + ex.ToString());
                    throw;
                }

                yield return new WaitUntil(() => !Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any());

                _pendingReviewTextControl = null;
            }
        }

        private void OnSelectLevel(BeatmapDifficultyViewController sender, IDifficultyBeatmap selectedDifficulty)
        {
            UpdateDetailsUI(selectedDifficulty);
        }

        private void UpdateDetailsUI(IDifficultyBeatmap selectedDifficulty)
        {
            if (_pendingReviewTextControl != null)
            {
                _pendingReviewTextControl.gameObject.SetActive(false);
            }
            
            StartCoroutine(HandlePendingReviewAfterResultsScreenClosed(selectedDifficulty));
        }

        private IEnumerator HandlePendingReviewAfterResultsScreenClosed(IDifficultyBeatmap selectedDifficulty)
        {
            yield return new WaitUntil(() => !Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(x => x.isActiveAndEnabled)); // wait until the results view is gone

            try
            {
                var songDetailViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultyViewController>().First();
                var detailsRectTransform = songDetailViewController.GetComponent<RectTransform>();

                if (_pendingReviewTextControl == null)
                {
                    var result = new GameObject("TryEverything.PendingReviewMessage", typeof(RectTransform));
                    result.transform.position = new Vector3(1.3f, 0.45f, 2.22f);
                    result.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

                    var canvas = result.AddComponent<Canvas>();
                    canvas.sortingOrder = 99999;
                    canvas.renderMode = RenderMode.WorldSpace;
                    var rectTransform = canvas.transform as RectTransform;
                    rectTransform.sizeDelta = new Vector2(100, 50);

                    var textObject = new GameObject("Text", typeof(RectTransform));
                    rectTransform = textObject.transform as RectTransform;
                    rectTransform.SetParent(canvas.transform, false);
                    rectTransform.anchoredPosition = new Vector2(0, 0);
                    rectTransform.sizeDelta = new Vector2(100, 20);

                    _pendingReviewTextControl = textObject.AddComponent<TextMeshProUGUI>();
                    _pendingReviewTextControl.text = "Pending Review";
                    _pendingReviewTextControl.fontSize = 20f;
                    _pendingReviewTextControl.color = new Color(1, 0, 0, 0.6f);
                }

                if (selectedDifficulty.level.levelID.Length > 32)
                {
                    var songTitle = selectedDifficulty.level.levelID.Substring(33);
                    songTitle = songTitle.Substring(0, songTitle.IndexOf("∎"));
                    var showText = Plugin.HostInstance.IsPendingSong(songTitle);

                    Plugin.Log("Updating pending review text visibility for song \"" + songTitle + "\".");
                    Plugin.Log(showText ? "Showing pending review text." : "Hiding pending review text.");

                    _pendingReviewTextControl.gameObject.SetActive(showText);
                }
                else
                {
                    Plugin.Log("Hiding pending review text for non-custom song.");

                    _pendingReviewTextControl.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log("Failed to setup Pending Review indicator: " + ex.ToString());
            }
        }
    }
}
