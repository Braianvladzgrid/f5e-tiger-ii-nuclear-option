using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Blueprinter;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class F5AircraftBuilder {
 public const string R="Assets/Blueprinter/Mods/F5";
 public const string Key="F5E_TigerII";
 public static readonly Vector3 Offset=new Vector3(0,0,1.25f);
 static Component C(GameObject r,string n)=>F5Pipeline.Comp(r,n);
 static Transform T(GameObject r,string n)=>r.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 static Transform Ref(SerializedObject s,string n) {var o=s.FindProperty(n).objectReferenceValue;return o is GameObject g?g.transform:o as Transform;}
 static void Set(SerializedObject so,string n,string v){so.FindProperty(n).stringValue=v;}
 static void Set(SerializedObject so,string n,float v){so.FindProperty(n).floatValue=v;}
 static void Renderers(SerializedObject so,string field,IEnumerable<Renderer> rs){var p=so.FindProperty(field);var a=rs.Where(r=>r).Distinct().ToArray();p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];}
 static Bounds BoundsOf(MeshFilter mf){var b=mf.sharedMesh.bounds;var result=new Bounds(mf.transform.TransformPoint(b.center),Vector3.zero);foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})result.Encapsulate(mf.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return result;}
 static bool Interior(string n)=>n=="Panel"||n=="Panel_0"||n=="Panel_1"||n=="Cockpit"||n=="Dash"||n=="seat"||n=="CPT-Seat"||n.Contains("cushion")||n.StartsWith("disk")||n.StartsWith("block")||n.StartsWith("rect")||n.StartsWith("poly")||n.StartsWith("object")||n.StartsWith("cylinder")||n.Contains("handle")||n.Contains("oxy")||n.Contains("safety");
 static bool Gear(string n)=>n.Contains("Gear")||n.Contains("Tire")||n.Contains("doorgear");

 [MenuItem("F5/Rebuild and Compile")]
 public static void Build() {
  AssetDatabase.Refresh();
  foreach(var f in new[]{"F5.prefab","F5Definition.asset","F5Parameters.asset","F5Hangars.asset"})if(AssetDatabase.LoadMainAssetAtPath(R+"/"+f))AssetDatabase.DeleteAsset(R+"/"+f);
  var def=Object.Instantiate((ScriptableObject)F5Pipeline.Native("FS-12",".asset"));def.name=Key;
  var parameters=Object.Instantiate((ScriptableObject)F5Pipeline.Native("Fighter1Parameters",".asset"));parameters.name=Key+"Parameters";
  F5Pipeline.ApplyCatalogSpecification(def,parameters);
  var ds=new SerializedObject(def);Set(ds,"jsonKey",Key);Set(ds,"unitName","F-5E Tiger II");Set(ds,"code","F-5E");
  Set(ds,"description","Light fighter adaptation. Tier 2, $14 million. Native flight, weapons and HUD systems; experimental flight tuning.");
  Set(ds,"length",14.45f);Set(ds,"width",8.13f);Set(ds,"height",4.08f);Set(ds,"radarSize",.7f);
  ds.FindProperty("spawnOffset").vector3Value=new Vector3(0,1.55f,-1.25f);ds.FindProperty("aircraftParameters").objectReferenceValue=parameters;ds.ApplyModifiedPropertiesWithoutUndo();
  var ps=new SerializedObject(parameters);Set(ps,"aircraftName","F-5E Tiger II");ps.FindProperty("liveries").arraySize=0;ps.ApplyModifiedPropertiesWithoutUndo();
  AssetDatabase.CreateAsset(def,R+"/F5Definition.asset");AssetDatabase.CreateAsset(parameters,R+"/F5Parameters.asset");
  ConfigureLivery(parameters);
  var root=Object.Instantiate((GameObject)F5Pipeline.Native("Fighter1",".prefab"));root.name=Key;
  try {
   foreach(var lod in root.GetComponentsInChildren<LODGroup>(true))lod.enabled=false;
   var aircraft=new SerializedObject(C(root,"Aircraft"));var cp=aircraft.FindProperty("cockpitRenderers");
   var originalCockpit=Enumerable.Range(0,cp.arraySize).Select(i=>cp.GetArrayElementAtIndex(i).objectReferenceValue as Renderer).Where(r=>r).ToArray();
   var cockpitMeshes=new HashSet<Renderer>(originalCockpit);
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
    var renderer=mf.GetComponent<Renderer>();
    // Keep native cockpit displays and jet effects. All donor exterior geometry is replaced.
    string n=mf.name.ToLowerInvariant();
    bool display=cockpitMeshes.Contains(renderer)&&(n.Contains("screen")||n.Contains("display"));
    if(!display&&!n.Contains("afterburn"))mf.sharedMesh=null;
   }
   foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.sharedMesh=null;
   var importer=(ModelImporter)AssetImporter.GetAtPath(R+"/Models/f5emin.obj");
   if(importer.globalScale!=1 || !importer.isReadable){importer.globalScale=1;importer.isReadable=true;importer.SaveAndReimport();}
   var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Models/f5emin.obj"),root.transform);model.name="F5Exterior";model.transform.localPosition=Offset;model.transform.localScale=new Vector3(-.973f,.973f,.973f);
   var own=model.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh).ToArray();
   var cockpit=new GameObject("F5Cockpit");cockpit.transform.SetParent(root.transform,false);
   var nativeCockpit=T(root,"cockpit_interior");
   foreach(var mf in own) {
    if(mf.name=="Pilot"||mf.name.StartsWith("LR-arm")||mf.name=="tailhook"||mf.name.StartsWith("revoprobe")){mf.gameObject.SetActive(false);continue;}
    if(Interior(mf.name)){mf.transform.SetParent(cockpit.transform,true);mf.gameObject.layer=nativeCockpit.gameObject.layer;}
    ConvertMaterials(mf);
   }
   // Keep functional native instruments; move the camera and displays together to the F-5 eye station.
   var eye=Ref(aircraft,"cockpitViewPoint");var eyeTarget=root.transform.TransformPoint(new Vector3(0,.98f,3.92f));
   var eyeDelta=eyeTarget-eye.position;eye.position=eyeTarget;nativeCockpit.position+=eyeDelta;
   aircraft.FindProperty("definition").objectReferenceValue=def;
   Renderers(aircraft,"exteriorRenderers",own.Where(m=>!Interior(m.name)).Select(m=>m.GetComponent<Renderer>()));
   Renderers(aircraft,"cockpitRenderers",originalCockpit.Concat(cockpit.GetComponentsInChildren<Renderer>(true)));aircraft.ApplyModifiedPropertiesWithoutUndo();
   foreach(var name in new[]{"Windscreen","WindscreenFrame","CanopyGlass","CanopyFrame"}) {
    var source=own.FirstOrDefault(m=>m.name==name);if(!source)continue;var copy=Object.Instantiate(source.gameObject,cockpit.transform);copy.name=name+"_Interior";copy.transform.position=source.transform.position;copy.transform.rotation=source.transform.rotation;copy.layer=nativeCockpit.gameObject.layer;
   }
   aircraft.Update();Renderers(aircraft,"cockpitRenderers",originalCockpit.Concat(cockpit.GetComponentsInChildren<Renderer>(true)));aircraft.ApplyModifiedPropertiesWithoutUndo();
   FitGear(root,own);FitSurfaces(root,own);FitHull(root,own);ConfigureEngine(root,own);ConfigureWeapons(root,parameters);
   foreach(var part in root.GetComponentsInChildren<Component>(true).Where(c=>c&&c.GetType().Name=="AeroPart")) {
    var so=new SerializedObject(part);var mass=so.FindProperty("mass");if(mass!=null)mass.floatValue*=.72f;so.ApplyModifiedPropertiesWithoutUndo();
   }
   // Blueprinter resolves components by hierarchy path; sibling names must be unique.
   foreach(var t in root.GetComponentsInChildren<Transform>(true))foreach(var group in Enumerable.Range(0,t.childCount).Select(t.GetChild).GroupBy(c=>c.name).Where(g=>g.Count()>1)) {int k=0;foreach(var child in group)if(k++>0)child.name+="_F5_"+k;}
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,R+"/F5.prefab");ds.Update();ds.FindProperty("unitPrefab").objectReferenceValue=prefab;ds.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(def);
  } finally {Object.DestroyImmediate(root);}
  AddHangars();F5GearRepair.Apply();CreateIcon();AssetDatabase.SaveAssets();Validate();
  ModBuilder.Build("F5",Key,"0.1.0",Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds"));
  Debug.Log("[F5] COMPILED tier=2 price=14000000; runtime flight test still required");
 }
 static void ConvertMaterials(MeshFilter mf) {
  var mr=mf.GetComponent<MeshRenderer>();if(!mr)return;
  var materials=mr.sharedMaterials;
  for(int i=0;i<materials.Length;i++) {
   var source=materials[i];if(!source)continue;var path=R+"/F5_"+source.name.Replace('/','_')+(mf.name.Contains("Glass")||mf.name=="Windscreen"?"_glass":"")+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   var color=source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;
   mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.25f);if(source.mainTexture)mat.SetTexture("_BaseMap",source.mainTexture);
   if(color.a<.95f||mf.name.Contains("Glass")||mf.name=="Windscreen") {
    color.a=Mathf.Min(color.a,.08f);mat.SetColor("_BaseColor",color);mat.SetFloat("_Surface",1);mat.SetFloat("_ZWrite",0);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_Cull",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;mat.SetShaderPassEnabled("ShadowCaster",false);
   }
   materials[i]=mat;EditorUtility.SetDirty(mat);
  }mr.sharedMaterials=materials;
 }
 static void FitGear(GameObject root,MeshFilter[] own) {
  string[] model={"NGearTire","LMainTire","RMainTire"},names={"gear_F","gear_L_sprung","gear_R_sprung"};
  for(int i=0;i<3;i++) {
   var mesh=own.First(m=>m.name==model[i]);var b=BoundsOf(mesh);var so=new SerializedObject(C(T(root,names[i]).gameObject,"LandingGear"));
   var wheel=so.FindProperty("wheels").GetArrayElementAtIndex(0).objectReferenceValue as Transform;var delta=b.center-wheel.position;
   // Move each native gear mount as one assembly, preserving its local hinge and suspension relationships.
   var hinge=Ref(so,"gearHinge");var assembly=i==0?hinge:hinge.parent;assembly.position+=delta;
   float radius=Mathf.Max(.13f,b.extents.y);Set(so,"wheelRadius",radius);Set(so,"rollingResistance",.01f);
   var unsprung=Ref(so,"unsprung");var bump=Ref(so,"bumpStop");float travel=so.FindProperty("suspensionTravel").floatValue;
   unsprung.position=b.center;bump.position=b.center+unsprung.up*(travel-radius);if(Ref(so,"castPoint"))Ref(so,"castPoint").position=bump.position;wheel.position=b.center;
   so.ApplyModifiedPropertiesWithoutUndo();
  }
 }
 static void FitSurfaces(GameObject root,MeshFilter[] own) {
  var map=new Dictionary<string,string>{{"aileron_L","LAileron"},{"aileron_R","RAileron"},{"flap_L","LBFlap"},{"flap_R","RBFlap"},{"canard_L","LHStab"},{"canard_R","RHStab"},{"rudder","Rudder"}};
  foreach(var entry in map) {
   var target=T(root,entry.Key);var mesh=own.First(m=>m.name==entry.Value);
   Transform visual;
   if(entry.Key.StartsWith("flap")) {
    var highLift=new SerializedObject(C(target.gameObject,"HighLiftDevice"));
    visual=highLift.FindProperty("movingParts").GetArrayElementAtIndex(0).FindPropertyRelative("transform").objectReferenceValue as Transform;
   } else {var so=new SerializedObject(C(target.gameObject,"ControlSurface"));visual=Ref(so,"visibleMesh");
    // The donor's forward canards are now aft stabilators: reverse pitch deflection.
    if(entry.Key.StartsWith("canard")){so.FindProperty("pitchRange").floatValue=-25;so.ApplyModifiedPropertiesWithoutUndo();}
   }
   var b=BoundsOf(mesh);target.position=b.center;
   visual.position=new Vector3(b.center.x,b.center.y,b.max.z);mesh.transform.SetParent(visual,true);
  }
 }
 static void FitHull(GameObject root,MeshFilter[] own) {
  Directory.CreateDirectory(R+"/Collision");var done=new HashSet<GameObject>();
  foreach(var collider in root.GetComponentsInChildren<MeshCollider>(true)) {
   if(!done.Add(collider.gameObject))continue;string n=collider.name;bool left=n.EndsWith("_L");string source=null;
   if(n.StartsWith("wing"))source=left?"LWing":"RWing";
   if(n.StartsWith("aileron"))source=left?"LAileron":"RAileron";
   if(n.StartsWith("flap"))source=left?"LBFlap":"RBFlap";
   if(n.StartsWith("canard"))source=left?"LHStab":"RHStab";
   if(n=="rudder")source="Rudder";
   if(n.StartsWith("vstab"))source="VTail";
   if(n.StartsWith("intake"))source=left?"LInlet":"RInlet";
   bool body=n=="fuselage"||n=="tail"||n=="cockpit"||n=="nose"||n.StartsWith("weaponbay")||collider.transform==root.transform;
   if(body)source="fuselage";
   if(source==null){continue;}
   var b=BoundsOf(own.First(m=>m.name==source));
   if(body){var old=collider.bounds;var min=b.min;var max=b.max;min.z=Mathf.Clamp(old.min.z,b.min.z,b.max.z-.05f);max.z=Mathf.Clamp(old.max.z,min.z+.05f,b.max.z);b.SetMinMax(min,max);}
   if(n.StartsWith("vstab")){var min=b.min;var max=b.max;if(n=="vstab1")max.y=b.center.y;else min.y=b.center.y;b.SetMinMax(min,max);}
   if(n.StartsWith("wing")){var min=b.min;var max=b.max;float a=n.StartsWith("wingtip")?3.3f:n.StartsWith("wing2")?2.1f:0.7f;float end=n.StartsWith("wingtip")?4.1f:n.StartsWith("wing2")?3.3f:2.1f;min.x=left?-end:a;max.x=left?-a:end;if(n.StartsWith("wing1b"))max.z=b.center.z;else if(n.StartsWith("wing1a"))min.z=b.center.z;b.SetMinMax(min,max);}
   var local=new Bounds(collider.transform.InverseTransformPoint(b.center),Vector3.zero);foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})local.Encapsulate(collider.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
   string path=R+"/Collision/"+n+".asset";var hull=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!hull){hull=new Mesh();AssetDatabase.CreateAsset(hull,path);}hull.Clear();var c=local.center;var e=Vector3.Max(local.extents,Vector3.one*.025f);
   hull.vertices=new[]{c+new Vector3(-e.x,-e.y,-e.z),c+new Vector3(e.x,-e.y,-e.z),c+new Vector3(e.x,e.y,-e.z),c+new Vector3(-e.x,e.y,-e.z),c+new Vector3(-e.x,-e.y,e.z),c+new Vector3(e.x,-e.y,e.z),c+new Vector3(e.x,e.y,e.z),c+new Vector3(-e.x,e.y,e.z)};
   hull.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};hull.RecalculateNormals();hull.RecalculateBounds();EditorUtility.SetDirty(hull);collider.sharedMesh=hull;
  }
 }
 static void ConfigureEngine(GameObject root,MeshFilter[] own) {
  var jet=new SerializedObject(C(root,"Turbojet"));Set(jet,"maxThrust",44000);jet.FindProperty("thrustVectoring").vector3Value=Vector3.zero;jet.FindProperty("thrustVectoringGain").vector3Value=Vector3.zero;jet.ApplyModifiedPropertiesWithoutUndo();
  // One native engine controller represents the pair for the initial integration; two exhaust effects.
  var nozzle=C(root,"JetNozzle");var clone=Object.Instantiate(nozzle.gameObject,nozzle.transform.parent);clone.name="F5Nozzle_R";nozzle.name="F5Nozzle_L";
  nozzle.transform.position=root.transform.TransformPoint(new Vector3(-.32f,.02f,-4.9f));clone.transform.position=root.transform.TransformPoint(new Vector3(.32f,.02f,-4.9f));
  foreach(var n in new[]{nozzle.gameObject,clone})foreach(var tr in n.GetComponentsInChildren<Transform>(true).Where(t=>t.name.IndexOf("afterburn",StringComparison.OrdinalIgnoreCase)>=0))tr.localScale*=.45f;
  jet.Update();var nozzles=jet.FindProperty("nozzles");nozzles.arraySize=2;nozzles.GetArrayElementAtIndex(0).objectReferenceValue=nozzle;nozzles.GetArrayElementAtIndex(1).objectReferenceValue=C(clone,"JetNozzle");jet.ApplyModifiedPropertiesWithoutUndo();
 }
 static void ConfigureWeapons(GameObject root,ScriptableObject parameters) {
  var wm=new SerializedObject(C(root,"WeaponManager"));var sets=wm.FindProperty("hardpointSets");
  for(int s=0;s<sets.arraySize;s++) {
   var set=sets.GetArrayElementAtIndex(s);var points=set.FindPropertyRelative("hardpoints");set.FindPropertyRelative("name").stringValue=s==0?"Internal Cannon":"F-5 External Stations "+s;
   for(int p=0;p<points.arraySize;p++) {
    var point=points.GetArrayElementAtIndex(p);var mount=point.FindPropertyRelative("transform").objectReferenceValue as Transform;
    if(s>0 && mount){float x=(p%2==0?-1:1)*(s==1?1.3f:s==2?2.45f:4f);mount.position=root.transform.TransformPoint(new Vector3(x,-.2f,s==1?.5f:s==2?.2f:-.7f));}
    point.FindPropertyRelative("bayDoors").arraySize=0;
   }
  }wm.ApplyModifiedPropertiesWithoutUndo();
 }
 static void AddHangars() {
  OpReferenceIndex.Refresh();var op=ScriptableObject.CreateInstance<OpAddAircraftToHangars>();op.aircraftJsonKey=Key;var donor=F5Pipeline.Native("FS-12",".asset");
  foreach(var guid in AssetDatabase.FindAssets("t:BuildingDefinition",new[]{"Assets/Blueprinter/_donotship"})) {
   var d=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));var so=new SerializedObject(d);var prefab=so.FindProperty("unitPrefab").objectReferenceValue as GameObject;if(!prefab)continue;var names=new List<string>();
   foreach(var h in prefab.GetComponentsInChildren<Component>(true).Where(c=>c&&c.GetType().Name=="Hangar")){var list=new SerializedObject(h).FindProperty("availableAircraft");if(Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==donor))names.Add(h.name.Replace("_PLACEHOLDER",""));}
   if(names.Count>0)op.hangars.Add(new OpAddAircraftToHangars.HangarTarget{hangarUnitJsonKey=so.FindProperty("jsonKey").stringValue,hangarNames=names});
  }if(op.hangars.Count==0)throw new Exception("No compatible hangars");AssetDatabase.CreateAsset(op,R+"/F5Hangars.asset");
 }
 static void ConfigureLivery(ScriptableObject parameters) {
  string path=R+"/F5Silver.asset";var livery=AssetDatabase.LoadMainAssetAtPath(path);
  if(!livery){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LiveryData")).First(t=>t!=null);livery=ScriptableObject.CreateInstance(type);AssetDatabase.CreateAsset(livery,path);}
  var ls=new SerializedObject(livery);ls.FindProperty("Texture").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(R+"/Models/fuselage.png");ls.FindProperty("Glossiness").floatValue=.25f;ls.FindProperty("Colors").arraySize=0;ls.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(livery);
  var donor=new SerializedObject(F5Pipeline.Native("Fighter1Parameters",".asset"));var ps=new SerializedObject(parameters);var entries=ps.FindProperty("liveries");entries.arraySize=2;
  for(int i=0;i<2;i++){var entry=entries.GetArrayElementAtIndex(i);entry.FindPropertyRelative("name").stringValue="F-5E Silver";entry.FindPropertyRelative("faction").objectReferenceValue=donor.FindProperty("liveries").GetArrayElementAtIndex(i).FindPropertyRelative("faction").objectReferenceValue;entry.FindPropertyRelative("assetReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(path);}
  ps.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(parameters);
 }
 static void CreateIcon() {
  // Render the aircraft's own mesh into a silhouette; no unrelated image assets.
  var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Models/f5emin.obj"));model.transform.position=new Vector3(0,1000,0);
  var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",Color.white);
  foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();if(Gear(r.name)||Interior(r.name)||r.name=="Pilot")r.enabled=false;}
  var camera=new GameObject("F5IconCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,1030,0);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.farClipPlane=50;
  var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(256,256,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();File.WriteAllBytes(R+"/F5Icon.png",tex.EncodeToPNG());RenderTexture.active=old;
  Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(model);Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(mat);
  AssetDatabase.ImportAsset(R+"/F5Icon.png");var importer=(TextureImporter)AssetImporter.GetAtPath(R+"/F5Icon.png");importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
  var ds=new SerializedObject(AssetDatabase.LoadMainAssetAtPath(R+"/F5Definition.asset"));ds.FindProperty("mapIcon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(R+"/F5Icon.png");ds.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(ds.targetObject);
 }
 public static void Validate() {
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/F5.prefab");var ds=new SerializedObject(AssetDatabase.LoadMainAssetAtPath(R+"/F5Definition.asset"));var ps=new SerializedObject(AssetDatabase.LoadMainAssetAtPath(R+"/F5Parameters.asset"));
  if(ds.FindProperty("value").floatValue!=14||ps.FindProperty("rankRequired").intValue!=2)throw new Exception("Invalid tier/price");
  if(root.GetComponentsInChildren<Component>(true).Any(c=>!c))throw new Exception("Missing native script");
  if(root.GetComponentsInChildren<Component>(true).Count(c=>c&&c.GetType().Name=="LandingGear")!=3)throw new Exception("Expected 3 gears");
  if(!ds.FindProperty("mapIcon").objectReferenceValue)throw new Exception("Own icon missing");
  if(ps.FindProperty("liveries").arraySize<2)throw new Exception("Both factions require a selectable livery");
  var report="Tier: 2\nPrice: 14000000\nNative landing gears: 3\nOwn icon: assigned\nNative systems: retained\nIn-game flight: NOT TESTED\n";File.WriteAllText("../../work/f5/build-validation.txt",report);
 }
 public static void BuildAndPreview(){Build();F5PreviewRender.Capture();}
}
