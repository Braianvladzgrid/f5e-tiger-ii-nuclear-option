using System;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

// Local integration-test fixture only. Never include in the distributed addon.
[BepInPlugin("local.f5.testprobe","Temporary F5 Test Probe","0.1.0")]
public sealed class F5TestProbe:BaseUnityPlugin {
 const string Folder=@"C:\Users\braia\Documents\Codex\2026-10-10\tui\work\f5";
 static Aircraft target;
 static bool driving,takeoff,cruise,landing,touched,fullPower; static float firingUntil,approachStarted;
 static float throttle,pitch,roll,yaw,brake=1,until,startY,runwayY;
 static Vector3 heading;
 bool bindingsLogged; float next; static int launchStation=-1; static float launchUntil,launchReport;
 static object Field(object o,string n)=>AccessTools.Field(o.GetType(),n)?.GetValue(o);
 static bool Own(Aircraft a)=>a&&a.definition&&a.definition.jsonKey=="F5E_TigerII";
 void Awake(){new Harmony("local.f5.testprobe").PatchAll(typeof(F5TestProbe));}
 string Guns(Aircraft a){return string.Join(";",a.weaponStations.Where(w=>w.WeaponInfo.gun).Select(w=>"station="+w.Number+" total="+w.GetAmmoTotal()+" loaded="+w.GetAmmoLoaded()+" safety="+w.SafetyIsOn(a)+" ready="+w.Ready()).ToArray());}
 void Log(string s){File.AppendAllText(Path.Combine(Folder,"test-telemetry.txt"),DateTime.UtcNow.ToString("O")+" "+s+Environment.NewLine);}
 void Update(){
  if(Time.unscaledTime<next)return;next=Time.unscaledTime+1;
  if(!bindingsLogged&&Rewired.ReInput.isReady){bindingsLogged=true;foreach(var action in Rewired.ReInput.mapping.Actions)if(action.name.Contains("View")||action.name.Contains("Camera")||action.name=="Center")Log("ACTION id="+action.id+" name="+action.name);}
  Aircraft a;if(!GameManager.GetLocalAircraft(out a)||!Own(a))return;
  if(target!=a){target=a;driving=true;takeoff=landing=false;throttle=pitch=roll=yaw=0;brake=1;until=Time.unscaledTime+600;Log("FIXTURE hold new spawn at idle with brake");}
  if(firingUntil>0&&Time.unscaledTime>=firingUntil){a.SetFiringState(0,false);firingUntil=0;Log("GUN STOP "+Guns(a));}
  if(launchReport>0&&Time.unscaledTime>=launchReport){a.SetFiringState(launchStation,false);Log("LAUNCH AFTER "+Station(a,launchStation));launchReport=0;launchStation=-1;}
  var path=Path.Combine(Folder,"test-command.txt");
  if(File.Exists(path)) {
   var command=File.ReadAllText(path).Trim();File.Delete(path);target=a;
   try {
    if(command=="place_runway"||command=="approach") {
     var airbase=a.GetAirbase()??FindObjectsOfType<Airbase>().OrderBy(b=>Vector3.Distance(b.transform.position,a.transform.position)).First();
     var usage=typeof(Airbase).GetMethod("GetTakeoffRunway").Invoke(airbase,new object[]{a,600f});
     var runway=(Airbase.Runway)usage.GetType().GetField("Runway").GetValue(usage);bool reverse=(bool)usage.GetType().GetField("Reverse").GetValue(usage);
     var near=reverse?runway.End:runway.Start;var far=reverse?runway.Start:runway.End;heading=Vector3.ProjectOnPlane(far.position-near.position,Vector3.up).normalized;
     runwayY=near.position.y;var origin=a.transform.position;var rotation=Quaternion.LookRotation(heading)*Quaternion.Inverse(a.transform.rotation);var destination=command=="approach"?near.position+heading*100+Vector3.up*25:near.position+heading*100+Vector3.up*1.6f;
     var bodies=a.partLookup.OfType<AeroPart>().Select(p=>p.rb).Where(b=>b).Concat(new[]{a.CockpitRB()}).Distinct().ToArray();
     var poses=bodies.Select(b=>new{body=b,position=destination+rotation*(b.position-origin),rotation=rotation*b.rotation}).ToArray();
     foreach(var p in poses){p.body.position=p.position;p.body.rotation=p.rotation;p.body.velocity=command=="approach"?heading*95:Vector3.zero;p.body.angularVelocity=Vector3.zero;}
     Physics.SyncTransforms();a.SetGear(true);driving=true;takeoff=landing=false;throttle=pitch=roll=yaw=0;brake=1;until=Time.unscaledTime+180;
     if(command=="approach"){landing=true;touched=false;approachStarted=Time.unscaledTime;throttle=.25f;brake=0;until=Time.unscaledTime+120;}
     Log("FIXTURE placed at runway; subsequent motion uses native forces. heading="+heading);
    } else if(command=="takeoff") {driving=takeoff=true;landing=false;cruise=false;startY=a.GlobalPosition().y;until=Time.unscaledTime+600;throttle=1;brake=0;}
    else if(command=="full_power"){fullPower=true;Log("SPEED TEST full power; native forces and controlled altitude, no speed hold");}
    else if(command=="cruise"){fullPower=false;}
    else if(command=="stop"){driving=true;takeoff=landing=false;throttle=pitch=roll=yaw=0;brake=1;until=Time.unscaledTime+180;}
    else if(command=="release"){driving=takeoff=landing=false;}
    else if(command=="fire_gun"){a.SetFiringState(0,true);firingUntil=Time.unscaledTime+3;Log("GUN START "+Guns(a));}
    else if(command=="stations"){foreach(var station in a.weaponStations)Log("STATION "+Station(a,station.Number));}
    else if(command.StartsWith("fire_station ")){int n=int.Parse(command.Substring(13));var manager=Field(a,"weaponManager") as WeaponManager;Log("LAUNCH BEFORE "+Station(a,n));manager.SetActiveStation((byte)n);launchStation=n;launchUntil=Time.unscaledTime+.3f;launchReport=Time.unscaledTime+2;}
    else if(command=="gear_up")a.SetGear(false);
    else if(command=="gear_down")a.SetGear(true);
    else if(command.StartsWith("throttle ")){driving=true;takeoff=false;throttle=float.Parse(command.Substring(9),System.Globalization.CultureInfo.InvariantCulture);pitch=roll=yaw=brake=0;until=Time.unscaledTime+30;}
    Log("COMMAND "+command);
   }catch(Exception ex){Log("COMMAND FAILED "+ex);driving=takeoff=landing=false;}
  }
  var rb=a.CockpitRB();var input=a.GetInputs();
  var cam=FindObjectOfType<CameraStateManager>();Log("CAMERA "+(cam?cam.currentState.GetType().Name:"none")+" pan="+(cam?Field(cam.currentState,"panView"):null)+" tilt="+(cam?Field(cam.currentState,"tiltView"):null));
  Log("speed="+rb.velocity.magnitude+" altitude="+a.GlobalPosition().y+" mass="+a.GetMass()+" CG="+a.transform.InverseTransformPoint(a.GetCenterOfMass())+" controls="+input.throttle+","+input.pitch+","+input.roll+","+input.yaw+","+input.brake+" rotation="+a.transform.eulerAngles+" health="+string.Join(",",a.partLookup.Select(p=>p.name+":"+p.hitPoints).ToArray()));
  foreach(var g in FindObjectsOfType<LandingGear>().Where(g=>(Aircraft)Field(g,"aircraft")==a))Log("GEAR "+g.name+" fold="+Field(g,"foldAmount")+" compression="+Field(g,"compressionDistance")+" contact="+g.WeightOnWheel(.1f)+" bump="+a.transform.InverseTransformPoint(((GameObject)Field(g,"bumpStop")).transform.position));
  foreach(var e in FindObjectsOfType<Turbojet>().Where(e=>(Aircraft)Field(e,"aircraft")==a)){
   Log("ENGINE "+e.name+" max="+Field(e,"maxThrust")+" current="+Field(e,"thrust")+" afterburnerOn="+Field(e,"afterburnerOn"));
   foreach(var nozzle in (Array)Field(e,"nozzles"))foreach(var burner in (Array)Field(nozzle,"afterburners")){
    var flame=Field(burner,"flameRenderer") as Renderer;
    Log("AFTERBURNER "+((Component)nozzle).name+" amount="+Field(burner,"afterburnerAmount")+" flameEnabled="+(flame&&flame.enabled)+" active="+(flame&&flame.gameObject.activeInHierarchy));
   }
  }
 }
 string Station(Aircraft a,int n){var s=a.weaponStations[n];return "station="+n+" name="+s.WeaponInfo.weaponName+" total="+s.GetAmmoTotal()+" safety="+s.SafetyIsOn(a)+" ready="+s.Ready()+" intactHardpointWeapons="+s.Weapons.Count(w=>w&&w.IsAttached());}
 void FixedUpdate(){
  if(launchStation>=0&&Time.unscaledTime<launchUntil&&Own(target)){var manager=Field(target,"weaponManager") as WeaponManager;if(manager)manager.Fire();}
  if(firingUntil>Time.unscaledTime&&Own(target)){var manager=Field(target,"weaponManager") as WeaponManager;if(manager)manager.FireGuns();}
  if(!driving||!Own(target))return;
  if(Time.unscaledTime>until){driving=takeoff=false;Log("TIMEOUT released all test inputs");return;}
  if(landing){var body=target.CockpitRB();var angularLocal=target.transform.InverseTransformDirection(body.angularVelocity);float height=target.GlobalPosition().y-runwayY;float desiredVertical=height>6?-3f:-1.2f;float noseUp=Mathf.Clamp(3+(desiredVertical-body.velocity.y)*2,-4,14);pitch=Mathf.Clamp(Mathf.DeltaAngle(target.transform.eulerAngles.x,-noseUp)*.12f-angularLocal.x*.3f,-.9f,.9f);roll=Mathf.Clamp(Mathf.DeltaAngle(0,target.transform.eulerAngles.z)*.04f+angularLocal.z*.2f,-.3f,.3f);yaw=Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(target.transform.forward,Vector3.up),heading,Vector3.up)/20,-.3f,.3f);throttle=Mathf.Clamp(.12f+(95-body.velocity.magnitude)*.015f,0,.6f);brake=0;if(Time.unscaledTime>approachStarted+2&&FindObjectsOfType<LandingGear>().Count(g=>(Aircraft)Field(g,"aircraft")==target&&g.WeightOnWheel(.01f))>=2)touched=true;if(touched){throttle=0;brake=1;pitch=roll=yaw=0;if(body.velocity.magnitude<2){Log("LANDING STOPPED health="+string.Join(",",target.partLookup.Select(p=>p.name+":"+p.hitPoints).ToArray()));landing=false;}}return;}
  if(!takeoff)return;
  if(target.partLookup.Any(p=>p.hitPoints<=0)){Log("TAKEOFF FAIL: structural damage; stopping test");takeoff=false;throttle=pitch=roll=yaw=0;brake=1;return;}
  var rb=target.CockpitRB();float speed=rb.velocity.magnitude;var angular=target.transform.InverseTransformDirection(rb.angularVelocity);
  float desiredPitch=speed>65?-10:0;
  if(cruise)desiredPitch=-Mathf.Clamp(4+(startY+300-target.GlobalPosition().y)*.04f,-4,12);
  pitch=Mathf.Clamp(Mathf.DeltaAngle(target.transform.eulerAngles.x,desiredPitch)*.12f-angular.x*.3f,-.9f,.9f);
  roll=Mathf.Clamp(Mathf.DeltaAngle(0,target.transform.eulerAngles.z)*.04f+angular.z*.2f,-.3f,.3f);
  yaw=Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(target.transform.forward,Vector3.up),heading,Vector3.up)/20,-.3f,.3f);
  throttle=fullPower?1:cruise?Mathf.Clamp(.6f+(180-speed)*.01f,.3f,.9f):1;brake=0;
  if(!cruise&&target.GlobalPosition().y-startY>100){Log("TAKEOFF PASS: 100m climb using native forces; continuing controlled flight");cruise=true;}
 }
 [HarmonyPatch(typeof(Aircraft),"GetInputs"),HarmonyPostfix,HarmonyPriority(Priority.Last)]
 static void Inputs(Aircraft __instance,ControlInputs __result){if(driving&&__instance==target&&__result!=null){__result.throttle=throttle;__result.pitch=pitch;__result.roll=roll;__result.yaw=yaw;__result.brake=brake;}}
 [HarmonyPatch(typeof(Aircraft),"FilterInputs"),HarmonyPrefix]static void Filter(Aircraft __instance){Inputs(__instance,__instance.GetInputs());}
 [HarmonyPatch(typeof(Turbojet),"FixedUpdate"),HarmonyPrefix,HarmonyPriority(Priority.Last)]static void Engine(Aircraft ___aircraft,ControlInputs ___controlInputs){Inputs(___aircraft,___controlInputs);}
 [HarmonyPatch(typeof(LandingGear),"FixedUpdate"),HarmonyPrefix,HarmonyPriority(Priority.Last)]static void Gear(Aircraft ___aircraft,ControlInputs ___controlInputs){Inputs(___aircraft,___controlInputs);}
}




