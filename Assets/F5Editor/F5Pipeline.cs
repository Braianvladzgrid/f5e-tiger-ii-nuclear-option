using System;
using System.IO;
using System.Linq;
using System.Text;
using Blueprinter;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class F5Pipeline {
 const string R="Assets/Blueprinter/Mods/F5";
 public const int RequiredTier=2;
 // Nuclear Option stores catalog prices in millions.
 public const float PriceMillions=14f;
 public static void ApplyCatalogSpecification(ScriptableObject definition, ScriptableObject parameters) {
  var ds=new SerializedObject(definition);
  ds.FindProperty("value").floatValue=PriceMillions;
  ds.ApplyModifiedPropertiesWithoutUndo();
  var ps=new SerializedObject(parameters);
  ps.FindProperty("rankRequired").intValue=RequiredTier;
  ps.ApplyModifiedPropertiesWithoutUndo();
 }
 public static Object Native(string name,string ext) {
 var path=AssetDatabase.GetAllAssetPaths().Single(p=>p.StartsWith("Assets/Blueprinter/_donotship/")&&p.EndsWith("/"+name+"_PLACEHOLDER"+ext));return AssetDatabase.LoadMainAssetAtPath(path);
 }
 public static Component Comp(GameObject root,string type)=>root.GetComponentsInChildren<Component>(true).First(c=>c&&c.GetType().Name==type);
 public static void Inspect() {
 var report=new StringBuilder();var root=(GameObject)Native("Fighter1",".prefab");
 foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
  if(t.GetComponents<Component>().Any(c=>c && new[]{"AeroPart","ControlSurface","LandingGear","Turbojet","WeaponManager","Aircraft","Hardpoint"}.Contains(c.GetType().Name))) {
   report.AppendLine("TRANSFORM "+t.name+" pos="+root.transform.InverseTransformPoint(t.position)+" rot="+t.localEulerAngles);
   foreach(var c in t.GetComponents<Component>().Where(c=>c&&!(c is Transform)&&!(c is Renderer)&&!(c is MeshFilter)&&!(c is Collider))) {
    report.AppendLine("COMPONENT "+c.GetType().Name);report.AppendLine(EditorJsonUtility.ToJson(c,true));
   }
  }
 }
 File.WriteAllText("../../work/f5/native-inspection.txt",report.ToString());
 Debug.Log("[F5] Native inspection complete");
 }
}
