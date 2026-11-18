using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallQuest
{
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        RectTransform rect;
        Vector2 origin;
        bool pressed;
        void Awake() {rect=(RectTransform)transform;origin=rect.anchoredPosition;}
        public void OnPointerDown(PointerEventData e) { if(GetComponent<Button>().interactable)pressed=true; }
        public void OnPointerUp(PointerEventData e) => pressed=false;
        public void OnPointerExit(PointerEventData e) => pressed=false;
        void Update()
        {
            rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+Vector2.down*(pressed?5:0),1-Mathf.Exp(-Time.unscaledDeltaTime*26));
            rect.localScale=Vector3.Lerp(rect.localScale,Vector3.one*(pressed?.975f:1),1-Mathf.Exp(-Time.unscaledDeltaTime*26));
        }
    }
}
