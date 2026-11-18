using System;
using UnityEditor;
using UnityEngine;

namespace BallQuest.Editor
{
    /// <summary>One-time, isolated authoring correction for the Canyon Choice fork.</summary>
    public static class ManualRaceCourseGenerator
    {
        const int Version=1;
        public static void Generate(GameCatalog catalog)
        {
            if(catalog.manualRaceVersion>=Version)return;
            var race=Array.Find(catalog.races??Array.Empty<RaceTrackDefinition>(),value=>value&&value.id==4);
            if(!race||!race.course||race.alternatives==null||race.alternatives.Length==0)
                throw new InvalidOperationException("The manual race upgrade needs the authored Canyon Choice branch.");
            var course=race.course;var branch=race.alternatives[0];
            Vector3 entry=course.routePoints[branch.entryRouteIndex],exit=course.routePoints[branch.exitRouteIndex];
            Vector3 axis=Vector3.ProjectOnPlane(exit-entry,Vector3.up).normalized;
            float length=Vector3.Dot(exit-entry,axis),height=entry.y;
            int corrected=0;
            foreach(var piece in course.tracks)
            {
                float along=Vector3.Dot(piece.position-entry,axis);
                // The elevated spiral crosses this world-Z interval later. Its separate floor must be retained.
                if(along<-.1f||along>length+.1f||Mathf.Abs(piece.position.y-height)>2)continue;
                // Both choices meet at a common contact plane. No raised lip or rail may divide a fork throat.
                piece.rotation=new Vector3(0,piece.rotation.y,0);
                piece.position=new Vector3(piece.position.x,height-piece.size.y*.5f,piece.position.z);
                piece.rails=false;corrected++;
            }
            if(corrected<4)throw new InvalidOperationException("Canyon Choice fork geometry was not found.");
            for(int i=branch.entryRouteIndex;i<=branch.exitRouteIndex;i++)
            {
                Vector3 point=course.routePoints[i];point.y=height;course.routePoints[i]=point;
            }
            race.route=(Vector3[])course.routePoints.Clone();
            foreach(var option in race.alternatives)
                foreach(int i in Indices(option.points.Length))
                {Vector3 point=option.points[i];point.y=height;option.points[i]=point;}
            foreach(var pickup in course.pickups)
            {
                float along=Vector3.Dot(pickup.position-entry,axis);
                if(along>=0&&along<=length&&pickup.position.y>height&&pickup.position.y<height+3)
                    pickup.position=new Vector3(pickup.position.x,height+.85f,pickup.position.z);
            }
            catalog.manualRaceVersion=Version;
            EditorUtility.SetDirty(course);EditorUtility.SetDirty(race);EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Canyon Choice fork corrected: "+corrected+" contact-plane pieces; clear manual fork/merge, no changes to the 55-course campaign.");
        }
        static System.Collections.Generic.IEnumerable<int> Indices(int count)
        {for(int i=0;i<count;i++)yield return i;}
    }
}
