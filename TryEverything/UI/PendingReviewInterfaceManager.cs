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

                    var standardLevelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();

                    var standardLevelListViewController = ReflectionUtil.GetPrivateField<StandardLevelListViewController>(standardLevelSelectionFlowCoordinator, "_levelListViewController");
                    standardLevelListViewController.didSelectLevelEvent += OnSelectLevel;

                    if (standardLevelListViewController.selectedLevel != null)
                    {
                        UpdateDetailsUI(standardLevelSelectionFlowCoordinator, standardLevelListViewController.selectedLevel.levelID);
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

        private void OnSelectLevel(StandardLevelListViewController sender, IStandardLevel selectedLevel)
        {
            var standardLevelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            UpdateDetailsUI(standardLevelSelectionFlowCoordinator, selectedLevel.levelID);
        }

        private void UpdateDetailsUI(StandardLevelSelectionFlowCoordinator flowCoordinator, string selectedLevel)
        {
            if (_pendingReviewTextControl != null)
            {
                _pendingReviewTextControl.gameObject.SetActive(false);
            }
            
            StartCoroutine(HandlePendingReviewAfterResultsScreenClosed(flowCoordinator, selectedLevel));
        }

        private IEnumerator HandlePendingReviewAfterResultsScreenClosed(StandardLevelSelectionFlowCoordinator flowCoordinator, string selectedLevel)
        {
            yield return new WaitUntil(() => !Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(x => x.isActiveAndEnabled)); // wait until the results view is gone

            try
            {
                var songDetailViewController = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(flowCoordinator, "_levelDetailViewController");
                var detailsRectTransform = songDetailViewController.GetComponent<RectTransform>();

                if (_pendingReviewTextControl == null)
                {
                    var result = new GameObject("TryEverything.PendingReviewMessage", typeof(RectTransform));
                    result.transform.position = new Vector3(1.35f, 0.95f, 2.395f);
                    result.transform.eulerAngles = new Vector3(0, 0, -45f);
                    result.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                    var canvas = result.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    var rectTransform = canvas.transform as RectTransform;
                    rectTransform.sizeDelta = new Vector2(100, 50);

                    var textObject = new GameObject("Text", typeof(RectTransform));
                    rectTransform = textObject.transform as RectTransform;
                    rectTransform.SetParent(canvas.transform, false);
                    rectTransform.anchoredPosition = new Vector2(0, 0);
                    rectTransform.sizeDelta = new Vector2(100, 20);

                    _pendingReviewTextControl = textObject.AddComponent<TextMeshProUGUI>();
                    _pendingReviewTextControl.text = "Pending\n        Review";
                    _pendingReviewTextControl.fontSize = 20f;
                    _pendingReviewTextControl.color = new Color(1, 0, 0, 0.2f);
                }

                if (selectedLevel.Length > 32)
                {
                    var songTitle = selectedLevel.Substring(33);
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
