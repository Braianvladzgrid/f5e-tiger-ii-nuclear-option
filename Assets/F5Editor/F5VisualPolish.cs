using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Visual additions only: no colliders, rigid bodies or aircraft parameter changes.
public static class F5VisualPolish {
 const string R=F5AircraftBuilder.R;
 static Material Metal(string name,Color color,float metallic,float smooth){
  string path=R+"/Visual/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
 }
 static Mesh Save(string name,List<Vector3> vertices,List<int> triangles){
  string path=R+"/Visual/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.SetVertices(vertices);for(int i=0;i<triangles.Count;i+=3){int temporary=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=temporary;}mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
 }
 static void Add(Transform parent,string name,Mesh mesh,Material material,Vector3 position){
  var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
 }
 public static Renderer[] Apply(GameObject root){
  Directory.CreateDirectory(R+"/Visual");
  var group=new GameObject("F5ExhaustDetails").transform;group.SetParent(root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="fuselage"),true);group.position=root.transform.position;group.rotation=root.transform.rotation;
  var steel=Metal("ExhaustSteel",new Color(.16f,.17f,.18f),.8f,.35f);var inner=Metal("ExhaustInterior",new Color(.025f,.022f,.02f),.35f,.15f);var blade=Metal("TurbineMetal",new Color(.12f,.105f,.085f),.7f,.3f);
  var v=new List<Vector3>();var tri=new List<int>();const int count=24;
  for(int i=0;i<count;i++){
   float a=(i*360f/count+.35f)*Mathf.Deg2Rad,b=((i+1)*360f/count-.35f)*Mathf.Deg2Rad;int k=v.Count;
   foreach(var sample in new[]{new Vector3(.258f,0,-.008f),new Vector3(.228f,0,-.008f),new Vector3(.245f,0,.07f),new Vector3(.220f,0,.07f)}){v.Add(new Vector3(Mathf.Sin(a)*sample.x,Mathf.Cos(a)*sample.x,sample.z));v.Add(new Vector3(Mathf.Sin(b)*sample.x,Mathf.Cos(b)*sample.x,sample.z));}
   tri.AddRange(new[]{k,k+3,k+1,k,k+2,k+3,k,k+1,k+5,k,k+5,k+4,k+2,k+7,k+3,k+2,k+6,k+7});
  }
  var petals=Save("NozzlePetals",v,tri);v.Clear();tri.Clear();v.Add(new Vector3(0,0,.16f));
  for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;v.Add(new Vector3(Mathf.Sin(a)*.225f,Mathf.Cos(a)*.225f,.16f));if(i>0)tri.AddRange(new[]{0,i+1,i});}
  var cavity=Save("NozzleCavity",v,tri);v.Clear();tri.Clear();
  for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16,b=a+.19f;int k=v.Count;v.Add(new Vector3(Mathf.Sin(a)*.04f,Mathf.Cos(a)*.04f,.145f));v.Add(new Vector3(Mathf.Sin(b)*.18f,Mathf.Cos(b)*.18f,.145f));v.Add(new Vector3(Mathf.Sin(b+.09f)*.18f,Mathf.Cos(b+.09f)*.18f,.145f));tri.AddRange(new[]{k,k+2,k+1});}
  var fan=Save("TurbineVanes",v,tri);
  for(int side=-1;side<=1;side+=2){var p=new Vector3(side*.270f,.185f,-5.135f);string suffix=side<0?"L":"R";Add(group,"NozzlePetals_"+suffix,petals,steel,p);Add(group,"NozzleCavity_"+suffix,cavity,inner,p);Add(group,"TurbineVanes_"+suffix,fan,blade,p);}
  return group.GetComponentsInChildren<Renderer>(true);
 }
}

