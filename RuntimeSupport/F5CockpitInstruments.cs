using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

// Instruments read native simulation values; they never write flight inputs.
public sealed class F5CockpitInstruments : MonoBehaviour {
 Aircraft aircraft; Transform panel; Font font; Material ink,face; readonly List<Renderer> renderers=new List<Renderer>();
 TextMesh speedText,altText,attitudeText,systems,hud; Transform speedNeedle,altNeedle; float next;
 static readonly Color Green=new Color(.2f,1f,.25f);
 void Start(){
  aircraft=GetComponent<Aircraft>();
  try {Build();}catch(Exception ex){Debug.LogError("[F5Instruments] "+ex);enabled=false;}
 }
 Transform Node(string name,Vector3 position){var g=new GameObject(name);g.layer=3;g.transform.SetParent(panel,false);g.transform.localPosition=position;return g.transform;}
 Material Solid(string name,Color color){var shader=Shader.Find("Unlit/Color")??Shader.Find("Sprites/Default");var m=new Material(shader);m.name=name;m.color=color;return m;}
 TextMesh Text(string name,Vector3 position,string value,float size,Color color){
  var t=Node(name,position).gameObject.AddComponent<TextMesh>();t.font=font;t.fontSize=64;t.characterSize=size*.3f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;t.text=value;
  var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;renderers.Add(r);return t;
 }
 void Line(string name,Vector3[] points,float width,Material mat,Transform parent=null){
  var t=Node(name,Vector3.zero);if(parent)t.SetParent(parent,false);
  var l=t.gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=l.endWidth=width;l.sharedMaterial=mat;l.numCapVertices=2;renderers.Add(l);
 }
 Transform Dial(string label,float x,out TextMesh value){
  var center=new Vector3(x,.77f,4.24f);const float radius=.05f;
  var p=new Vector3[65];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/64;p[i]=center+new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*radius;}
  Line(label+" bezel",p,.005f,ink);
  for(int i=0;i<21;i++){float a=(-135+i*13.5f)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);Line(label+" tick "+i,new[]{center+d*(radius-.004f),center+d*(radius-(i%5==0?.018f:.010f))},.0015f,ink);}
  Text(label+" title",center+new Vector3(0,.025f,-.005f),label,.007f,Color.white);
  value=Text(label+" value",center+new Vector3(0,-.035f,-.006f),"---",.009f,Color.white);
  var needle=Node(label+" needle",center+new Vector3(0,0,-.008f));Line(label+" pointer",new[]{Vector3.zero,new Vector3(0,.050f,0)},.002f,ink,needle);return needle;
 }
 void Build(){
  var cockpit=aircraft.partLookup.OfType<AeroPart>().FirstOrDefault(p=>p.name=="cockpit");if(!cockpit)throw new Exception("Cockpit part unavailable");
  panel=new GameObject("F5LiveInstruments").transform;panel.SetParent(cockpit.transform,false);
  // Positions are defined in the aircraft station system before physics reparenting.
  panel.position=aircraft.transform.position;panel.rotation=aircraft.transform.rotation;
  font=Resources.GetBuiltinResource<Font>("Arial.ttf");ink=Solid("F5 instrument white",new Color(.8f,.85f,.8f));face=Solid("F5 panel charcoal",new Color(.018f,.025f,.028f));
  var backing=Node("Instrument face",new Vector3(0,.68f,4.27f));var mf=backing.gameObject.AddComponent<MeshFilter>();var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.35f,-.15f,0),new Vector3(.35f,-.15f,0),new Vector3(.35f,.15f,0),new Vector3(-.35f,.15f,0)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mf.sharedMesh=mesh;var mr=backing.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=face;renderers.Add(mr);
  speedNeedle=Dial("KM/H",-.22f,out speedText);altNeedle=Dial("ALT M",.22f,out altText);attitudeText=Text("Attitude",new Vector3(0,.72f,4.23f),"",.009f,Color.white);
  systems=Text("Engine fuel gear strip",new Vector3(0,.65f,4.23f),"",.008f,Green);
  hud=Text("HUD supplementary flight data",new Vector3(-.24f,1.10f,4.25f),"",.006f,Green);
  var f=AccessTools.Field(typeof(Aircraft),"cockpitRenderers");var old=(Renderer[])f.GetValue(aircraft);f.SetValue(aircraft,old.Concat(renderers).ToArray());
  Debug.Log("[F5Instruments] Live airspeed, altitude, attitude, fuel, RPM, gear and supplementary HUD initialized");
 }
 void LateUpdate(){
  if(!aircraft||!panel||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
  var rb=aircraft.CockpitRB();if(!rb)return;var air=rb.velocity-aircraft.GetWindVelocity();float speed=air.magnitude*3.6f,alt=aircraft.GlobalPosition().y;
  float pitch=-Mathf.DeltaAngle(0,aircraft.transform.eulerAngles.x),bank=Mathf.DeltaAngle(0,aircraft.transform.eulerAngles.z);var input=aircraft.GetInputs();
  speedText.text=speed.ToString("000");altText.text=alt.ToString("0000");attitudeText.text="P "+pitch.ToString("+0;-0;0")+"\nB "+bank.ToString("+0;-0;0");
  speedNeedle.localRotation=Quaternion.Euler(0,0,135-Mathf.Clamp01(speed/1200)*270);altNeedle.localRotation=Quaternion.Euler(0,0,135-Mathf.Clamp01(alt/10000)*270);
  var jet=FindObjectsOfType<Turbojet>().FirstOrDefault(e=>(Aircraft)AccessTools.Field(typeof(Turbojet),"aircraft").GetValue(e)==aircraft);
  string rpm=jet?(jet.GetRPMRatio()*100).ToString("0"):"--";
  systems.text="FUEL "+aircraft.GetFuelQuantity().ToString("0")+" KG   RPM "+rpm+"%\nGEAR "+aircraft.gearState+"   BRAKE "+(input.brake*100).ToString("0")+"%";
  var localAir=aircraft.transform.InverseTransformDirection(air);float aoa=Mathf.Atan2(-localAir.y,localAir.z)*Mathf.Rad2Deg;float vvi=rb.velocity.y;hud.text="F-5E\nTAS "+speed.ToString("0")+" KM/H\nV/S "+vvi.ToString("+0.0;-0.0;0.0")+" M/S\nAOA "+aoa.ToString("0.0")+" DEG\nFUEL "+aircraft.GetFuelQuantity().ToString("0")+" KG";
 }
 void OnDestroy(){if(panel)Destroy(panel.gameObject);if(ink)Destroy(ink);if(face)Destroy(face);}
}




