using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UHFPS.Runtime;
using Blockout.Discovery;

[InitializeOnLoad]
public static class FacilityCartCheck
{
    const string Key="FacilityCartCheck.Running";
    static double started, step;
    static int phase;
    static FacilityCartHandle handle;
    static PlayerStateMachine player;
    static FacilityCartCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;phase=0;Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);var path=SessionState.GetString(Key+".Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);}
        };
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))
        {
            if(!File.Exists("Logs/FacilityCartCheck.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/FacilityCartCheck.request");
            SessionState.SetString(Key+".Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene=null;
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||started==0||phase==9)return;
        try
        {
            if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Cart check timed out.");
            if(phase==0)
            {
                if(EditorApplication.timeSinceStartup-started<6)return;
                player=PlayerManager.Instance.PlayerStateMachine;
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=11;floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(20,1,20);
                var cart=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/Facility Props/Supply Cart - HOLD TO MOVE.prefab"));
                cart.transform.position=new Vector3(1000,0,1000);handle=cart.GetComponentInChildren<FacilityCartHandle>();handle.HoldToMove=false;
                var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(1000,.1f,998.75f);cc.enabled=true;
                Physics.SyncTransforms();
                var state=handle.OnStateInteract();Require(state!=null,"Cart grip area blocked unexpectedly.");
                player.ChangeState(state.stateKey,state.stateData);step=EditorApplication.timeSinceStartup;phase=1;return;
            }
            if(phase==1)
            {
                if(EditorApplication.timeSinceStartup-step<2)return;
                Require(handle.IsHeld&&player.IsCurrent(PlayerStateMachine.PUSHING_STATE),"Player did not enter cart pushing state.");
                var state=player.CurrentState.fsmState;
                var canMove=state.GetType().GetMethod("CanMove",BindingFlags.Instance|BindingFlags.NonPublic);
                Require((bool)canMove.Invoke(state,new object[]{Vector3.forward}),"Cart cannot move in clear space.");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=11;wall.transform.position=handle.Movement.RootMovable.position;Physics.SyncTransforms();
                Require(!(bool)canMove.Invoke(state,new object[]{Vector3.forward}),"Cart did not detect solid obstruction.");
                UnityEngine.Object.Destroy(wall);
                handle.HoldToMove=true;step=EditorApplication.timeSinceStartup;phase=2;return;
            }
            if(phase==2)
            {
                if(EditorApplication.timeSinceStartup-step<.5)return;
                Require(!handle.IsHeld&&!player.IsCurrent(PlayerStateMachine.PUSHING_STATE),"Releasing Use did not exit pushing state.");
                Finish("PASS: native pushing state entered; clear path allowed; solid obstacle blocked; releasing Use returned player control. Runtime-only objects removed on leaving Play mode.\n");
            }
        }
        catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Finish(string result){phase=9;File.WriteAllText("Logs/FacilityCartCheck.txt",result);EditorApplication.isPlaying=false;}
}
