using TMPro;
using UnityEngine.UI;

namespace TryEverything.Helpers
{
    static class ExtensionsForButton
    {
        public static void SetText(this Button button, string text)
        {
            var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }
}
