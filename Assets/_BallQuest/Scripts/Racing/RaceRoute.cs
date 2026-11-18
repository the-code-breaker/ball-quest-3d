using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    /// <summary>Ordered three-dimensional route distance; progress is gated and never derived from world Z.</summary>
    public sealed class RaceRoute
    {
        public readonly Vector3[] Points;
        public readonly float[] Distances;
        public readonly int[] GateIndices;
        public readonly float[] Widths;
        public readonly RaceAlternative[] Alternatives;
        public float Length=>Distances.Length==0?0:Distances[Distances.Length-1];

        public RaceRoute(RaceTrackDefinition definition)
        {
            var values=new List<Vector3>();
            if(definition.route!=null)
                foreach(var point in definition.route)
                    if(values.Count==0||Vector3.Distance(values[values.Count-1],point)>.05f)values.Add(point);
            if(values.Count<2&&definition.course)
            {
                values.Clear();values.Add(definition.course.spawn-Vector3.up*.6f);
                foreach(var piece in definition.course.tracks)
                {
                    var rotation=Quaternion.Euler(piece.rotation);
                    var end=piece.position+rotation*Vector3.up*piece.size.y*.5f+rotation*Vector3.forward*piece.size.z*.5f;
                    if(Vector3.Distance(values[values.Count-1],end)>.05f)values.Add(end);
                }
                values.Add(definition.course.finish-Vector3.up*.7f);
            }
            if(values.Count<2)throw new ArgumentException("A race needs at least two ordered route points.");
            Points=values.ToArray();Distances=new float[Points.Length];Widths=new float[Points.Length];
            var gates=new List<int>();float previousGate=0,airborneUntil=-1;
            for(int i=0;i<Points.Length;i++)
            {
                if(i>0)Distances[i]=Distances[i-1]+Vector3.Distance(Points[i-1],Points[i]);
                Widths[i]=RoadWidth(definition.course,Points[i]);
                if(i>0)
                {
                    float drop=Points[i-1].y-Points[i].y;
                    var horizontal=Points[i]-Points[i-1];horizontal.y=0;
                    if(drop>2&&horizontal.magnitude<12)
                        airborneUntil=Mathf.Max(airborneUntil,Distances[i]+25*Mathf.Sqrt(2*drop/9.81f)+5);
                }
                // A logical gate is placed on the long catch after a drop, not vertically below its lip.
                if(i>0&&(Distances[i]>=airborneUntil||i==Points.Length-1)&&(Distances[i]-previousGate>=18||i==Points.Length-1))
                {gates.Add(i);previousGate=Distances[i];}
            }
            GateIndices=gates.ToArray();
            var options=new List<RaceAlternative>();
            if(definition.alternatives!=null)foreach(var option in definition.alternatives)
            {
                if(option==null||option.points==null||option.points.Length<2||option.entryRouteIndex<0||option.exitRouteIndex<=option.entryRouteIndex||option.exitRouteIndex>=Points.Length)continue;
                float width=RoadWidth(definition.course,option.points[option.points.Length/2]);
                options.Add(new RaceAlternative(option,new RaceRoute(option.points,width),Distances[option.entryRouteIndex],Distances[option.exitRouteIndex]));
            }
            Alternatives=options.ToArray();
        }

        RaceRoute(Vector3[] points,float width)
        {
            Points=(Vector3[])points.Clone();Distances=new float[Points.Length];Widths=new float[Points.Length];
            var gates=new List<int>();float last=0;
            for(int i=0;i<Points.Length;i++)
            {
                Widths[i]=width;if(i==0)continue;
                Distances[i]=Distances[i-1]+Vector3.Distance(Points[i-1],Points[i]);
                if(Distances[i]-last>=18||i==Points.Length-1){gates.Add(i);last=Distances[i];}
            }
            GateIndices=gates.ToArray();Alternatives=Array.Empty<RaceAlternative>();
        }

        static float RoadWidth(LevelDefinition course,Vector3 point)
        {
            float best=float.PositiveInfinity,width=12;
            if(!course||course.tracks==null)return width;
            foreach(var piece in course.tracks)
            {
                var q=Quaternion.Euler(piece.rotation);var forward=q*Vector3.forward*piece.size.z*.5f;
                var center=piece.position+q*Vector3.up*piece.size.y*.5f;
                var a=center-forward;var b=center+forward;var p=Closest(point,a,b,out _);
                float distance=(p-point).sqrMagnitude;
                if(distance<best){best=distance;width=piece.size.x;}
            }
            return Mathf.Max(2,width);
        }

        public Vector3 Sample(float distance,out Vector3 tangent,out float width)
        {
            int segment=FindSegment(distance);float span=Distances[segment+1]-Distances[segment];
            float t=span>.0001f?Mathf.Clamp01((distance-Distances[segment])/span):0;
            tangent=(Points[segment+1]-Points[segment]).normalized;
            width=Mathf.Lerp(Widths[segment],Widths[segment+1],t);
            return Vector3.Lerp(Points[segment],Points[segment+1],t);
        }

        public int FindSegment(float distance)
        {
            int low=0,high=Distances.Length-2;
            while(low<high){int mid=(low+high+1)/2;if(Distances[mid]<=distance)low=mid;else high=mid-1;}
            return low;
        }

        public static Vector3 Closest(Vector3 point,Vector3 a,Vector3 b,out float t)
        {
            var direction=b-a;float length=direction.sqrMagnitude;
            t=length>.000001f?Mathf.Clamp01(Vector3.Dot(point-a,direction)/length):0;
            return a+direction*t;
        }

        public float ProjectAnywhere(Vector3 point)
        {
            float best=float.PositiveInfinity,result=0;
            for(int i=0;i<Points.Length-1;i++)
            {
                var closest=Closest(point,Points[i],Points[i+1],out float t);float d=(point-closest).sqrMagnitude;
                if(d<best){best=d;result=Mathf.Lerp(Distances[i],Distances[i+1],t);}
            }
            return result;
        }
    }

    public sealed class RaceAlternative
    {
        public readonly RaceRouteOption Definition;
        public readonly RaceRoute Path;
        public readonly float EntryDistance;
        public readonly float ExitDistance;
        public RaceAlternative(RaceRouteOption definition,RaceRoute path,float entry,float exit)
        {Definition=definition;Path=path;EntryDistance=entry;ExitDistance=exit;}
    }

    /// <summary>A competitor can only cross route gates in their authored order.</summary>
    public sealed class RaceRouteCursor
    {
        public float Distance {get;private set;}
        public int NextGate {get;private set;}
        public bool AllGatesPassed=>NextGate>=route.GateIndices.Length;
        public float Progress=>route.Length>.001f?Mathf.Clamp01(Distance/route.Length):0;
        public RaceAlternative ActiveAlternative {get;private set;}
        public RaceAlternative ChosenAlternative=>ActiveAlternative??chosenAlternative;
        public int CompletedAlternatives {get;private set;}
        public float RemainingDistance=>ActiveAlternative==null?Mathf.Max(0,route.Length-Distance):Mathf.Max(0,ActiveAlternative.Path.Length-alternativeCursor.Distance)+Mathf.Max(0,route.Length-ActiveAlternative.ExitDistance);
        readonly RaceRoute route;
        RaceAlternative chosenAlternative;
        RaceRouteCursor alternativeCursor;
        Vector3 previous;
        bool initialized;

        public RaceRouteCursor(RaceRoute value,Vector3 spawn){route=value;Reset(0,spawn);}

        public void Reset(float distance,Vector3 position)
        {
            Distance=Mathf.Clamp(distance,0,route.Length);NextGate=0;
            ActiveAlternative=null;chosenAlternative=null;alternativeCursor=null;
            while(NextGate<route.GateIndices.Length&&route.Distances[route.GateIndices[NextGate]]<=Distance+.1f)NextGate++;
            previous=position;initialized=true;
        }

        public bool ChooseAlternative(int index)
        {
            if(index<0||index>=route.Alternatives.Length||ActiveAlternative!=null)return false;
            var alternative=route.Alternatives[index];
            if(Distance<alternative.EntryDistance-28||Distance>alternative.EntryDistance+5)return false;
            chosenAlternative=alternative;return true;
        }
        public void CancelAlternativeChoice(){if(ActiveAlternative==null)chosenAlternative=null;}
        public Vector3 SampleAhead(float ahead,out Vector3 tangent,out float width)
        {
            var alternative=ChosenAlternative;
            if(alternative!=null)
            {
                float offset=ActiveAlternative!=null?alternativeCursor.Distance+ahead:Distance+ahead-alternative.EntryDistance;
                if(offset>=0)
                {
                    if(offset<=alternative.Path.Length)return alternative.Path.Sample(offset,out tangent,out width);
                    return route.Sample(alternative.ExitDistance+offset-alternative.Path.Length,out tangent,out width);
                }
            }
            return route.Sample(Distance+ahead,out tangent,out width);
        }

        public void Update(Vector3 position,float deltaTime)
        {
            if(!initialized){previous=position;initialized=true;return;}
            // Respawns must use Reset. A discontinuous move cannot award race progress.
            if(Vector3.Distance(previous,position)>Mathf.Max(2,45*deltaTime+1))
            {previous=position;return;}
            if(ActiveAlternative==null)TryEnterAlternative(position);
            if(ActiveAlternative!=null)
            {
                alternativeCursor.Update(position,deltaTime);
                Distance=Mathf.Lerp(ActiveAlternative.EntryDistance,ActiveAlternative.ExitDistance,alternativeCursor.Progress);
                while(NextGate<route.GateIndices.Length&&route.Distances[route.GateIndices[NextGate]]<=Distance+.1f)NextGate++;
                var end=ActiveAlternative.Path.Points[ActiveAlternative.Path.Points.Length-1];
                if(alternativeCursor.Progress>=.97f&&Vector3.Distance(position,end+Vector3.up*.5f)<Mathf.Max(3,ActiveAlternative.Path.Widths[0]*.5f))
                {
                    Distance=ActiveAlternative.ExitDistance;CompletedAlternatives++;
                    ActiveAlternative=null;chosenAlternative=null;alternativeCursor=null;
                }
                previous=position;return;
            }
            int gateBudget=4;
            while(NextGate<route.GateIndices.Length&&gateBudget-->0)
            {
                int index=route.GateIndices[NextGate];var gate=route.Points[index];
                var forward=route.Points[index]-route.Points[Mathf.Max(0,index-1)];forward.y=0;
                if(forward.sqrMagnitude<.001f)forward=Vector3.forward;forward.Normalize();
                float before=Vector3.Dot(previous-gate,forward),after=Vector3.Dot(position-gate,forward);
                var lateral=Vector3.ProjectOnPlane(position-gate,forward);lateral.y=0;
                bool crossing=before<=1.5f&&after>=-.5f;
                bool close=Vector3.Distance(position,gate+Vector3.up*.5f)<Mathf.Max(3,route.Widths[index]*.5f);
                if((crossing||close)&&lateral.magnitude<=route.Widths[index]*.5f+1&&Mathf.Abs(position.y-gate.y)<3)
                {NextGate++;continue;}
                break;
            }
            int first=NextGate==0?0:route.GateIndices[Mathf.Max(0,NextGate-1)];
            first=Mathf.Max(0,first-2);
            int last=NextGate<route.GateIndices.Length?route.GateIndices[NextGate]:route.Points.Length-1;
            float best=float.PositiveInfinity,result=Distance;
            for(int i=first;i<last;i++)
            {
                var point=RaceRoute.Closest(position,route.Points[i],route.Points[i+1],out float t);
                float d=(point-position).sqrMagnitude;
                if(d<best){best=d;result=Mathf.Lerp(route.Distances[i],route.Distances[i+1],t);}
            }
            if(NextGate<route.GateIndices.Length)result=Mathf.Min(result,route.Distances[route.GateIndices[NextGate]]);
            Distance=Mathf.Clamp(result,0,route.Length);previous=position;
        }

        void TryEnterAlternative(Vector3 position)
        {
            foreach(var alternative in route.Alternatives)
            {
                bool requested=chosenAlternative==alternative;
                if(Distance<alternative.EntryDistance-.6f||Distance>alternative.EntryDistance+15)continue;
                if(Vector3.Distance(position,alternative.Path.Points[0]+Vector3.up*.5f)>16)continue;
                float branchDistance=alternative.Path.ProjectAnywhere(position);
                if(branchDistance>Mathf.Min(15,alternative.Path.Length*.3f))continue;
                var branchPoint=alternative.Path.Sample(branchDistance,out _,out float width);
                float branchError=Vector3.Distance(position,branchPoint+Vector3.up*.5f);
                if(branchError>Mathf.Max(1.5f,width*.5f+.5f))continue;
                var primaryPoint=route.Sample(Distance,out _,out _);
                if(!requested&&(branchDistance<2||Vector3.Distance(position,primaryPoint+Vector3.up*.5f)<branchError+.2f))continue;
                ActiveAlternative=alternative;alternativeCursor=new RaceRouteCursor(alternative.Path,position);
                alternativeCursor.Reset(0,position);return;
            }
        }
    }
}
