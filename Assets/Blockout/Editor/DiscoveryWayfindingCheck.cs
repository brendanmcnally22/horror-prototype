using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class DiscoveryWayfindingCheck
{
    const string Key="DiscoveryWayfindingCheck.Running";
    static string Log(string name)=>Application.dataPath+"/../Logs/"+name;
    static int phase;
    static double start,step;
    static readonly List<string> results=new List<string>();
    static readonly List<string> errors=new List<string>();
    static PlayerManager player;
    static PrototypeMover mover;
    static PlatformSafetyGate gate;
    static FlashlightItem flashlight;
    static FacilityFlashlightBeam beam;
    static Vector3 origin;
    static DiscoveryWayfindingCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){phase=0;start=EditorApplication.timeSinceStartup;results.Clear();errors.Clear();Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.playModeStartScene=null;}
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!m.StartsWith("Layout update failed"))errors.Add(m);};
    }
    [MenuItem("Tools/Blockout/Test Discovery Signs Elevator and Flashlight")]
    static void Begin()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Wait for edit mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.isDirty)throw new Exception("Save current changes before the check.");
        if(scene.path!=DiscoverySetup.ScenePath)throw new Exception("Open Discovery Blank.");
        SessionState.SetBool(Key,true);EditorSceneManager.playModeStartScene=null;EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Press(bool value)
    {
        Require(Keyboard.current!=null,"Keyboard missing.");
        InputSystem.QueueStateEvent(Keyboard.current,value?new KeyboardState(KeyCodeF):new KeyboardState());
    }
    const UnityEngine.InputSystem.Key KeyCodeF=UnityEngine.InputSystem.Key.F;
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||start==0||phase==99)return;
        try
        {
            double now=EditorApplication.timeSinceStartup;
            if(now-start>65)throw new Exception("Focused play check timed out at phase "+phase);
            if(phase==0)
            {
                if(now-start<5)return;
                player=PlayerManager.Instance;flashlight=player.GetComponentInChildren<FlashlightItem>(true);beam=player.GetComponent<FacilityFlashlightBeam>();
                mover=UnityEngine.Object.FindObjectsByType<PrototypeMover>(FindObjectsSortMode.None).Single(m=>m.name=="STARTING ELEVATOR");
                gate=mover.GetComponentInChildren<PlatformSafetyGate>();origin=mover.MovingPart.localPosition;
                Require(!gate.IsOpen&&gate.ReadyForTravel,"Starting cabin is not closed.");
                Require(Inventory.Instance.GetItemQuantity("ba53a6ad76bb4513ba434a3b7542345a")==1,"Starting flashlight missing from inventory.");
                Press(true);step=now;phase=1;return;
            }
            if(phase==1)
            {
                if(now-step<.25)return;Press(false);step=now;phase=2;return;
            }
            if(phase==2)
            {
                if(now-step<2.5)return;
                Require(player.PlayerItems.CurrentItem==flashlight&&flashlight.IsEquipped(),"F failed to equip flashlight.");
                Require(beam.Volume.enabled&&beam.VisibleLength>0,"Visible flashlight beam missing.");
                results.Add("PASS: actual F input equips the native flashlight; owned in inventory; visible beam enabled.");
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="Temporary beam test wall";block.layer=11;
                block.transform.position=beam.Source.transform.position+beam.Source.transform.forward*.65f;
                block.transform.rotation=beam.Source.transform.rotation;block.transform.localScale=new Vector3(1,1,.1f);
                Physics.SyncTransforms();beam.SendMessage("LateUpdate");Require(beam.VisibleLength<.7f,"Beam did not stop at close wall.");UnityEngine.Object.Destroy(block);
                results.Add("PASS: beam clips against nearby wall.");
                ScreenCapture.CaptureScreenshot(Log("DiscoveryFlashlightPlay.png"));
                Press(true);step=now;phase=3;return;
            }
            if(phase==3)
            {
                if(now-step<.25)return;Press(false);step=now;phase=4;return;
            }
            if(phase==4)
            {
                if(now-step<2)return;
                Require(!flashlight.ItemObject.activeInHierarchy&&!beam.Volume.enabled,"F failed to stow flashlight/beam.");
                results.Add("PASS: second F stows the model and visible beam.");
                GameManager.Instance.FreezePlayer(true);mover.Open();step=now;phase=5;return;
            }
            if(phase==5)
            {
                if(Vector3.Distance(mover.MovingPart.localPosition,origin)<1)return;
                Require(mover.IsTravelling&&!gate.IsOpen&&gate.ReadyForTravel,"Gate opened in transit.");
                var towardDoor=gate.transform.TransformPoint(gate.EntranceCenter)-player.transform.position;towardDoor.y=0;
                player.PlayerCollider.Move(towardDoor.normalized*3);
                step=now;phase=6;return;
            }
            if(phase==6)
            {
                if(now-step<.4)return;
                Require(!gate.IsOpen&&!gate.Blocked&&gate.ReadyForTravel,"Approaching gate in transit reopened it.");
                float z=gate.transform.InverseTransformPoint(player.transform.position).z;
                Require(z>gate.EntranceCenter.z,"Player crossed closed gate (local z "+z+").");
                results.Add("PASS: approach/exit attempt during descent does not reopen gates; actual player collider remains inside.");
                phase=7;return;
            }
            if(phase==7)
            {
                if(!mover.AtEnd)return;
                step=now;phase=8;return;
            }
            if(phase==8)
            {
                if(now-step<1.5)return;
                Require(!mover.IsTravelling&&gate.IsOpen,"Gate did not open at bottom.");
                Require(Vector3.Distance(mover.MovingPart.localPosition,origin+mover.Travel)<.001f,"Not at bottom stop.");
                results.Add("PASS: gate opens only at the completed bottom stop.");
                Require(errors.Count==0,string.Join("\n",errors));
                Finish("PASS");return;
            }
        }
        catch(Exception e){results.Add(e.ToString());Finish("FAIL");}
    }
    static void Finish(string status)
    {
        phase=99;Press(false);File.WriteAllText(Log("WayfindingPlayCheck.txt"),status+"\n"+string.Join("\n",results)+"\n"+string.Join("\n",errors));EditorApplication.isPlaying=false;
    }
}
