using TMPro;
using UnityEngine;

namespace BallQuest
{
    /// <summary>World labels recede before the moving camera can pass through their glyphs.</summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class CourseSignVisibility : MonoBehaviour
    {
        public float hiddenDistance=8;
        public float fullDistance=18;
        TextMeshPro label;
        public float VisibilityAlpha=>label?label.alpha:1;
        void Awake()=>label=GetComponent<TextMeshPro>();
        void LateUpdate()
        {
            var camera=Camera.main;
            if(!camera||!label)return;
            var offset=transform.position-camera.transform.position;
            float alpha=Vector3.Dot(camera.transform.forward,offset)<=.5f?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(hiddenDistance,Mathf.Max(hiddenDistance+.1f,fullDistance),offset.magnitude));
            if(Mathf.Abs(label.alpha-alpha)>.002f)label.alpha=alpha;
        }
    }
}
