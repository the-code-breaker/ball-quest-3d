using UnityEngine;

namespace BallQuest
{
    public sealed class RaceCompetitor : MonoBehaviour
    {
        public int Id {get;private set;}
        public string DisplayName {get;private set;}
        public bool IsPlayer=>Id==0;
        public BallController Ball {get;private set;}
        public RaceManager Manager {get;private set;}
        public int Position {get;internal set;}
        public bool Finished {get;internal set;}
        public bool Eliminated {get;internal set;}
        public float FinishTime {get;internal set;}
        public int Falls {get;internal set;}
        public int Contacts {get;private set;}
        public float Progress=>Cursor==null?0:Cursor.Progress;
        public float Distance=>Cursor==null?0:Cursor.Distance;
        public int CheckpointIndex {get;internal set;}=-1;
        public bool Recovering {get;internal set;}
        public float RecoveryRemaining {get;internal set;}
        public RaceRouteCursor Cursor {get;private set;}
        public RaceDriver Driver {get;internal set;}
        internal Vector3 CheckpointPosition;
        internal float CheckpointDistance;
        internal float LastAdvanceAt;
        internal float LastAdvanceDistance;
        internal Vector3 PreviousPhysicsPosition;
        internal float EliminatedTime;
        internal int EliminationPlace;
        internal bool FinishArmed;
        internal bool PendingFinish;

        public void Initialize(RaceManager manager,int id,string name,BallController ball,RaceRoute route,Vector3 spawn,float distance)
        {
            Manager=manager;Id=id;DisplayName=name;Ball=ball;
            Cursor=new RaceRouteCursor(route,spawn);Cursor.Reset(distance,spawn);
            CheckpointPosition=spawn;CheckpointDistance=distance;PreviousPhysicsPosition=spawn;
            LastAdvanceDistance=distance;LastAdvanceAt=0;Position=id+1;
            Finished=Eliminated=Recovering=false;FinishTime=0;Falls=0;Contacts=0;CheckpointIndex=-1;
        }

        public void OnCourseTrigger(bool finish,int index,Vector3 respawn)
        {
            if(Manager)Manager.HandleGate(this,finish,index,respawn);
        }

        void OnCollisionEnter(Collision collision)
        {
            if(!Manager||Manager.State!=RaceState.Racing||Finished||Eliminated)return;
            if(collision.relativeVelocity.magnitude>2&&collision.rigidbody&&collision.rigidbody.TryGetComponent<RaceCompetitor>(out _))Contacts++;
        }
    }
}
