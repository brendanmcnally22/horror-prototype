#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UHFPS.Runtime;
using Blockout.Optimization;

public sealed class AreaControlsSmoke : MonoBehaviour
{
    public GameObject TriggerPrefab;
    int checks;
    string Report => Application.dataPath+"/../Logs/AreaControlsTest.txt";
    void Check(bool condition,string message) { if(!condition)throw new Exception(message); checks++; }
    IEnumerator Start()
    {
        var test=RunChecks();
        while(true)
        {
            bool next;
            try { next=test.MoveNext(); }
            catch(Exception e) { File.WriteAllText(Report,"FAIL after "+checks+" checks\n"+e); break; }
            if(!next) { File.WriteAllText(Report,"PASS: "+checks+" checks in isolated Play Mode scene. Startup disabling, non-player rejection, physical player entry, repeat entry, null lists entries, conflicting targets, protected objects and one-shot behavior. Working Blockout scene was not run.\n"); break; }
            yield return test.Current;
        }
        EditorApplication.isPlaying=false;
    }
    IEnumerator RunChecks()
    {
        var target=new GameObject("TEST target to enable");
        var startup=new GameObject("TEST startup host"); startup.SetActive(false);
        var disable=startup.AddComponent<DisableObjectsOnStartup>(); disable.ObjectsToDisable=new[]{target,null,startup};
        startup.SetActive(true);
        Check(!target.activeSelf && startup.activeSelf,"Startup disabling / self protection failed.");
        var off=new GameObject("TEST target to disable");
        var trigger=Instantiate(TriggerPrefab).GetComponent<AreaActivationTrigger>();
        trigger.EnableOnEnter=new[]{target,null}; trigger.DisableOnEnter=new[]{off,null,trigger.gameObject};
        var npc=GameObject.CreatePrimitive(PrimitiveType.Sphere); npc.name="TEST non-player"; npc.transform.position=new Vector3(-5,0,0);
        var body=npc.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
        yield return new WaitForFixedUpdate(); body.position=Vector3.zero;
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Check(!target.activeSelf && off.activeSelf,"Non-player activated trigger.");
        body.position=new Vector3(-5,0,5);
        var player=new GameObject("TEST player"); player.SetActive(false); player.transform.position=new Vector3(-5,1,0);
        player.AddComponent<PlayerManager>().enabled=false;
        var controller=player.AddComponent<CharacterController>(); player.SetActive(true);
        trigger.DisableOnEnter=new[]{off,null,trigger.gameObject,player};
        yield return new WaitForFixedUpdate(); controller.Move(new Vector3(5,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Check(target.activeSelf && !off.activeSelf,"Player physical entry did not apply targets.");
        Check(player.activeSelf && trigger.gameObject.activeSelf,"Protected player/trigger disabled.");
        controller.Move(new Vector3(-5,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        target.SetActive(false); off.SetActive(true);
        trigger.DisableOnEnter=new[]{target,off};
        controller.Move(new Vector3(5,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Check(target.activeSelf && !off.activeSelf,"Re-entry or enable-wins conflict rule failed.");
        var once=Instantiate(TriggerPrefab,new Vector3(15,0,0),Quaternion.identity).GetComponent<AreaActivationTrigger>();
        once.OneShot=true; once.EnableOnEnter=new[]{off};
        controller.Move(new Vector3(15,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Check(off.activeSelf,"First one-shot entry failed.");
        controller.Move(new Vector3(5,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        off.SetActive(false); controller.Move(new Vector3(-5,0,0));
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Check(!off.activeSelf,"One-shot trigger fired twice.");
    }
}
#endif
