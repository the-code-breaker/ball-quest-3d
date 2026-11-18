using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BallQuest.Tests
{
    public sealed class PresentationV7Tests
    {
        readonly List<Object> owned=new List<Object>();
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        [UnityTearDown]public IEnumerator Cleanup()
        {Time.timeScale=1;foreach(var value in owned)if(value)Object.Destroy(value);owned.Clear();yield return null;}

        [UnityTest]
        public IEnumerator AnimatedRewardFacesStayReadableWithoutMovingTriggersOrDuplicatingAssets()
        {
            var course=Keep(new GameObject("Reward resource owner"));
            var camera=Keep(new GameObject("Reward camera",typeof(Camera)));camera.tag="MainCamera";camera.transform.position=new Vector3(0,4,-8);camera.transform.LookAt(Vector3.zero);
            Transform first=null,firstVisual=null;Mesh shared=null;Material material=null;
            for(int i=0;i<80;i++)
            {
                var pickup=new GameObject("Coin trigger");pickup.transform.SetParent(course.transform);pickup.transform.position=new Vector3(i%8,1,i/8*2);
                pickup.AddComponent<SphereCollider>().isTrigger=true;
                var visual=PremiumCollectibleVisuals.Attach(pickup.transform,PickupKind.Coin,null);
                var filter=visual.GetComponentInChildren<MeshFilter>();var renderer=filter.GetComponent<Renderer>();
                if(i==0){first=pickup.transform;firstVisual=visual;shared=filter.sharedMesh;material=renderer.sharedMaterial;}
                else{Assert.AreSame(shared,filter.sharedMesh);Assert.AreSame(material,renderer.sharedMaterial);}
                Assert.AreEqual(2,visual.GetComponentsInChildren<Renderer>().Length);
                Assert.AreEqual(0,visual.GetComponentsInChildren<Collider>().Length,"Cosmetic coin parts must not create disabled collider components.");
            }
            var library=course.GetComponent<PremiumCollectibleVisuals>();Assert.AreEqual(80,library.RegisteredPickupCount);Assert.AreEqual(7,library.SharedMeshCount);
            Assert.AreEqual(80,course.GetComponentsInChildren<Collider>().Length);
            Vector3 anchor=first.position;Quaternion rotation=first.rotation;
            for(int i=0;i<30;i++)
            {
                camera.transform.position=new Vector3(Mathf.Sin(i*.1f)*6,4,-8);
                yield return null;
                Assert.AreEqual(anchor,first.position,"Only the visual bob may animate; broad-phase trigger positions must remain stable.");
                Assert.AreEqual(rotation,first.rotation);
                Vector3 toward=Vector3.ProjectOnPlane(camera.transform.position-first.position,Vector3.up).normalized;
                Assert.Greater(Mathf.Abs(Vector3.Dot(firstVisual.forward,toward)),.84f,"The embossed face must not spend frames edge-on to the chase camera.");
            }
            Assert.Greater(shared.bounds.size.z,.2f,"Coin must have a beveled physical silhouette rather than a flat card.");
            Object.Destroy(course);yield return null;yield return null;
            Assert.IsTrue(shared==null,"Course teardown must release shared generated meshes.");
            Assert.IsTrue(material==null,"Course teardown must release shared generated materials.");
        }

        [UnityTest]
        public IEnumerator AcceleratorTriggersRealBoostAndItsLightPulseResetsWithTheCourse()
        {
            var course=Keep(new GameObject("Accelerator resource owner"));
            var camera=Keep(new GameObject("Accelerator camera",typeof(Camera)));camera.tag="MainCamera";camera.transform.position=new Vector3(0,4,-8);
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(20,1,50);
            var go=new GameObject("Accelerator machinery");go.transform.SetParent(course.transform);go.transform.position=new Vector3(0,.1f,0);
            var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(5,1,4);
            var visual=PremiumBoosterVisuals.Attach(go.transform,new ObstacleSpec{kind=ObstacleKind.Booster,size=new Vector3(5,.2f,4),axis=Vector3.forward},null);
            go.AddComponent<Rigidbody>().isKinematic=true;var obstacle=go.AddComponent<QuestObstacle>();obstacle.kind=ObstacleKind.Booster;obstacle.axis=Vector3.forward;
            var ballGo=Keep(GameObject.CreatePrimitive(PrimitiveType.Sphere));ballGo.transform.position=new Vector3(0,.55f,-4);ballGo.AddComponent<Rigidbody>();ballGo.AddComponent<BallInput>();var ball=ballGo.AddComponent<BallController>();
            ball.ApplyProfile(Keep(ScriptableObject.CreateInstance<BallProfile>()));ball.active=true;ball.SetExternalControl(true);ball.SubmitControl(new BallControlCommand(1,0));
            float end=Time.time+3;while(!ball.Boosted&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball.Boosted);Assert.Greater(visual.Activation,.5f);
            Assert.AreEqual(3,go.GetComponentsInChildren<Renderer>().Length);Assert.AreEqual(1,go.GetComponentsInChildren<Collider>().Length);
            Assert.Greater(ball.Body.linearVelocity.z,1);Assert.Less(Mathf.Abs(ball.Body.linearVelocity.x),.05f);
            obstacle.ResetCourseState();Assert.AreEqual(0,visual.Activation);
        }

        [UnityTest]
        public IEnumerator RepeatedRewardAndBoostFeedbackUsesFixedCapacityAndExpires()
        {
            var root=Keep(new GameObject("Bounded feedback"));var effects=root.AddComponent<EffectPool>();
            for(int i=0;i<150;i++)
            {
                effects.Pickup(new Vector3(i*.01f,0,0),new Color(1,.7f,.1f));
                effects.Boost(new Vector3(i*.01f,0,0),Vector3.up);
            }
            Assert.AreEqual(12,effects.Capacity);Assert.AreEqual(12,effects.RingCapacity);
            Assert.AreEqual(12,root.GetComponentsInChildren<ParticleSystem>().Length);
            Assert.AreEqual(24,root.transform.childCount,"Feedback capacity must not grow with coin count.");
            yield return new WaitForSecondsRealtime(.9f);
            foreach(var particles in root.GetComponentsInChildren<ParticleSystem>())Assert.AreEqual(0,particles.particleCount);
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())Assert.IsFalse(renderer.enabled);
        }

        [Test]
        public void WarmedSharedRewardAndActiveBoosterAnimationAllocatesNoManagedBytesOnCallingThread()
        {
            var course=Keep(new GameObject("Isolated reward animation allocation measurement"));
            for(int i=0;i<80;i++)
            {
                var reward=new GameObject("Reward");reward.transform.SetParent(course.transform);
                reward.transform.position=new Vector3(i%8,1,i/8*2);
                PremiumCollectibleVisuals.Attach(reward.transform,i%3==0?PickupKind.Gem:i%3==1?PickupKind.Coin:PickupKind.Token,null);
                if(i%11==0)reward.SetActive(false);
            }
            var padRoot=new GameObject("Pulsing accelerator");padRoot.transform.SetParent(course.transform);
            var pad=PremiumBoosterVisuals.Attach(padRoot.transform,new ObstacleSpec{kind=ObstacleKind.Booster,size=new Vector3(5,.2f,4)},null);
            var library=course.GetComponent<PremiumCollectibleVisuals>();library.enabled=false;
            var cameraPositions=new[]{new Vector3(0,4,-8),new Vector3(0,4,-90),new Vector3(0,4,-205)};
            pad.Activate(null);
            for(int i=0;i<120;i++)library.Animate(i*.016f,.00001f,cameraPositions[i%3]);
            // Warm the allocation counter itself. The measured loop has no assertions,
            // delegates, reflection, frame yields, test-runner or rendering callbacks.
            System.GC.GetAllocatedBytesForCurrentThread();
            const int iterations=600;
            long before=System.GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<iterations;i++)library.Animate(2+i*.016f,.00001f,cameraPositions[i%3]);
            long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(0,allocated,"Warmed shared animation managed allocations on the current Editor test thread.");
            Assert.Greater(pad.Activation,.9f,"The measured pass must exercise active light property updates.");
            Assert.AreEqual(80,library.RegisteredPickupCount);Assert.AreEqual(1,library.RegisteredBoosterCount);
            Assert.AreEqual(7,library.SharedMeshCount);Assert.AreEqual(10,library.SharedMaterialCount);
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/reward-animation-allocation-v10.json",
                "{\n  \"environment\": \"Unity Editor isolated warmed animation calls\",\n  \"scope\": \"Managed bytes on calling thread only; excludes native/GPU work, initial construction, whole-frame and Android performance\",\n  \"rewardCount\": 80,\n  \"activeBoosterCount\": 1,\n  \"iterations\": "+iterations+",\n  \"managedBytes\": "+allocated+"\n}");
        }

        [Test]
        public void CoinFacetsHaveSlopedNormalsAndLensClearanceLeavesCollectionActive()
        {
            var course=Keep(new GameObject("Coin facet and lens-clearance review"));
            var reward=new GameObject("Coin trigger");reward.transform.SetParent(course.transform);
            var trigger=reward.AddComponent<SphereCollider>();trigger.isTrigger=true;
            var visual=PremiumCollectibleVisuals.Attach(reward.transform,PickupKind.Coin,null);
            var library=course.GetComponent<PremiumCollectibleVisuals>();library.enabled=false;
            var meshes=visual.GetComponentsInChildren<MeshFilter>();
            var vertices=meshes[1].sharedMesh.vertices;var normals=meshes[1].sharedMesh.normals;
            int slopedCrownVertices=0;
            for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(vertices[i].z)>.19f&&Mathf.Abs(normals[i].z)<.98f&&Mathf.Abs(normals[i].z)>.5f)slopedCrownVertices++;
            Assert.GreaterOrEqual(slopedCrownVertices,20,"Both embossed faces need true sloped highlight facets.");
            library.Animate(0,.016f,new Vector3(0,0,-5));
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())Assert.IsFalse(renderer.enabled);
            Assert.IsTrue(reward.activeInHierarchy&&trigger.enabled&&trigger.isTrigger);
            Assert.AreEqual(Vector3.zero,reward.transform.position);Assert.AreEqual(Vector3.one,reward.transform.localScale);
            library.Animate(1,.016f,new Vector3(0,0,-12));
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())Assert.IsTrue(renderer.enabled);
            Assert.AreEqual(Vector3.one,visual.localScale);
        }
    }
}
