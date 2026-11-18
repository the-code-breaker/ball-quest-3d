using UnityEngine;

namespace BallQuest
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect previous;
        Vector2 screen;
        void Update()
        {
            Rect area = Screen.safeArea;
            if (previous == area && screen == new Vector2(Screen.width, Screen.height)) return;
            previous = area; screen = new Vector2(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.Scale(area.position, new Vector2(1f / Screen.width, 1f / Screen.height));
            rect.anchorMax = Vector2.Scale(area.position + area.size, new Vector2(1f / Screen.width, 1f / Screen.height));
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
