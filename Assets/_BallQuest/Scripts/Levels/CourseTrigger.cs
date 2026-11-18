using UnityEngine;

namespace BallQuest
{
    public sealed class CourseTrigger : MonoBehaviour
    {
        public GameSession session;
        public bool finish;
        public int index;
        public Vector3 respawn;
        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<BallController>(out var ball)) return;
            if(ball.TryGetComponent<RaceCompetitor>(out var competitor))
            {competitor.OnCourseTrigger(finish,index,respawn);return;}
            if(!session||session.Ball!=ball)return;
            if (finish) session.Finish(); else session.Checkpoint(index, respawn);
        }
    }
}
