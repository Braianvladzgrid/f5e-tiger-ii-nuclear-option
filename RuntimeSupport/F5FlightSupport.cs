using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

// Blueprinter replaces collider meshes, so Unity's automatic body centers no
// longer match the donor's mass stations. Keep those stations explicit.
[BepInPlugin("vladzgrid.f5.flightsupport", "F-5E Flight Support", "0.1.0")]
public sealed class F5FlightFix : BaseUnityPlugin
{
    float nextScan;
    static int collisionReports;
    [HarmonyPatch(typeof(AeroPart), "OnCollisionEnter"), HarmonyPrefix]
    static void CollisionReport(AeroPart __instance, Collision __0) {
        var plane=__instance.parentUnit as Aircraft;
        if(!IsF5(plane)||collisionReports++>60)return;
        Debug.Log("[F5Contact] part="+__instance.name+" other="+__0.collider.name+" impulse="+__0.impulse.magnitude+" relative="+__0.relativeVelocity+" own="+__0.GetContact(0).thisCollider.name);
    }
    static bool IsF5(Aircraft plane) {
        return plane && ((plane.definition&&plane.definition.jsonKey=="F5E_TigerII") || plane.name.StartsWith("F5") || plane.transform.Find("F5Exterior") || plane.transform.Find("F5GearVisuals"));
    }
    void Awake() { new Harmony("vladzgrid.f5.flightsupport").PatchAll(typeof(F5FlightFix)); }
    // The donor HUD has six pylon graphics. New centerline/cannon indices must
    // remain valid; the compact F-5 panel reports all seven external stations.
    [HarmonyPatch(typeof(PylonIndicator), "UpdateImages"), HarmonyPrefix]
    static void ExpandPylonIndicator(PylonIndicator __instance) {
        var plane=AccessTools.Field(typeof(PylonIndicator),"attachedAircraft").GetValue(__instance) as Aircraft;
        if(!IsF5(plane))return;
        var field=AccessTools.Field(typeof(PylonIndicator),"pylonElementSets");var old=field.GetValue(__instance) as Array;
        if(old==null||old.Length>=8)return;
        var element=old.GetType().GetElementType();var expanded=Array.CreateInstance(element,8);Array.Copy(old,expanded,old.Length);
        for(int i=old.Length;i<8;i++) {
            var set=Activator.CreateInstance(element);
            foreach(var f in element.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))if(f.FieldType.IsArray)f.SetValue(set,Array.CreateInstance(f.FieldType.GetElementType(),0));
            expanded.SetValue(set,i);
        }
        field.SetValue(__instance,expanded);
        Debug.Log("[F5HUD] Expanded native station index coverage to eight including cannon");
    }
    void Update()
    {
        // Blueprinter finishes adding custom children after the donor's Awake.
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+.5f;
        foreach(var plane in UnityEngine.Object.FindObjectsOfType<Aircraft>(true)) {
            if(!IsF5(plane)||plane.partLookup==null)continue;
            SourceGearVisuals(plane);
            Aircraft localPlane;
            if(GameManager.GetLocalAircraft(out localPlane)&&localPlane==plane&&!plane.GetComponent<F5CockpitInstruments>())plane.gameObject.AddComponent<F5CockpitInstruments>();
            var roots=plane.partLookup.OfType<AeroPart>().Select(p=>p.transform).Concat(new[]{plane.transform}).Distinct();
            foreach(var root in roots) {
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true)) {
                    string name=mesh.name;
                    if(name.StartsWith("wheel_")||name.StartsWith("gear_")||name.StartsWith("geardoor")||name.StartsWith("gearHinge"))mesh.sharedMesh=null;
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
                    if(t.name=="gearlight_F")t.gameObject.SetActive(false);
                    if(t.name=="navlight_effects_L")t.position=plane.transform.TransformPoint(new Vector3(-4.02f,0,-.1f));
                    if(t.name=="navlight_effects_R")t.position=plane.transform.TransformPoint(new Vector3(4.02f,0,-.1f));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "Awake"), HarmonyPostfix]
    static void SourceGearVisuals(Aircraft __instance)
    {
        if (IsF5(__instance)
            && __instance.transform.Find("F5GearVisuals")
            && !__instance.GetComponent<F5GearAnimation>())
            __instance.gameObject.AddComponent<F5GearAnimation>();
    }

    // Local F-5E only: native parked state leaves brake=1, even without a
    // manual request. Read the same manual Brake action used by the player state.
    [HarmonyPatch(typeof(Aircraft), "GetInputs"), HarmonyPostfix]
    static void ManualBrakeOnly(Aircraft __instance, ControlInputs __result)
    {
        if (!IsF5(__instance)
            || __result == null || !Rewired.ReInput.isReady) return;
        Aircraft local;
        if (!GameManager.GetLocalAircraft(out local) || local != __instance) return;
        __result.brake = Rewired.ReInput.players.GetPlayer(0).GetButton("Brake") ? 1f : 0f;
    }

    // Gears cache the shared input object. Clear an automatic brake before wheel
    // physics even when a parked pilot has not requested GetInputs again.
    [HarmonyPatch(typeof(LandingGear), "FixedUpdate"), HarmonyPrefix]
    static void WheelBrake(Aircraft ___aircraft, ControlInputs ___controlInputs)
    {
        if (___aircraft) ManualBrakeOnly(___aircraft, ___controlInputs);
    }

    // Native gear also applies a second automatic brake directly to a local
    // variable, independent of ControlInputs. Disable that threshold locally.
    static float AutomaticBrakeThreshold(LandingGear gear)
    {
        var plane=(Aircraft)AccessTools.Field(typeof(LandingGear),"aircraft").GetValue(gear);
        Aircraft local;
        return plane && (IsF5(plane) || (plane.definition&&plane.definition.jsonKey=="Su25_Frogfoot"))
            && GameManager.GetLocalAircraft(out local) && plane==local ? -1f : 1f;
    }
    [HarmonyPatch(typeof(LandingGear), "FixedUpdate"), HarmonyTranspiler, HarmonyPriority(Priority.Last)]
    static IEnumerable<CodeInstruction> RemoveAutomaticWheelBrake(IEnumerable<CodeInstruction> original)
    {
        var code=original.ToList();int replaced=0;
        for(int j=0;j<code.Count;j++) if(code[j].opcode==OpCodes.Call && code[j].operand is MethodInfo && ((MethodInfo)code[j].operand).Name=="AutomaticBrakeThreshold" && ((MethodInfo)code[j].operand).DeclaringType.Name=="Su25FlightFix") {code[j].operand=AccessTools.Method(typeof(F5FlightFix),"AutomaticBrakeThreshold");replaced++;}
        for(int i=1;i<code.Count;i++)if(code[i].opcode==OpCodes.Ldc_R4
            && Convert.ToSingle(code[i].operand)==1f
            && code[i-1].opcode==OpCodes.Ldfld
            && code[i-1].operand is FieldInfo
            && ((FieldInfo)code[i-1].operand).Name=="speed"
            && ((FieldInfo)code[i-1].operand).DeclaringType==typeof(Unit))
        {
            var load=new CodeInstruction(OpCodes.Ldarg_0);load.labels.AddRange(code[i].labels);load.blocks.AddRange(code[i].blocks);
            code[i]=load;
            code.Insert(i+1,new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(F5FlightFix),"AutomaticBrakeThreshold")));
            replaced++;i++;
        }
        if(replaced!=1)throw new InvalidOperationException("Native automatic wheel brake pattern changed; expected one threshold");
        return code;
    }

    [HarmonyPatch(typeof(Aircraft), "SetComplexPhysics"), HarmonyPostfix]
    static void SetMassStations(Aircraft __instance)
    {
        if(IsF5(__instance)||(__instance.definition&&__instance.definition.jsonKey=="Fighter1"))Debug.Log("[F5Physics] identity name="+__instance.name+" key="+(__instance.definition?__instance.definition.jsonKey:"null")+" sourceVisual="+(__instance.transform.Find("F5Exterior")!=null));
        if (!IsF5(__instance)) return;
        Debug.Log("[F5Physics] complex bodies name="+__instance.name+" key="+(__instance.definition?__instance.definition.jsonKey:"null")+" parts="+__instance.partLookup.Count);
        SourceGearVisuals(__instance);
        var bodies = new HashSet<Rigidbody>();
        foreach (var part in __instance.partLookup.OfType<AeroPart>())
            if (part.rb && bodies.Add(part.rb))
            {   part.rb.centerOfMass = part.rb.transform.InverseTransformPoint(part.transform.position);
                Debug.Log("[F5Body] "+part.name+" mass="+part.rb.mass+" position="+__instance.transform.InverseTransformPoint(part.transform.position)+" colliders="+string.Join(",",part.GetComponentsInChildren<Collider>(true).Select(c=>c.name+":"+c.bounds.min.y.ToString("F2")).ToArray()));
            }
    }
}

// Visual-only source animation. Native landing gear still handles contacts,
// suspension, drag, damage and the aircraft's actual gear state.
public sealed class F5GearAnimation : MonoBehaviour
{
    sealed class Track { public Transform pivot; public Quaternion rest; public AnimationCurve curve; public LandingGear gear; public bool compression; }
    sealed class Visibility { public Transform root; public LandingGear gear; }
    sealed class Wheel {public Transform pivot;public Quaternion rest;public Vector3 position;public LandingGear gear;public float angle;}
    readonly System.Collections.Generic.List<Wheel> wheels=new System.Collections.Generic.List<Wheel>();
    readonly System.Collections.Generic.List<Track> tracks = new System.Collections.Generic.List<Track>();
    readonly System.Collections.Generic.List<Visibility> visibility = new System.Collections.Generic.List<Visibility>();
    static readonly FieldInfo Fold = AccessTools.Field(typeof(LandingGear), "foldAmount");
    static readonly FieldInfo Compression = AccessTools.Field(typeof(LandingGear), "compressionDistance");
    static readonly FieldInfo Travel = AccessTools.Field(typeof(LandingGear), "suspensionTravel");
    static readonly FieldInfo WheelSpeed=AccessTools.Field(typeof(LandingGear),"wheelSpeed");
    static readonly FieldInfo WheelRadius=AccessTools.Field(typeof(LandingGear),"wheelRadius");
    static readonly FieldInfo GearAircraft=AccessTools.Field(typeof(LandingGear),"aircraft");
    Aircraft aircraft;
    bool initialized;
    float nextBind;
    int bindAttempts;
    void Start()
    {
        aircraft = GetComponent<Aircraft>();
        Bind();
    }
    void Bind()
    {
        nextBind=Time.unscaledTime+.2f;bindAttempts++;
        // Complex physics reparents AeroParts outside the aircraft hierarchy.
        // LandingGear can sit below a part rather than on its component root.
        // Match the serialized owner for externally reparented gear assemblies.
        var gears = aircraft.partLookup.OfType<AeroPart>()
            .SelectMany(p=>p.GetComponentsInChildren<LandingGear>(true))
            .Concat(UnityEngine.Object.FindObjectsOfType<LandingGear>(true)
                .Where(g=>(Aircraft)GearAircraft.GetValue(g)==aircraft))
            .Where(g=>(Aircraft)GearAircraft.GetValue(g)==aircraft).Distinct().ToArray();
        if(gears.Length!=3){
            if(bindAttempts>=25){Debug.LogError("[F5GearAnimation] Expected three owned landing gears, found "+gears.Length);enabled=false;}
            return;
        }
        foreach (var t in transform.Find("F5GearVisuals").GetComponentsInChildren<Transform>(true))
        {
            var data=t.name.Split('|');
            if(data.Length<2)continue;
            var gear=gears.FirstOrDefault(g=>g.name==(data[1]=="F"?"gear_F":"gear_"+data[1]+"_sprung"));
            if(!gear)continue;
            if(data[0]=="F5GearVisible")visibility.Add(new Visibility {root=t,gear=gear});
            if(data[0]=="F5GearWheel")wheels.Add(new Wheel {pivot=t,rest=t.localRotation,position=t.localPosition,gear=gear});
            if((data[0]!="F5GearCurve"&&data[0]!="F5GearCompression")||data.Length<3)continue;
            var curve=new AnimationCurve();
            foreach(var pair in data[2].Split(';')){
                var values=pair.Split(',');
                curve.AddKey(new Keyframe(float.Parse(values[0],CultureInfo.InvariantCulture),float.Parse(values[1],CultureInfo.InvariantCulture)));
            }
            // XML interpolation is piecewise linear, not Unity's smooth spline.
            for(int k=0;k<curve.length;k++){
                var key=curve.keys[k];
                key.inTangent=k==0?0:(key.value-curve.keys[k-1].value)/(key.time-curve.keys[k-1].time);
                key.outTangent=k==curve.length-1?0:(curve.keys[k+1].value-key.value)/(curve.keys[k+1].time-key.time);
                curve.MoveKey(k,key);
            }
            tracks.Add(new Track {pivot=t,rest=t.localRotation,curve=curve,gear=gear,compression=data[0]=="F5GearCompression"});
        }
        Debug.Log("[F5GearAnimation] initialized source tracks="+tracks.Count+" assemblies="+visibility.Count+" gears="+gears.Length+" names="+string.Join(",",gears.Select(g=>g.name).ToArray()));
        if(tracks.Count==0){Debug.LogError("[F5GearAnimation] No animation curves resolved; disabling animator");enabled=false;return;}
        initialized=true;
    }
    float Extension(LandingGear gear)
    {
        if(aircraft.gearState==LandingGear.GearState.LockedRetracted)return 0;
        if(aircraft.gearState==LandingGear.GearState.LockedExtended)return 1;
        return 1-Mathf.Clamp01((float)Fold.GetValue(gear));
    }
    void LateUpdate()
    {
        if(!initialized){if(aircraft&&Time.unscaledTime>=nextBind)Bind();return;}
        foreach(var t in tracks)if(t.pivot&&t.gear){
            float value=t.compression?Mathf.Clamp01((float)Compression.GetValue(t.gear)/(float)Travel.GetValue(t.gear)):Extension(t.gear);
            t.pivot.localRotation=t.rest*Quaternion.AngleAxis(t.curve.Evaluate(value),Vector3.right);
        }
        foreach(var v in visibility)if(v.root&&v.gear)v.root.gameObject.SetActive(Extension(v.gear)>.001f);
        foreach(var w in wheels)if(w.pivot&&w.gear){
            w.pivot.localPosition=w.position+w.pivot.parent.InverseTransformVector(aircraft.transform.up*(float)Compression.GetValue(w.gear));
            w.angle=(w.angle+(float)WheelSpeed.GetValue(w.gear)/(float)WheelRadius.GetValue(w.gear)*Mathf.Rad2Deg*Time.deltaTime)%360;
            w.pivot.localRotation=w.rest*Quaternion.AngleAxis(w.angle,Vector3.right);
        }
    }
}



