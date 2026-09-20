using System.Collections;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UHFPS.Runtime;

namespace Blockout.Progression
{
    public class FacilityProgression : MonoBehaviour, IDynamicUnlock, ISaveable
    {
        public const string ObjectiveKey="facility_restore_power";
        public const string SubKey="restore_four_fuses";
        public FuseboxPuzzle Fusebox;
        public DynamicObject ObservationDoor;
        public HintTrigger DoorHint;
        public ItemProperty PatientKeycard=new ItemProperty();
        public ObjectiveManager Objectives;
        public TMP_Text TopObjective;
        public GameObject WinPanel;
        public Light[] RoomLights;
        public MeshRenderer[] Diffusers;
        public Color EmergencyColor=new Color(.85f,.075f,.075f);
        public Color RestoredColor=Color.white;
        [TextArea] public string PowerObjective="RESTORE FULL POWER TO THE EXPERIMENT ROOM";
        [TextArea] public string NoPowerHint="The observation door has no power. Restore full power to the experiment room using the downstairs fusebox.";
        [TextArea] public string NoKeycardHint="Power is restored. Check the patient's body on the table in the experiment room for the observation keycard.";
        [TextArea] public string PowerRestoredHint="Full power restored. Return to the experiment room and use the observation door.";
        public float HintDuration=7f;
        public bool ObjectiveStarted {get;private set;}
        public bool Won {get;private set;}
        bool objectiveCompleted;
        bool unlocked;
        bool showingWin;
        float priorTimeScale=1f;
        MaterialPropertyBlock emission;
        public bool PowerRestored {
            get { if(!Fusebox || Fusebox.Fuses.Count!=4)return false;foreach(var fuse in Fusebox.Fuses)if(!fuse.IsInserted)return false;return true; }
        }
        public bool HasKeycard=>Inventory.HasReference && PatientKeycard.InInventory;
        public static string AccessState(bool power,bool keycard)=>!power?"power":!keycard?"keycard":"granted";
        IEnumerator Start() {yield return null;Reconcile();}
        void Reconcile()
        {
            ApplyLightState(PowerRestored);
            if(ObservationDoor)ObservationDoor.SetLockedStatus(!(unlocked && PowerRestored));
            if(PowerRestored && ObjectiveStarted && !objectiveCompleted)CompleteObjective();
            UpdateObjectiveText();
            if(Won)ShowWin();
        }
        public void OnFuseboxOpened()
        {
            if(ObjectiveStarted || Won)return;
            ObjectiveStarted=true;
            Objectives.AddObjective(ObjectiveKey,SubKey);
            if(PowerRestored)CompleteObjective();
            UpdateObjectiveText();
        }
        public void OnPowerRestored()
        {
            if(!PowerRestored)return;
            if(!ObjectiveStarted)OnFuseboxOpened();
            CompleteObjective();ApplyLightState(true);UpdateObjectiveText();
            GameManager.Instance.ShowHintMessage(HasKeycard?PowerRestoredHint:PowerRestoredHint+" "+NoKeycardHint,HintDuration);
        }
        void CompleteObjective()
        {
            if(objectiveCompleted)return;
            Objectives.CompleteObjective(ObjectiveKey,SubKey);objectiveCompleted=true;
        }
        void UpdateObjectiveText()
        {
            if(!TopObjective)return;
            TopObjective.gameObject.SetActive(ObjectiveStarted && !Won);
            TopObjective.text=PowerRestored?"RETURN TO THE EXPERIMENT ROOM — ENTER OBSERVATION":PowerObjective;
        }
        public void OnTryUnlock(DynamicObject door)
        {
            if(door!=ObservationDoor)return;
            switch(AccessState(PowerRestored,HasKeycard)) {
                case "power":SetHint(NoPowerHint);door.TryUnlockResult(false);break;
                case "keycard":SetHint(NoKeycardHint);door.TryUnlockResult(false);break;
                default:unlocked=true;door.TryUnlockResult(true);door.SetOpenState();break;
            }
        }
        void SetHint(string text) {DoorHint.HintMessage.NormalText=text;DoorHint.HintMessage.GlocText="*"+text;DoorHint.MessageTime=HintDuration;}
        public void ApplyLightState(bool restored)
        {
            Color color=restored?RestoredColor:EmergencyColor;
            foreach(var light in RoomLights)if(light)light.color=color;
            emission??=new MaterialPropertyBlock();
            foreach(var renderer in Diffusers)if(renderer) {
                renderer.GetPropertyBlock(emission);emission.SetColor("_EmissionColor",color*2f);emission.SetColor("_BaseColor",color);renderer.SetPropertyBlock(emission);
            }
        }
        public void CrossObservationThreshold()
        {
            if(Won || !PowerRestored || !HasKeycard || ObservationDoor.IsLocked || !ObservationDoor.IsOpened)return;
            Won=true;UpdateObjectiveText();ShowWin();
        }
        void ShowWin()
        {
            if(showingWin)return;
            showingWin=true;priorTimeScale=Time.timeScale;
            if(WinPanel)WinPanel.SetActive(true);
            GameManager.Instance.FreezePlayer(true);
            Time.timeScale=0f;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void ContinueExploring()
        {
            if(!showingWin)return;
            showingWin=false;if(WinPanel)WinPanel.SetActive(false);
            Time.timeScale=priorTimeScale;GameManager.Instance.FreezePlayer(false);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }
        void OnDestroy(){if(showingWin)Time.timeScale=priorTimeScale;}
        public StorableCollection OnSave()=>new StorableCollection{{"objectiveStarted",ObjectiveStarted},{"objectiveCompleted",objectiveCompleted},{"unlocked",unlocked},{"won",Won}};
        public void OnLoad(JToken data)
        {
            ObjectiveStarted=(bool?)data["objectiveStarted"]??false;objectiveCompleted=(bool?)data["objectiveCompleted"]??false;
            unlocked=(bool?)data["unlocked"]??false;Won=(bool?)data["won"]??false;StartCoroutine(AfterLoad());
        }
        IEnumerator AfterLoad(){yield return null;Reconcile();}
    }
}
