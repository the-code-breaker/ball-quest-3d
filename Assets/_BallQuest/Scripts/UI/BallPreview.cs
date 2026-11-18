using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BallQuest
{
    // A real lit, rotating 3D cosmetic render, shared by home and collection screens.
    [RequireComponent(typeof(RectTransform))]
    public sealed class BallPreview : MonoBehaviour
    {
        public bool podium=true;
        public float rotationSpeed=18;
        public int resolution=512;
        static int sequence;
        static readonly Dictionary<string,Texture2D> thumbnails=new Dictionary<string,Texture2D>();
        GameObject stage;
        GameObject ball;
        Camera camera;
        RenderTexture target;
        RawImage image;
        Material podiumMaterial;
        GameObject pedestal;
        bool ready;
        float nextRender;

        public void Show(BallSkin skin)
        {
            if(!skin)return;
            if(!ready)CreateStage();
            var catalog=GameBootstrap.Instance?GameBootstrap.Instance.catalog:null;
            BallLook.Apply(ball,skin,catalog);
            ball.transform.localRotation=Quaternion.Euler(8,192,-8);
            pedestal.SetActive(podium);
            RenderNow();
        }

        void CreateStage()
        {
            ready=true;
            image=GetComponent<RawImage>();if(!image)image=gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            var source=new Vector3(10000+(sequence++%32)*8,10000,10000);
            stage=new GameObject("Isolated 3D ball portrait");stage.transform.position=source;
            ball=new GameObject("Live ball model",typeof(MeshFilter),typeof(MeshRenderer));ball.transform.SetParent(stage.transform,false);ball.layer=10;
            ball.transform.localPosition=new Vector3(0,.1f,0);
            pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pedestal.name="Presentation plinth";pedestal.transform.SetParent(stage.transform,false);
            pedestal.transform.localPosition=new Vector3(0,-.48f,0);pedestal.transform.localScale=new Vector3(1.5f,.055f,1.05f);pedestal.layer=10;
            Destroy(pedestal.GetComponent<Collider>());
            var catalog=GameBootstrap.Instance?GameBootstrap.Instance.catalog:null;
            podiumMaterial=catalog?new Material(catalog.palette[7]):QuestVisuals.Material("Portrait plinth",new Color(.21f,.48f,.72f));
            podiumMaterial.SetColor("_BaseColor",new Color(.19f,.43f,.64f));podiumMaterial.SetFloat("_Metallic",.6f);podiumMaterial.SetFloat("_Smoothness",.75f);
            pedestal.GetComponent<Renderer>().sharedMaterial=podiumMaterial;
            var go=new GameObject("Ball portrait camera");go.transform.SetParent(stage.transform,false);camera=go.AddComponent<Camera>();
            go.transform.localPosition=new Vector3(0,.46f,-2.15f);go.transform.LookAt(source+new Vector3(0,.04f,0));
            camera.enabled=false;camera.cullingMask=1<<10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.orthographic=true;camera.orthographicSize=.8f;camera.nearClipPlane=.1f;camera.farClipPlane=5;camera.allowHDR=false;camera.allowMSAA=true;
            var extra=go.AddComponent<UniversalAdditionalCameraData>();extra.renderShadows=false;extra.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            target=new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGB32){name="Live glossy ball portrait",antiAliasing=2};target.Create();image.texture=target;
            AddLight("Portrait softbox",new Vector3(-1.2f,1.6f,-1.4f),new Color(.78f,.9f,1),8);
            AddLight("Portrait warm rim",new Vector3(1.1f,.4f,.2f),new Color(1,.82f,.62f),5);
        }

        void AddLight(string name,Vector3 point,Color color,float intensity)
        {
            var go=new GameObject(name);go.transform.SetParent(stage.transform,false);go.transform.localPosition=point;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=4;light.intensity=intensity;light.color=color;light.cullingMask=1<<10;light.shadows=LightShadows.None;
        }

        void LateUpdate()
        {
            if(!ready)return;
            ball.transform.Rotate(Vector3.up,rotationSpeed*Time.unscaledDeltaTime,Space.Self);
            if(Time.unscaledTime<nextRender)return;nextRender=Time.unscaledTime+1f/30;RenderNow();
        }
        void RenderNow()
        {
            if(!camera||!target)return;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        }

        public static Texture2D GetThumbnail(BallSkin skin)
        {
            if(!skin)return Texture2D.whiteTexture;
            if(thumbnails.TryGetValue(skin.id,out var cached)&&cached)return cached;
            var go=new GameObject("Temporary ball thumbnail",typeof(RectTransform));var preview=go.AddComponent<BallPreview>();preview.resolution=160;preview.podium=false;preview.Show(skin);
            var previous=RenderTexture.active;RenderTexture.active=preview.target;
            var texture=new Texture2D(160,160,TextureFormat.RGBA32,false){name=skin.displayName+" rendered thumbnail",filterMode=FilterMode.Bilinear};
            texture.ReadPixels(new Rect(0,0,160,160),0,0);texture.Apply();RenderTexture.active=previous;
            preview.Release();Destroy(go);thumbnails[skin.id]=texture;return texture;
        }

        void Release()
        {
            ready=false;
            if(stage){stage.SetActive(false);Destroy(stage);stage=null;}
            if(target){target.Release();Destroy(target);target=null;}
            if(podiumMaterial)Destroy(podiumMaterial);
        }
        void OnDestroy()=>Release();
    }
}
