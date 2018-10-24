using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TryEverything.UI
{
    class StatusMessageManager : MonoBehaviour
    {
        private GameObject _statusMessageGameObject;
        private TextMeshProUGUI _text;

        public void Awake()
        {
            StartCoroutine(WaitForMainMenu());
        }

        public void Update()
        {
            if (_statusMessageGameObject != null)
            {
                _text.text = GetStatusText();
                _statusMessageGameObject.GetComponent<Canvas>().enabled = _text.text != null;
            }
        }

        private IEnumerator WaitForMainMenu()
        {
            _statusMessageGameObject = null;

            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any(); });

            var mainMenuView = Resources.FindObjectsOfTypeAll<MainMenuViewController>().FirstOrDefault();

            _statusMessageGameObject = CreateStatusMessageGameObject(out _text);

            yield return new WaitUntil(delegate () { return !Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any(); }); // wait until menu is gone

            StartCoroutine(WaitForMainMenu());
        }

        private static GameObject CreateStatusMessageGameObject(out TextMeshProUGUI text)
        {
            new Vector3(0, 2.5f, 2.5f);
            new Vector3(0, 0, 0);
            new Vector3(0.01f, 0.01f, 0.01f);

            try
            {
                var result = new GameObject("TryEverything.StatusMessage", typeof(RectTransform));
                result.transform.position = new Vector3(0.45f, 2.1f, 2.1f);
                result.transform.eulerAngles = default(Vector3);
                result.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                var canvas = result.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.enabled = false;
                var rectTransform = canvas.transform as RectTransform;
                rectTransform.sizeDelta = new Vector2(100, 50);

                var textObject = new GameObject("Text", typeof(RectTransform));
                rectTransform = textObject.transform as RectTransform;
                rectTransform.SetParent(canvas.transform, false);
                rectTransform.anchoredPosition = new Vector2(0, 0);
                rectTransform.sizeDelta = new Vector2(100, 20);

                text = textObject.AddComponent<TextMeshProUGUI>();
                text.text = GetStatusText();
                text.fontSize = 15f;
                text.color = Color.yellow;

                return result;
            }
            catch (Exception ex)
            {
                Plugin.Log("Failed to create status message UI: " + ex.ToString());
                text = null;
                return null;
            }
        }

        private static string GetStatusText()
        {
            switch (Plugin.HostInstance.Status)
            {
                case HostStatus.Downloading:
                    return "Downloading songs for you to try...";
                case HostStatus.Idle:
                    return null;
                case HostStatus.Refreshing:
                    return "Refreshing songs list...";
                case HostStatus.Waiting:
                    return "Next song will download while you play";
                default:
                    Plugin.Log("Unexpected host status encountered: " + Plugin.HostInstance.Status + ".");
                    return null;
            }
        }
    }
}
