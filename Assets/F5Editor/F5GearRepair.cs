using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;
public static class F5GearRepair {
 const string R="Assets/Blueprinter/Mods/F5";
 static float N(XElement e,string key)=>float.Parse(e.Element(key).Value,CultureInfo.InvariantCulture);
 static Vector3 V(float x,float y,float z)=>new Vector3(-y,z,-x)*.973f;
 public static void Apply(){
  var root=PrefabUtility.LoadPrefabContents(R+"/F5.prefab");
  try {
   if(root.transform.Find("F5GearVisuals"))throw new Exception("Rebuild before applying source gear animations again");
   var visual=new GameObject("F5GearVisuals").transform;visual.SetParent(root.transform,false);
   var meshes=root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh&&AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith(R+"/Models/",StringComparison.Ordinal)).ToArray();
   var xml=XDocument.Load("Assets/F5Editor/F5SourceAnimations.xml");
   var animations=xml.Root.Elements("animation").Where(a=>a.Element("type")?.Value=="rotate"&&a.Element("property")?.Value.StartsWith("gear/gear[")==true).ToArray();
   var report=new StringBuilder();int count=0;
   foreach(var mesh in meshes) {
    var tracks=animations.Where(a=>a.Elements("object-name").Any(n=>n.Value==mesh.name)).ToArray();
    if(tracks.Length==0)continue;
    mesh.gameObject.SetActive(true);mesh.transform.SetParent(visual,true);
    Transform parent=visual;
    foreach(var a in tracks){
     var axis=a.Element("axis");Vector3 center,direction;
     if(axis.Element("x1-m")!=null){center=V(N(axis,"x1-m"),N(axis,"y1-m"),N(axis,"z1-m"));direction=V(N(axis,"x2-m"),N(axis,"y2-m"),N(axis,"z2-m"))-center;}
     else {var c=a.Element("center");center=V(N(c,"x-m"),N(c,"y-m"),N(c,"z-m"));direction=V(N(axis,"x"),N(axis,"y"),N(axis,"z"));}
     // Reflection into Unity's aircraft handedness reverses rotation axes.
     // The final source-to-Unity transform preserves handedness.
     bool compression=a.Element("property").Value.Contains("compression-norm");
     var curve=compression?"0,0;1,"+a.Element("factor").Value:string.Join(";",a.Element("interpolation").Elements("entry").Select(e=>e.Element("ind").Value+","+e.Element("dep").Value));
     string side=mesh.name.StartsWith("NGear")||mesh.name=="nose-well-cover"?"F":mesh.name.StartsWith("L")?"L":"R";
     // Correct mirrored pivot typos in the original source's wheel-fold links.
     if(side=="L"&&center.x>0||side=="R"&&center.x<0)center.x=-center.x;
     var pivot=new GameObject((compression?"F5GearCompression|":"F5GearCurve|")+side+"|"+curve+"|"+mesh.name+"_"+count).transform;
     pivot.SetParent(parent,false);pivot.position=root.transform.TransformPoint(center+F5AircraftBuilder.Offset);
     pivot.rotation=root.transform.rotation*Quaternion.FromToRotation(Vector3.right,direction.normalized);
     mesh.transform.SetParent(pivot,true);parent=pivot;count++;
    }
    if(!mesh.name.ToLowerInvariant().Contains("door")&&!mesh.name.Contains("cover")) {
     string side=mesh.name.StartsWith("NGear")?"F":mesh.name.StartsWith("L")?"L":"R";
     var tag=new GameObject("F5GearVisible|"+side).transform;
     tag.SetParent(parent,false);mesh.transform.SetParent(tag,true);
     if(mesh.name=="NGearTire"||mesh.name=="LMainTire"||mesh.name=="RMainTire"){
      var spin=new GameObject("F5GearWheel|"+side).transform;spin.SetParent(tag,false);
      spin.position=mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center);
      spin.rotation=root.transform.rotation;mesh.transform.SetParent(spin,true);
     }
    }
    report.AppendLine(mesh.name+" source tracks="+tracks.Length);
   }
   if(count<8)throw new Exception("Missing source gear animation tracks");
   PrefabUtility.SaveAsPrefabAsset(root,R+"/F5.prefab");
   File.WriteAllText(Path.GetFullPath("../../work/f5/gear-visual-repair.txt"),report.ToString());
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();
 }
}

