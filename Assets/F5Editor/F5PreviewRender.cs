using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class F5PreviewRender {
 public static void Capture() {
  var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(F5AircraftBuilder.R+"/F5.prefab"));
  var camera=new GameObject("F5PreviewCamera").AddComponent<Camera>();
  var sun=new GameObject("F5PreviewSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(40,-35,0);
  camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.17f,.21f);camera.nearClipPlane=.025f;camera.farClipPlane=100;camera.fieldOfView=55;
  UnityEngine.RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;UnityEngine.RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
  try {
   foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true))particle.gameObject.SetActive(false);
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.ToLowerInvariant().Contains("afterburn")))renderer.enabled=false;
   var aircraft=new SerializedObject(F5Pipeline.Comp(root,"Aircraft"));
   var cockpit=aircraft.FindProperty("cockpitRenderers");
   for(int i=0;i<cockpit.arraySize;i++){var renderer=cockpit.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;if(renderer)renderer.enabled=false;}
   camera.cullingMask=~0;camera.transform.position=new Vector3(-12,7,14);camera.transform.LookAt(new Vector3(0,.2f,1));Save(camera,"exterior");camera.transform.position=new Vector3(-2,1.4f,-8);camera.transform.LookAt(new Vector3(0,.2f,-4.6f));Save(camera,"exhaust-closeup");
   File.WriteAllLines("../../work/f5/contact-bounds.txt",root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).OrderBy(c=>c.bounds.min.y).Select(c=>c.name+" "+c.GetType().Name+" min="+c.bounds.min.ToString("F3")+" max="+c.bounds.max.ToString("F3")));
   for(int i=0;i<cockpit.arraySize;i++){var renderer=cockpit.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;if(renderer)renderer.enabled=true;}
   var eye=aircraft.FindProperty("cockpitViewPoint").objectReferenceValue as Transform;camera.transform.position=eye.position;camera.transform.rotation=root.transform.rotation;camera.cullingMask=~0;Save(camera,"cockpit");
  } finally {Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(sun.gameObject);}
 }
 static void Save(Camera camera,string name) {
  Directory.CreateDirectory("../../work/f5/previews");var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
  var texture=new Texture2D(1280,800,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes("../../work/f5/previews/"+name+".png",texture.EncodeToPNG());
  RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);
 }
}

