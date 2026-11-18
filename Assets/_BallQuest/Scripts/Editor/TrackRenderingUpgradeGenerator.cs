using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace BallQuest.Editor
{
    public static class TrackRenderingUpgradeGenerator
    {
        public static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline)
        {
            // Retain the existing atlas budget. Two cascades give the nearby rolling
            // surface more useful texels; filtered shadows avoid hard crawling edges.
            pipeline.mainLightShadowmapResolution=2048;
            pipeline.shadowCascadeCount=2;
            var settings=new SerializedObject(pipeline);
            settings.FindProperty("m_SoftShadowsSupported").boolValue=true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowDepthBias=1;
            pipeline.shadowNormalBias=.5f;
            EditorUtility.SetDirty(pipeline);
        }
    }
}
