using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class F5StationAudit {
 public static void Run(){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(F5AircraftBuilder.R+"/F5.prefab");var root=Object.Instantiate(prefab);var report=new StringBuilder();
  try{
   var wm=new SerializedObject(F5Pipeline.Comp(root,"WeaponManager"));var sets=wm.FindProperty("hardpointSets");int count=0;
   for(int s=1;s<sets.arraySize;s++){var set=sets.GetArrayElementAtIndex(s);var points=set.FindPropertyRelative("hardpoints");var options=set.FindPropertyRelative("weaponOptions");
    for(int i=0;i<points.arraySize;i++){count++;var mount=points.GetArrayElementAtIndex(i).FindPropertyRelative("transform").objectReferenceValue as Transform;report.AppendLine(set.FindPropertyRelative("name").stringValue+" "+mount.position.ToString("F3"));
     for(int k=1;k<options.arraySize;k++){var definition=options.GetArrayElementAtIndex(k).objectReferenceValue;var ds=new SerializedObject(definition);var weapon=(GameObject)ds.FindProperty("prefab").objectReferenceValue;var instance=Object.Instantiate(weapon);instance.transform.position=mount.position;instance.transform.rotation=mount.rotation;
      try{var renderers=instance.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh).ToArray();if(renderers.Length==0){report.AppendLine("  "+definition.name+" no editor mesh");continue;}var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);bool overlap=false;
       foreach(string surface in new[]{"LAileron","RAileron","LBFlap","RBFlap"}){var target=root.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name==surface);if(target){var pivot=target.transform.parent;var original=pivot.localRotation;foreach(float angle in new[]{-45f,-30f,-15f,0f,15f,30f,45f}){pivot.localRotation=original*Quaternion.Euler(angle,0,0);if(target.bounds.Intersects(bounds))overlap=true;}pivot.localRotation=original;}}
       report.AppendLine("  "+definition.name+" min="+bounds.min.ToString("F3")+" max="+bounds.max.ToString("F3")+" surfaceSweepAabbOverlap="+overlap);
      }finally{Object.DestroyImmediate(instance);}
     }
    }
   }
   if(count!=7)throw new Exception("Expected seven external stations, found "+count);if(!root.transform.Find("F5CockpitExterior"))throw new Exception("Exterior cockpit missing");report.AppendLine("PASS seven external mounts; exterior cockpit present. AABB sweep sampled local X deflections -45 to +45 degrees. Not an in-game firing or separation test.");
   File.WriteAllText("../../work/f5/station-audit.txt",report.ToString());Debug.Log("[F5StationAudit] PASS");
  }finally{Object.DestroyImmediate(root);}
 }
}
