using UnityEngine;

namespace BallQuest
{
    /// <summary>An explicit bounded gravity field. Only acceleration changes; poses and velocities remain physical.</summary>
    [DefaultExecutionOrder(-120)]
    public sealed class SignatureGravityField : MonoBehaviour
    {
        public Vector3 curveCenter=new Vector3(0,30,24);
        public float strength=9.81f;
        public int WallContacts { get; private set; }
        public int CeilingContacts { get; private set; }
        BallController[] balls;
        void Start()=>balls=GetComponentsInParent<LevelBuilder>().Length>0?GetComponentInParent<LevelBuilder>().GetComponentsInChildren<BallController>():FindObjectsByType<BallController>(FindObjectsSortMode.None);
        void FixedUpdate()
        {
            if(balls==null)return;
            foreach(var ball in balls)
            {
                if(!ball)continue;Vector3 p=ball.Body.position;
                bool inside=Mathf.Abs(p.x)<7&&p.y>16&&p.y<44&&p.z>-14&&p.z<38;
                if(!inside){ball.ResetGravity();continue;}
                if(p.z>=23.2f)
                {
                    Vector3 radial=new Vector3(0,p.y-curveCenter.y,p.z-curveCenter.z);
                    ball.SetGravityDirection(radial.sqrMagnitude>.1f?radial.normalized*strength:Physics.gravity);
                    if(ball.Grounded&&Vector3.Dot(ball.MovementUp,Vector3.back)>.8f)WallContacts++;
                }
                else ball.SetGravityDirection(p.y>35?Vector3.up*strength:Vector3.down*strength);
                if(ball.Grounded&&Vector3.Dot(ball.MovementUp,Vector3.down)>.8f)CeilingContacts++;
            }
        }
        void OnDisable(){if(balls!=null)foreach(var ball in balls)if(ball)ball.ResetGravity();}
    }

}
