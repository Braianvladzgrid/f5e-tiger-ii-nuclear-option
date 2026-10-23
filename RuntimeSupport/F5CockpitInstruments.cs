using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

// Read-only instruments. No forces, input changes or colliders are created here.
public sealed class F5CockpitInstruments : MonoBehaviour {
 Aircraft aircraft; Transform panel; Font font; Turbojet jet; float next;
 readonly List<Renderer> renderers=new List<Renderer>();
 readonly List<Material> materials=new List<Material>();
 readonly List<Mesh> meshes=new List<Mesh>();
 Material ink,green,face; TextMesh attitudeText,systems,hudLeft,hudRight,caution;
 TextMesh gearLamp,brakeLamp,fuelLamp,engineLamp;
 Dial speed,altitude,vertical,fuel,rpm; LineRenderer horizon,velocityMarker;
 static readonly Color Green=new Color(.25f,1,.35f),Amber=new Color(1,.65f,.16f);
 sealed class Dial {public Transform needle;public TextMesh value;public float maximum;}
 void Start(){aircraft=GetComponent<Aircraft>();try{Build();}catch(Exception e){Debug.LogError("[F5Instruments] "+e);enabled=false;}}
 Transform Node(string name,Vector3 pos){var g=new GameObject(name);g.layer=3;g.transform.SetParent(panel,false);g.transform.localPosition=pos;return g.transform;}
 Material Solid(string name,Color color){var m=new Material(Shader.Find("Unlit/Color")??Shader.Find("Sprites/Default"));m.name=name;m.color=color;materials.Add(m);return m;}
 TextMesh Text(string name,Vector3 pos,string value,float size,Color color){var t=Node(name,pos).gameObject.AddComponent<TextMesh>();t.font=font;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;t.text=value;var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;renderers.Add(r);return t;}
 LineRenderer Line(string name,Vector3[] points,float width,Material mat,Transform parent=null){var t=Node(name,Vector3.zero);if(parent)t.SetParent(parent,false);var l=t.gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=l.endWidth=width;l.sharedMaterial=mat;l.numCapVertices=2;renderers.Add(l);return l;}
 Vector3[] Circle(Vector3 center,float radius,int count=48){return Enumerable.Range(0,count+1).Select(i=>center+new Vector3(Mathf.Sin(i*Mathf.PI*2/count),Mathf.Cos(i*Mathf.PI*2/count),0)*radius).ToArray();}
 void Quad(string name,Vector3 center,float w,float h,Material mat){var t=Node(name,center);var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-w/2,-h/2,0),new Vector3(w/2,-h/2,0),new Vector3(w/2,h/2,0),new Vector3(-w/2,h/2,0)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;renderers.Add(r);meshes.Add(mesh);}
 Dial Gauge(string label,float x,float y,float maximum,string units){
  var c=new Vector3(x,y,4.24f);const float r=.041f;Line(label+" bezel",Circle(c,r),.002f,ink);Line(label+" inner rim",Circle(c,r-.003f),.0008f,ink);
  for(int i=0;i<=20;i++){float a=(-135+i*13.5f)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);Line(label+" graduation "+i,new[]{c+d*(r-.005f),c+d*(r-(i%5==0?.014f:.009f))},.001f,ink);}
  Text(label+" legend",c+new Vector3(0,.016f,-.002f),label,.0015f,Color.white);Text(label+" units",c+new Vector3(0,.006f,-.002f),units,.0011f,Color.gray);
  var needle=Node(label+" needle",c+new Vector3(0,0,-.004f));Line(label+" pointer",new[]{new Vector3(0,-.006f,0),new Vector3(0,.029f,0)},.0015f,ink,needle);
  return new Dial{needle=needle,value=Text(label+" readout",c+new Vector3(0,-.025f,-.005f),"---",.0018f,Color.white),maximum=maximum};
 }
 void Read(Dial d,float value,string format="0"){d.value.text=value.ToString(format);d.needle.localRotation=Quaternion.Euler(0,0,135-Mathf.Clamp01(value/d.maximum)*270);}
 TextMesh Lamp(string name,float x){Quad(name+" recess",new Vector3(x,.936f,4.25f),.065f,.018f,face);return Text(name,new Vector3(x,.936f,4.235f),name,.0015f,Color.gray);}
 void Build(){
  var cockpit=aircraft.partLookup.OfType<AeroPart>().First(p=>p.name=="cockpit");panel=new GameObject("F5LiveInstruments").transform;panel.SetParent(cockpit.transform,false);panel.position=aircraft.transform.position;panel.rotation=aircraft.transform.rotation;
  // Retire the unconnected imported panel blanks that occlude live instruments.
  foreach(var root in aircraft.partLookup.OfType<AeroPart>().Select(p=>p.transform).Concat(new[]{aircraft.transform}).Distinct())
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
    string n=mf.name;bool blank=n=="Panel"||n.StartsWith("Panel_")||n=="Dash"||n.StartsWith("block")||n.StartsWith("rect")||n.StartsWith("poly")||n.StartsWith("object")||n.StartsWith("cylinder")||(n.StartsWith("disk")&&n!="disk");
    if(blank&&mf.GetComponentsInParent<Transform>(true).Any(t=>t.name=="F5Cockpit")&&mf.sharedMesh&&aircraft.transform.InverseTransformPoint(mf.GetComponent<Renderer>().bounds.center).z>3.7f)mf.sharedMesh=null;
   }
  font=Resources.GetBuiltinResource<Font>("Arial.ttf");ink=Solid("F5 ivory markings",new Color(.8f,.85f,.8f));green=Solid("F5 HUD phosphor",Green);face=Solid("F5 charcoal instrument panel",new Color(.012f,.018f,.02f));
  Quad("Instrument face",new Vector3(0,.785f,4.27f),.65f,.34f,face);
  speed=Gauge("AIRSPEED",-.18f,.873f,650,"KNOTS");altitude=Gauge("ALTITUDE",.18f,.873f,40000,"FT MSL");vertical=Gauge("CLIMB",0,.755f,100,"M/S");fuel=Gauge("FUEL",-.18f,.755f,3200,"KG");rpm=Gauge("ENGINE",.18f,.755f,110,"RPM %");
  var hc=new Vector3(0,.866f,4.235f);Line("Attitude bezel",Circle(hc,.047f),.002f,ink);
  horizon=Line("Live attitude horizon",new[]{hc,hc},.0018f,green);
  Line("Fixed miniature aircraft",new[]{hc+new Vector3(-.027f,0,-.003f),hc+new Vector3(-.008f,0,-.003f),hc+new Vector3(0,-.006f,-.003f),hc+new Vector3(.008f,0,-.003f),hc+new Vector3(.027f,0,-.003f)},.002f,ink);
  for(int i=-2;i<=2;i++){float a=i*30*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);Line("Bank index "+i,new[]{hc+d*.044f,hc+d*.04f},.001f,ink);}
  attitudeText=Text("Attitude readout",hc+new Vector3(0,-.034f,-.005f),"",.0013f,Color.white);
  gearLamp=Lamp("GEAR",-.24f);brakeLamp=Lamp("BRAKE",-.08f);fuelLamp=Lamp("FUEL",.08f);engineLamp=Lamp("ENGINE",.24f);
  systems=Text("Lower systems strip",new Vector3(0,.685f,4.23f),"",.0015f,Green);
  foreach(float x in new[]{-.30f,.30f})foreach(float y in new[]{.63f,.94f}){var c=new Vector3(x,y,4.235f);Line("Panel fastener",Circle(c,.003f,12),.001f,ink);Line("Fastener slot",new[]{c+Vector3.left*.002f,c+Vector3.right*.002f},.0007f,ink);}
  hudLeft=Text("HUD left data",new Vector3(-.27f,1.17f,4.25f),"",.00125f,Green);hudRight=Text("HUD right data",new Vector3(.27f,1.17f,4.25f),"",.00125f,Green);
  caution=Text("HUD caution",new Vector3(0,1.31f,4.25f),"",.0014f,Amber);
  // Flight path cue is bounded to the combiner area, never to flight controls.
  velocityMarker=Line("Air velocity cue",Circle(Vector3.zero,.008f,24),.0008f,green);
  var cluster=new GameObject("CompactInstrumentCluster").transform;cluster.SetParent(panel,false);
  foreach(var child in Enumerable.Range(0,panel.childCount).Select(panel.GetChild).Where(t=>t!=cluster&&!t.name.StartsWith("HUD")&&t.name!="Air velocity cue").ToArray())child.SetParent(cluster,false);
  cluster.localScale=Vector3.one*.72f;cluster.localPosition=new Vector3(0,.79f,4.24f)*.28f+new Vector3(0,-.035f,.13f);
  var f=AccessTools.Field(typeof(Aircraft),"cockpitRenderers");f.SetValue(aircraft,((Renderer[])f.GetValue(aircraft)).Concat(renderers).ToArray());
  jet=FindObjectsOfType<Turbojet>().FirstOrDefault(e=>(Aircraft)AccessTools.Field(typeof(Turbojet),"aircraft").GetValue(e)==aircraft);
  Debug.Log("[F5Instruments] Detailed panel initialized: 5 live dials, attitude horizon, 4 annunciators, velocity cue and HUD telemetry");
 }
 void LateUpdate(){
  if(!aircraft||!panel||Time.unscaledTime<next)return;next=Time.unscaledTime+.05f;var rb=aircraft.CockpitRB();if(!rb)return;
  var air=rb.velocity-aircraft.GetWindVelocity();var local=aircraft.transform.InverseTransformDirection(air);float tas=air.magnitude*3.6f,alt=aircraft.GlobalPosition().y,vvi=rb.velocity.y,f=aircraft.GetFuelQuantity(),power=jet?jet.GetRPMRatio()*100:0;
  float pitch=-Mathf.DeltaAngle(0,aircraft.transform.eulerAngles.x),bank=Mathf.DeltaAngle(0,aircraft.transform.eulerAngles.z);var input=aircraft.GetInputs();
  Read(speed,tas/1.852f,"000");Read(altitude,alt*3.28084f,"00000");Read(vertical,vvi+50);vertical.value.text=vvi.ToString("+0.0;-0.0;0.0");Read(fuel,f,"0000");Read(rpm,power,"000");
  float displacement=Mathf.Clamp(-pitch*.0015f,-.038f,.038f);float half=Mathf.Sqrt(.04f*.04f-displacement*displacement);var q=Quaternion.Euler(0,0,-bank);var hc=new Vector3(0,.866f,4.23f);horizon.SetPositions(new[]{hc+q*new Vector3(-half,displacement,0),hc+q*new Vector3(half,displacement,0)});attitudeText.text="P "+pitch.ToString("+0;-0;0")+"  B "+bank.ToString("+0;-0;0");
  bool gearDown=aircraft.gearState==LandingGear.GearState.LockedExtended;gearLamp.color=gearDown?Green:Color.gray;brakeLamp.color=input.brake>.05f?Amber:Color.gray;fuelLamp.color=f<500?Amber:Green;engineLamp.color=power>20?Green:Amber;
  systems.text="GEAR "+aircraft.gearState+"  BRAKE "+(input.brake*100).ToString("0")+"%\nF-5E   NATIVE SYSTEMS";
  string aoa=air.sqrMagnitude>=100? (Mathf.Atan2(-local.y,local.z)*Mathf.Rad2Deg).ToString("0.0"):"--";
  hudLeft.text="TAS "+tas.ToString("000")+" KM/H\nAOA "+aoa+" DEG\nFUEL "+f.ToString("0")+" KG";
  hudRight.text="ALT "+alt.ToString("0000")+" M\nV/S "+vvi.ToString("+0.0;-0.0;0.0")+" M/S\nRPM "+power.ToString("000")+"%";
  caution.text=f<500?"FUEL LOW":input.brake>.05f?"BRAKE":gearDown&&tas>350?"GEAR DOWN":"";
  velocityMarker.enabled=local.z>10&&renderers.Any(r=>r&&r!=velocityMarker&&r.enabled);velocityMarker.transform.localPosition=new Vector3(Mathf.Clamp(local.x/Mathf.Max(local.z,10)*.7f,-.20f,.20f),1.03f+Mathf.Clamp(local.y/Mathf.Max(local.z,10)*.7f,-.12f,.18f),4.25f);
 }
 void OnDestroy(){if(panel)Destroy(panel.gameObject);foreach(var m in materials)if(m)Destroy(m);foreach(var m in meshes)if(m)Destroy(m);}
}
