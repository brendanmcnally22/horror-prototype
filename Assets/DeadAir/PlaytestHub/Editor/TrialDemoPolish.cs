using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

public static class TrialDemoPolish {
 static readonly string[] Trials={"AI Discovery Trial","Security Terminal Trial","Three Doors Lighting Trial"};
 static GameObject Rail(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool visible=true){
 var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=11;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().enabled=visible;return g;}
 static List<GoblinSurfacePatrol.SurfacePoint> FloorRoute(Vector3 origin){
 const int mask=~((1<<3)|(1<<5)|(1<<7)|(1<<13));
 foreach(var offset in Enumerable.Range(-8,17).SelectMany(x=>Enumerable.Range(-8,17).Select(z=>new Vector3(x,0,z))).OrderBy(v=>v.sqrMagnitude)){
 if(!Physics.Raycast(origin+offset+Vector3.up*2,Vector3.down,out var floor,5,mask,QueryTriggerInteraction.Ignore)||floor.normal.y<.9f)continue;
 Vector3 start=floor.point+Vector3.up*.85f;if(Physics.CheckSphere(start,.8f,mask,QueryTriggerInteraction.Ignore))continue;
 foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){
 Vector3 end=start+dir*2;if(Physics.SphereCast(start,.8f,dir,out _,2,mask,QueryTriggerInteraction.Ignore))continue;
 bool supported=true;for(int i=1;i<=8;i++)if(!Physics.Raycast(Vector3.Lerp(start,end,i/8f),Vector3.down,out var f,1.1f,mask,QueryTriggerInteraction.Ignore)||f.normal.y<.9f||Mathf.Abs(f.point.y-floor.point.y)>.15f){supported=false;break;}
 if(supported)return new List<GoblinSurfacePatrol.SurfacePoint>{new GoblinSurfacePatrol.SurfacePoint{Position=start,Normal=Vector3.up},new GoblinSurfacePatrol.SurfacePoint{Position=end,Normal=Vector3.up}};
 }}return null;
 }
 [MenuItem("Tools/Dead Air/Inspect Platform Lever Assignments")]
 static void Levers(){
 var path="Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity";var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;var previous=SceneManager.GetActiveScene();if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 File.WriteAllLines(Application.dataPath+"/../Logs/PlatformLeverAssignments.txt",all.Select(t=>t.GetComponent<PrototypeLever>()).Where(l=>l&&l.Targets.Any(m=>m&&m.MovingPart&&(m.name.Contains("Platform",StringComparison.OrdinalIgnoreCase)||m.name=="STARTING ELEVATOR"))).Select(l=>l.name+" label="+l.Label+" world="+l.transform.position+" parent="+l.transform.parent?.name+" targets="+string.Join(",",l.Targets.Where(m=>m).Select(m=>m.name+" onboard="+l.transform.IsChildOf(m.MovingPart)+" local="+m.MovingPart.InverseTransformPoint(l.transform.position)))));
 }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(s,true);}
 }
 [MenuItem("Tools/Dead Air/Apply Trial Demo Polish")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
 var previous=SceneManager.GetActiveScene();var result=new List<string>();
 string matPath="Assets/DeadAir/PlaytestHub/Materials/Platform safety railing.mat";
 var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
 if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.65f,.39f,.06f);material.SetFloat("_Metallic",.5f);AssetDatabase.CreateAsset(material,matPath);}
 foreach(string name in Trials){
 string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";
 var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeDemoPolish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 foreach(var pc in all.Select(t=>t.GetComponent<VirgoTerminal>()).Where(pc=>pc)){
 Undo.RecordObject(pc,"Assign app icons");pc.AppIcons=new[]{"explorer","email","bunny_break","Googus","Security App"}.Select(n=>AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DeadAir/PlaytestHub/"+n+".png")).ToArray();EditorUtility.SetDirty(pc);if(PrefabUtility.IsPartOfPrefabInstance(pc))PrefabUtility.RecordPrefabInstancePropertyModifications(pc);
 }
 foreach(var beam in all.Select(t=>t.GetComponent<FacilityFlashlightBeam>()).Where(c=>c)){Undo.RecordObject(beam,"Remove flashlight haze");beam.enabled=false;if(beam.Volume){Undo.RecordObject(beam.Volume,"Remove haze volume");beam.Volume.enabled=false;EditorUtility.SetDirty(beam.Volume);if(PrefabUtility.IsPartOfPrefabInstance(beam.Volume))PrefabUtility.RecordPrefabInstancePropertyModifications(beam.Volume);}EditorUtility.SetDirty(beam);if(PrefabUtility.IsPartOfPrefabInstance(beam))PrefabUtility.RecordPrefabInstancePropertyModifications(beam);}
 string marker="DEMO POLISH - Applied";if(!all.Any(t=>t.name==marker)){
 foreach(var light in all.Select(t=>t.GetComponent<Light>()).Where(l=>l&&!l.GetComponentInParent<UHFPS.Runtime.PlayerManager>())){
 Undo.RecordObject(light,"Slightly darker facility");light.intensity*=.8f;EditorUtility.SetDirty(light);if(PrefabUtility.IsPartOfPrefabInstance(light))PrefabUtility.RecordPrefabInstancePropertyModifications(light);
 }
 RenderSettings.ambientLight*=.8f;RenderSettings.ambientIntensity*=.8f;
 var flag=new GameObject(marker);SceneManager.MoveGameObjectToScene(flag,scene);Undo.RegisterCreatedObjectUndo(flag,"Track darkness adjustment");
 }
 int platforms=0,gates=0;
 if(name.StartsWith("AI")){
 foreach(var mover in all.Select(t=>t.GetComponent<PrototypeMover>()).Where(m=>m&&m.MovingPart&&(m.name.Contains("Platform",StringComparison.OrdinalIgnoreCase)||m.name=="STARTING ELEVATOR"))){
 if(mover.MovingPart.Find("CONFIGURABLE FOUR SIDE SAFETY"))continue;
 var deck=mover.MovingPart.GetComponentsInChildren<BoxCollider>(true).Where(b=>!b.isTrigger&&b.enabled).OrderByDescending(b=>b.bounds.size.x*b.bounds.size.z).FirstOrDefault();
 if(!deck){result.Add("No deck box on "+mover.name);continue;}
 var corners=new List<Vector3>();foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})corners.Add(mover.MovingPart.InverseTransformPoint(deck.transform.TransformPoint(deck.center+Vector3.Scale(deck.size*.5f,new Vector3(x,y,z)))));
 var min=corners.Aggregate(Vector3.Min);var max=corners.Aggregate(Vector3.Max);
 var size=max-min;var center=(min+max)*.5f;
 if(size.x<.5f||size.z<.5f){result.Add("Skipped non-deck bounds "+mover.name);continue;}
 var root=new GameObject("CONFIGURABLE FOUR SIDE SAFETY");root.transform.SetParent(mover.MovingPart,false);Undo.RegisterCreatedObjectUndo(root,"Add platform safety rails");
 for(int side=0;side<4;side++){
 // Existing starting-elevator gate already secures its front.
 if(side==0&&mover.name=="STARTING ELEVATOR"&&mover.GetComponentInChildren<PlatformSafetyGate>())continue;
 bool alongX=side<2;float width=alongX?size.x:size.z;
 var gateRoot=new GameObject(new[]{"Entrance - Front (-Z)","Entrance - Rear (+Z)","Entrance - Left (-X)","Entrance - Right (+X)"}[side]);gateRoot.transform.SetParent(root.transform,false);
 gateRoot.transform.localPosition=new Vector3(alongX?center.x:(side==2?min.x:max.x),max.y,alongX?(side==0?min.z:max.z):center.z);
 gateRoot.transform.localRotation=Quaternion.Euler(0,alongX?0:90,0);
 var gate=gateRoot.AddComponent<PlatformSafetyGate>();gate.Mover=mover;gate.OpenAtStart=side==0;gate.OpenAtEnd=side==0;gate.LiftOpen=true;gate.SlideDistance=1.25f;gate.DoorSpeed=2;
 gate.EntranceCenter=new Vector3(0,.6f,0);gate.EntranceSize=new Vector3(width,1.2f,.6f);
 gate.LeftClosed=new Vector3(-width/4,.55f,0);gate.RightClosed=new Vector3(width/4,.55f,0);
 foreach(bool left in new[]{true,false}){
 var leaf=Rail(gateRoot.transform,left?"Left gate leaf":"Right gate leaf",left?gate.LeftClosed:gate.RightClosed,new Vector3(width/2,1.1f,.075f),material,false);
 var visual=new GameObject("Rail bars");visual.transform.SetParent(gateRoot.transform,false);visual.transform.SetParent(leaf.transform,false);
 // Bars expressed in normalized coordinates of the leaf collider.
 foreach(float height in new[]{-.35f,.05f,.45f})Rail(visual.transform,"Horizontal rail",new Vector3(0,height,0),new Vector3(1,.065f,1),material).GetComponent<Collider>().enabled=false;
 foreach(float x in new[]{-.46f,0,.46f})Rail(visual.transform,"Upright",new Vector3(x,0,0),new Vector3(.035f,1,1),material).GetComponent<Collider>().enabled=false;
 if(left)gate.LeftLeaf=leaf.transform;else gate.RightLeaf=leaf.transform;
 }gates++;
 }platforms++;
 }
 var existing=all.Select(t=>t.GetComponent<GoblinSurfacePatrol>()).FirstOrDefault(g=>g);
 if(!existing){
 var crates=all.FirstOrDefault(t=>t.name=="Debris - Scattered Crates");
 if(crates){
 Physics.SyncTransforms();var route=FacilityIterationApply.Route(crates.position,.85f)??FloorRoute(crates.position);
 if(route!=null){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/PlaytestHub/Prefabs/Goblin Surface Enemy - 1.6m.prefab");var gob=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);gob.name="GOBLIN - AI Discovery Scare Patrol";gob.transform.SetPositionAndRotation(route[0].Position,Quaternion.identity);var ai=gob.GetComponent<GoblinSurfacePatrol>();ai.Route=route.ToArray();ai.CollisionRadius=.8f;ai.ScareSound=null;EditorUtility.SetDirty(ai);PrefabUtility.RecordPrefabInstancePropertyModifications(ai);Undo.RegisterCreatedObjectUndo(gob,"Add AI Discovery enemy");result.Add("AI Discovery: 1.6m goblin scare enemy placed by crate route.");}else result.Add("AI goblin route unavailable.");
 }}
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);result.Add(name+": icons, haze removal, darkness saved; "+platforms+" platforms, "+gates+" new configurable gates.");
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/TrialDemoPolish.txt",result);
 }
}


