using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    public static class ManualGameplayUpgradeGenerator
    {
        public static void Generate(GameCatalog catalog)
        {
            if(catalog.manualControlVersion>=2)return;
            foreach(var profile in catalog.balls)
            {
                if(!profile)continue;
                profile.acceleration=27;profile.braking=32;
                profile.rollingResistance=.55f;profile.groundedViscousResistance=.32f;
                profile.rollingTorqueRatio=.4f;profile.tractionTorque=5;
                profile.steeringResponseTime=.018f;profile.steeringReleaseTime=.018f;
                profile.reverseSpeed=profile.maxSpeed;
                EditorUtility.SetDirty(profile);
            }
            catalog.manualControlVersion=2;EditorUtility.SetDirty(catalog);
        }
    }
}
