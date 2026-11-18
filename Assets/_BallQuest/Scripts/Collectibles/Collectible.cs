using UnityEngine;

namespace BallQuest
{
    public sealed class Collectible : MonoBehaviour
    {
        public PickupKind kind;
        public GameSession session;
        bool collected;
        void OnTriggerEnter(Collider other)=>TryCollect(other);
        void OnTriggerStay(Collider other)=>TryCollect(other);
        void TryCollect(Collider other)
        {
            if(collected||!session||!other.TryGetComponent<BallController>(out var ball)||session.Ball!=ball||session.State!=SessionState.Playing)return;
            if(ball.TryGetComponent<RaceCompetitor>(out var competitor))
            {
                if(!competitor.IsPlayer||!competitor.Manager||competitor.Manager.State!=RaceState.Racing)return;
                competitor.Manager.Collect(kind);
            }
            else session.Collect(kind);
            collected=true;
            if (GameBootstrap.Instance)
                GameBootstrap.Instance.Effects.Pickup(transform.position, kind == PickupKind.Coin ? new Color(1, 0.75f, 0.15f) : kind==PickupKind.Gem?new Color(.8f,.25f,1):Color.cyan);
            gameObject.SetActive(false);
        }
    }
}
