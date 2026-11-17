using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using DeadAir.Playtesting;
public static class FacilityIterationApply {
 const int Mask=~((1<<3)|(1<<5)|(1<<7)|(1<<13));
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No geometry "+g.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static bool Clear(Vector3 a,Vector3 b,float radius=.2f)=>!Physics.SphereCast(a,radius,(b-a).normalized,out _,Vector3.Distance(a,b),Mask,QueryTriggerInteraction.Ignore);
 public static List<GoblinSurfacePatrol.SurfacePoint> Route(Vector3 origin,float clearance=.5f){
 foreach(var offset in Enumerable.Range(-3,7).SelectMany(x=>Enumerable.Range(-3,7).Select(z=>new Vector2Int(x,z))).OrderBy(v=>v.sqrMagnitude)){int x=offset.x,z=offset.y;
 Vector3 seed=origin+new Vector3(x,2,z);
 if(!Physics.Raycast(seed,Vector3.down,out var floor,5,Mask,QueryTriggerInteraction.Ignore)||floor.normal.y<.9f)continue;
 Vector3 start=floor.point+Vector3.up*(clearance+.05f);
 if(Physics.CheckSphere(start,clearance,Mask,QueryTriggerInteraction.Ignore))continue;
 foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){
 if(!Physics.Raycast(start,dir,out var wall,5,Mask,QueryTriggerInteraction.Ignore)||Mathf.Abs(wall.normal.y)>.1f)continue;
 Vector3 low=wall.point+wall.normal*(clearance+.05f);
 if(!Physics.Raycast(low+Vector3.up*.2f,Vector3.up,out var roof,7,Mask,QueryTriggerInteraction.Ignore)||roof.normal.y>-.8f)continue;
 Vector3 high=roof.point+Vector3.down*(clearance+.05f),over=high+wall.normal*1.5f;
 if(high.y-low.y<.6f||!Clear(start,low,clearance)||!Clear(low,high,clearance)||!Clear(high,over,clearance))continue;
 return new List<GoblinSurfacePatrol.SurfacePoint>{
 new GoblinSurfacePatrol.SurfacePoint{Position=start,Normal=Vector3.up},
 new GoblinSurfacePatrol.SurfacePoint{Position=low,Normal=wall.normal},
 new GoblinSurfacePatrol.SurfacePoint{Position=high,Normal=wall.normal},
 new GoblinSurfacePatrol.SurfacePoint{Position=over,Normal=Vector3.down}};
 }}
 return null;
 }
 [MenuItem("Tools/Dead Air/Apply Mail Clock Cameras And Goblin")]
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 var previous=SceneManager.GetActiveScene();var report=new List<string>();
 foreach(string name in new[]{"Security Terminal Trial","AI Discovery Trial"}){
 var scene=SceneManager.GetSceneByPath("Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity",OpenSceneMode.Additive);
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var before=all.ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf));
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeMailClockGoblin-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 Physics.SyncTransforms();
 if(name.StartsWith("Security")){
 var clock=all.FirstOrDefault(t=>t.name=="Clock");
 if(clock&&!clock.GetComponent<FacilityClock>()){
 report.AddRange(clock.GetComponentsInChildren<Renderer>().Select(r=>"Clock part "+r.name+" bounds="+r.bounds.size+" local="+r.transform.localPosition+" rotation="+r.transform.localEulerAngles));
 var driver=Undo.AddComponent<FacilityClock>(clock.gameObject);driver.Axis=Vector3.up;driver.Direction=-1;
 // The modeled clock lies in its local XZ plane; new pivots keep imported mesh pivots untouched.
 var center=clock.Find("Clock Base");Vector3 pivot=center?center.localPosition:Vector3.zero;
 foreach(int hand in new[]{0,1,2}){
 var p=new GameObject(hand==0?"Working Hour Hand":hand==1?"Working Minute Hand":"Working Second Hand");p.transform.SetParent(clock,false);p.transform.localPosition=pivot+Vector3.up*.065f;
 var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Hand mesh";cube.transform.SetParent(p.transform,false);UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
 float length=hand==0?.85f:hand==1?1.2f:1.35f;cube.transform.localPosition=new Vector3(0,0,length*.5f);cube.transform.localScale=new Vector3(hand==2?.025f:.06f,.03f,length);
 var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=hand==2?Color.red:Color.black;
 string mp="Assets/DeadAir/PlaytestHub/Resources/ClockHand"+hand+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!saved){AssetDatabase.CreateAsset(m,mp);saved=m;}else UnityEngine.Object.DestroyImmediate(m);cube.GetComponent<Renderer>().sharedMaterial=saved;
 if(hand==0)driver.HourHand=p.transform;else if(hand==1)driver.MinuteHand=p.transform;else driver.SecondHand=p.transform;
 }
 foreach(var t in clock.GetComponentsInChildren<Transform>().Where(t=>t.name=="Second Hand"||t.name=="Cylinder"||t.name=="Cylinder.001")){var r=t.GetComponent<Renderer>();if(r){Undo.RecordObject(r,"Replace static clock hand");r.enabled=false;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
 report.Add("Clock: three working hands added; original static hand renderers hidden.");
 }
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/SecurityCamera.prefab");
 var terminal=all.Select(t=>t.GetComponent<SecurityTrialTerminal>()).FirstOrDefault(t=>t);
 int count=0;var added=new List<Camera>();var gates=new List<SecurityPanelDoor>();
 var sources=all.Where(t=>t.name.StartsWith("SecurityCamera")&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)=="Assets/DeadAir/SecurityCamera.prefab").ToArray();
 foreach(var door in all.Select(t=>t.GetComponent<PrototypeMover>()).Where(m=>m&&(m.name.StartsWith("Door -")||m.name=="TRANSIT DOOR"))){
 if(sources.Any(s=>Vector3.Distance(s.position,door.transform.position)<5)||scene.GetRootGameObjects().Any(g=>g.name=="Facility Camera - "+door.name))continue;
 foreach(var offset in new[]{new Vector3(3,2.7f,-3),new Vector3(-3,2.7f,-3),new Vector3(3,2.7f,3),new Vector3(-3,2.7f,3)}){
 var pos=door.transform.position+offset;
 // Place a wall-mounted unit on an actual wall, facing back into the room.
 var horizontal=(door.transform.position-pos);horizontal.y=0;horizontal.Normalize();
 if(!Physics.Raycast(pos,-horizontal,out var mount,5,Mask,QueryTriggerInteraction.Ignore)||Mathf.Abs(mount.normal.y)>.2f)continue;
 pos=mount.point+mount.normal*.45f;
 var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);g.name="Facility Camera - "+door.name;g.transform.position=pos;g.transform.rotation=Quaternion.LookRotation(mount.normal);
 var bounds=BoundsOf(g);bool blocked=Physics.OverlapBox(bounds.center,bounds.extents,Quaternion.identity,Mask,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(g.transform));
 if(blocked){UnityEngine.Object.DestroyImmediate(g);continue;}
 var view=new GameObject("LIVE - "+door.name).AddComponent<Camera>();view.transform.SetParent(g.transform,false);view.transform.position=pos+mount.normal*.2f;view.transform.LookAt(door.MovingPart.position+Vector3.up*1.2f);view.enabled=false;view.fieldOfView=75;view.nearClipPlane=.08f;
 var guard=door.GetComponent<SecurityPanelDoor>();if(!guard)guard=Undo.AddComponent<SecurityPanelDoor>(door.gameObject);guard.Mover=door;guard.DoorLabel=door.name;EditorUtility.SetDirty(guard);
 if(PrefabUtility.IsPartOfPrefabInstance(guard))PrefabUtility.RecordPrefabInstancePropertyModifications(guard);
 added.Add(view);gates.Add(guard);Undo.RegisterCreatedObjectUndo(g,"Add facility camera");count++;report.Add("Camera: "+door.name+" at "+pos+" clearance passed.");break;
 }}
 if(terminal&&added.Count>0){Undo.RecordObject(terminal,"Add facility feeds");var prior=terminal.CameraFeeds.Length;terminal.CameraFeeds=terminal.CameraFeeds.Concat(added).ToArray();var mappings=new SecurityPanelDoor[prior+gates.Count];Array.Copy(terminal.FeedDoors,mappings,Math.Min(prior,terminal.FeedDoors.Length));for(int i=0;i<gates.Count;i++)mappings[prior+i]=gates[i];terminal.FeedDoors=mappings;EditorUtility.SetDirty(terminal);PrefabUtility.RecordPrefabInstancePropertyModifications(terminal);}
 report.Add("Security cameras: placed model already matches Assets/DeadAir/SecurityCamera.prefab. Added "+count+" wall-clear units.");
 }else{
 var slot=all.FirstOrDefault(t=>t.name.StartsWith("ENEMY SLOT"));
 if(slot&&!slot.GetComponentInChildren<GoblinSurfacePatrol>()){
 var route=Route(slot.position);if(route==null)throw new Exception("No continuous floor-wall-ceiling route found near AI slot; scene not saved.");
 var root=new GameObject("GOBLIN - Floor Wall Ceiling Patrol");SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetParent(slot,false);root.transform.position=route[0].Position;
 var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/Goblin.fbx"),root.transform);var b=BoundsOf(model);model.transform.localScale*=.65f/Mathf.Max(.01f,b.size.y);b=BoundsOf(model);model.transform.position+=root.transform.position-b.center;
 foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
 var patrol=root.AddComponent<GoblinSurfacePatrol>();patrol.Visual=model.transform;patrol.Route=route.ToArray();patrol.ScareSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThunderWire Studio/UHFPS/Content/Sounds/Interact/Lever/Lever_On_1.wav");
 Undo.RegisterCreatedObjectUndo(root,"Add goblin patrol");report.Add("Goblin: connected "+route.Count+" floor / wall / ceiling points near "+slot.position+"; .34m swept clearance passed.");
 }}
 foreach(var pair in before){var t=pair.Key;var b=pair.Value;if(!t||t.parent!=b.Item1||t.localPosition!=b.Item2||t.localRotation!=b.Item3||t.localScale!=b.Item4||t.gameObject.activeSelf!=b.Item5)throw new Exception("Existing placement changed.");}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(name+": saved, existing transforms preserved.");
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/FacilityMailClockGoblinResult.txt",report);
 }
}


