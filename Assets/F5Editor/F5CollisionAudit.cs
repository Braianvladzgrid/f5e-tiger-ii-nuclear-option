using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class F5CollisionAudit {
 static Component Owner(Collider c)=>c.GetComponentsInParent<Component>(true).FirstOrDefault(p=>p&&p.GetType().Name=="AeroPart");
 static bool Linked(Component a,Component b){var joints=new SerializedObject(a).FindProperty("joints");if(joints==null)return false;for(int i=0;i<joints.arraySize;i++)if(joints.GetArrayElementAtIndex(i).FindPropertyRelative("connectedPart").objectReferenceValue==b)return true;return false;}
 public static void Run(){
  var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(F5AircraftBuilder.R+"/F5.prefab"));
  try {
   Physics.SyncTransforms();var report=new List<string>();var all=root.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&Owner(c)).ToArray();
   for(int i=0;i<all.Length;i++)for(int j=i+1;j<all.Length;j++){
    var a=all[i];var b=all[j];var pa=Owner(a);var pb=Owner(b);if(pa==pb||Linked(pa,pb)||Linked(pb,pa))continue;
    Vector3 direction;float depth;if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out direction,out depth)&&depth>.001f)report.Add(a.name+" / "+b.name+" penetration="+depth.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
   }
   int overlaps=report.Count;report.Insert(0,"Unlinked hull penetrations="+overlaps);
   Func<string,Bounds> bounds=n=>all.Single(c=>c.name==n).bounds;
   var gaps=new[]{bounds("vstab2").min.y-bounds("vstab1").max.y,bounds("aileron_R").min.x-bounds("flap_R").max.x,bounds("flap_L").min.x-bounds("aileron_L").max.x};
   report.Add("Seam gaps vertical tail / right flap-aileron / left flap-aileron="+string.Join(", ",gaps.Select(g=>g.ToString("F4",System.Globalization.CultureInfo.InvariantCulture))));
   File.WriteAllLines("../../work/f5/collision-audit.txt",report);Debug.Log("[F5] Unlinked hull overlaps="+overlaps);
  }finally{Object.DestroyImmediate(root);}
 }
}
