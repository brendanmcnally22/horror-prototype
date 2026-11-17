using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;
using UHFPS.Runtime;
public static class LiveSecurityCameraRead {
 [MenuItem("Tools/Dead Air/Read Current Security View")]
 public static void Read(){
 var lines=new List<string>{"Playing="+EditorApplication.isPlaying+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
 foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var brain=c.GetComponent<CinemachineBrain>();
 lines.Add("CAM "+c.name+" active="+c.gameObject.activeInHierarchy+" enabled="+c.enabled+" depth="+c.depth+" at="+c.transform.position+" rot="+c.transform.eulerAngles+" target="+(c.targetTexture?c.targetTexture.name:"SCREEN")+" brain="+(brain&&brain.ActiveVirtualCamera!=null?brain.ActiveVirtualCamera.Name:"none")+" parent="+c.transform.parent?.name);
 }
 foreach(var c in UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
 lines.Add("VCAM "+c.name+" enabled="+c.enabled+" active="+c.gameObject.activeInHierarchy+" priority="+c.Priority+" at="+c.transform.position+" parent="+c.transform.parent?.name+" scene="+c.gameObject.scene.name);
 foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))
 lines.Add("PLAYER "+p.name+" active="+p.gameObject.activeInHierarchy+" at="+p.transform.position+" Main="+p.MainCamera?.name+" VCam="+p.MainVirtualCamera?.name+" state="+p.PlayerStateMachine?.CurrentStateKey+" parent="+p.transform.parent?.name);
 File.WriteAllLines(Application.dataPath+"/../Logs/LiveSecurityView.txt",lines);
 }
}
