using System.Collections.Generic;
using UnityEngine;
namespace BallQuest
{
    public static class CampaignGeometry
    {
        public const int Skip=1<<8;
        public static TrackPiece[] Expand(LevelDefinition level)
        {
            if(!level.streamedCampaign)return level.tracks;
            var output=new List<TrackPiece>(level.routePoints.Length+level.bonusTracks.Length);
            for(int i=0;i<level.routePoints.Length-1;i++)
            {
                int flags=level.roadFlags[i];if((flags&Skip)!=0)continue;
                Vector3 a=level.routePoints[i],b=level.routePoints[i+1],d=b-a;if(d.sqrMagnitude<.0001f)continue;
                var q=Quaternion.LookRotation(d,Vector3.up);var style=(TrackStyle)((flags>>1)&15);
                output.Add(new TrackPiece{position=(a+b)*.5f-q*Vector3.up*.4f,size=new Vector3(level.routeWidths[i],.8f,d.magnitude+.08f),rotation=q.eulerAngles,style=style,rails=(flags&1)!=0,
                    surface=style==TrackStyle.Ice?TrackSurface.Ice:style==TrackStyle.HighGrip?TrackSurface.HighGrip:style==TrackStyle.Slippery?TrackSurface.Slippery:TrackSurface.Standard});
            }
            output.AddRange(level.bonusTracks);return output.ToArray();
        }
    }
}
