using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeadAir.Playtesting
{
    public partial class PlaytestHub
    {
        GUIStyle hubTitle,hubBody,hubButton,hubSmall,hubField;
        List<HubRun> book=new List<HubRun>();int bookIndex,filter;
        Vector2 bookScroll;
        string exportMessage="";
        Texture2D buttonSurface,hoverSurface,selectedSurface,fieldSurface;
        public void RefreshBook()
        {
            book=HubRunStore.List();
            if(filter>0&&Config&&filter<=Config.Experiences.Length)book=book.Where(r=>r.experienceId==Config.Experiences[filter-1].Id).ToList();
            bookIndex=Mathf.Clamp(bookIndex,0,Mathf.Max(0,book.Count-1));
        }
        public static string Duration(double seconds)=>TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(seconds>=3600?@"hh\:mm\:ss":@"mm\:ss");
        void Styles()
        {
            if(hubBody!=null)return;
            hubBody=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true,richText=false};hubBody.normal.textColor=new Color(.94f,.91f,.85f);
            hubTitle=new GUIStyle(hubBody){fontSize=30,fontStyle=FontStyle.Bold};
            hubSmall=new GUIStyle(hubBody){fontSize=14};
            hubButton=new GUIStyle(GUI.skin.button){fontSize=18,wordWrap=true,padding=new RectOffset(15,15,9,9)};
            hubField=new GUIStyle(GUI.skin.textArea){fontSize=16,wordWrap=true,richText=false};
            buttonSurface=Surface(new Color(.20f,.19f,.18f));hoverSurface=Surface(new Color(.32f,.25f,.19f));selectedSurface=Surface(new Color(.57f,.31f,.13f));fieldSurface=Surface(new Color(.13f,.125f,.12f));
            foreach(var state in new[]{hubButton.normal,hubButton.hover,hubButton.active,hubButton.focused,hubButton.onNormal,hubButton.onHover,hubButton.onActive,hubButton.onFocused})state.textColor=new Color(.97f,.94f,.88f);
            hubButton.normal.background=buttonSurface;hubButton.hover.background=hoverSurface;hubButton.active.background=selectedSurface;hubButton.focused.background=hoverSurface;
            hubButton.onNormal.background=selectedSurface;hubButton.onHover.background=selectedSurface;hubButton.onActive.background=selectedSurface;hubButton.onFocused.background=selectedSurface;
            foreach(var state in new[]{hubField.normal,hubField.hover,hubField.active,hubField.focused}){state.background=fieldSurface;state.textColor=new Color(.96f,.93f,.87f);}
        }
        static Texture2D Surface(Color color){var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();return texture;}
        void OnGUI()
        {
            if(!Config||!Experience)return;Styles();
            if(Experience.Page==HubPage.Experience&&!SwitcherOpen&&!loading)
            {
                var text=Experience.Objective+"\n[ — finish level";
                if(Definition!=null&&Definition.Kind==HubExperienceKind.Security&&CurrentRun!=null)text="Spot enemies on 3 different cameras: "+(CurrentRun.scoutCameraIds?.Count??0)+" / 3\n[ — finish level\nTerminal operation: "+Duration(CurrentRun.terminalSeconds);
                
                GUI.Label(new Rect(18,Screen.height-105,Mathf.Min(600,Screen.width-36),100),text,hubSmall);return;
            }
            GUI.depth=-100;
            var previous=GUI.matrix;
            var oldColor=GUI.color;GUI.color=new Color(.075f,.073f,.07f,1);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=oldColor;
            float scale=Mathf.Min(Screen.width/960f,Screen.height/640f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)/2,(Screen.height-640*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUILayout.BeginArea(new Rect(36,25,888,590));
            GUILayout.Label("D E A D   A I R     /     FIELD TESTS",hubSmall);
            if(loading)GUILayout.Label("Loading experience…",hubTitle);
            else if(Experience.Page==HubPage.Feedback)DrawFeedback();
            else if(Experience.Page==HubPage.Results)DrawBook();
            else DrawMenu();
            if(!string.IsNullOrEmpty(Message))GUILayout.Label(Message,hubSmall);
            if(!string.IsNullOrEmpty(HubRunStore.LastError))GUILayout.Label(HubRunStore.LastError,hubSmall);
            GUILayout.EndArea();GUI.matrix=previous;
        }
        bool Action(string text,Action click,float height=43)
        {
            if(!GUILayout.Button(text,hubButton,GUILayout.Height(height)))return false;click();return true;
        }
        void DrawMenu()
        {
            GUILayout.Label(SwitcherOpen?"Change your test":"Pick your next test",hubTitle);
            GUILayout.Label("Three focused experiences. Jump between them whenever you need.",hubSmall);GUILayout.Space(18);
            GUILayout.BeginHorizontal();
            for(int i=0;i<Config.Experiences.Length;i++)
            {
                var entry=Config.Experiences[i];GUILayout.BeginVertical(GUILayout.Width(280));
                GUILayout.Label("0"+(i+1)+"   /   "+entry.Kind.ToString().ToUpperInvariant(),hubSmall);
                bool clicked=GUILayout.Button(entry.Title+"\n\n"+entry.Description,hubButton,GUILayout.Height(152));
                GUILayout.EndVertical();if(i<Config.Experiences.Length-1)GUILayout.Space(24);
                if(clicked)ChooseExperience(entry.Id);
            }
            GUILayout.EndHorizontal();GUILayout.Space(20);
            bool demo=GUILayout.Toggle(DemoMode,DemoMode?"Demo mode: ON — feedback will be skipped":"Demo mode: OFF — offer feedback after each attempt",hubBody);
            if(demo!=DemoMode)SetDemoMode(demo);
            GUILayout.Space(8);GUILayout.BeginHorizontal();
            if(SwitcherOpen)
            {
                Action("Resume",CloseSwitcher);
                Action("Finish test",()=>Finish("completed","manual_finish"));
                Action("End early",()=>Finish("stopped","ended_early"));
            }
            Action("Results booklet",GoResults);GUILayout.EndHorizontal();
            if(SwitcherOpen)Action("Main menu",GoMenu,35);
            GUILayout.Label("Progress and feedback are saved locally for each attempt.",hubSmall);
        }
        void DrawFeedback()
        {
            GUILayout.Label(FeedbackRun!=null&&FeedbackRun.experienceId=="lighting"?"Door choice":"How was that experience?",hubTitle);
            if(FeedbackRun==null){GUILayout.Label("No run selected.",hubBody);Action("Back to menu",GoMenu);return;}
            var r=FeedbackRun;
            GUILayout.Label(r.title+"  ·  "+r.outcome+"  ·  active time "+Duration(r.activeSeconds),hubBody);
            GUILayout.Space(7);
            if(r.experienceId=="lighting"){
                GUILayout.Label("Why did you choose that door?",hubBody);
                r.notes=GUILayout.TextArea(r.notes??"",1200,hubField,GUILayout.Height(150));
                GUILayout.BeginHorizontal();Action("Save feedback & continue",()=>SaveFeedback());Action("Skip",()=>SaveFeedback(true,"skipped"));GUILayout.EndHorizontal();return;
            }
            r.clarity=Rating("How clear was the objective?",r.clarity);
            r.tension=Rating("How tense did it feel?",r.tension);
            r.enjoyment=Rating("How enjoyable was it?",r.enjoyment);
            GUILayout.Label("What worked?",hubSmall);r.worked=GUILayout.TextArea(r.worked??"",1200,hubField,GUILayout.Height(56));
            GUILayout.Label("What was confusing or frustrating?",hubSmall);r.confusing=GUILayout.TextArea(r.confusing??"",1200,hubField,GUILayout.Height(56));
            GUILayout.Label("Anything else?",hubSmall);r.notes=GUILayout.TextArea(r.notes??"",1200,hubField,GUILayout.Height(45));
            GUILayout.Space(10);GUILayout.BeginHorizontal();
            Action("Save feedback & continue",()=>SaveFeedback());
            Action("Skip / already answered",()=>SaveFeedback(true,"skipped_or_already_answered"));GUILayout.EndHorizontal();
            GUILayout.Label("Optional. 1 = low, 5 = high. Leave a rating blank if unsure.",hubSmall);
        }
        int Rating(string label,int current)
        {
            GUILayout.BeginHorizontal();GUILayout.Label(label,hubBody,GUILayout.Width(430));
            int value=GUILayout.SelectionGrid(current,new[]{"—","1","2","3","4","5"},6,hubButton,GUILayout.Height(35));GUILayout.EndHorizontal();return value;
        }
        void DrawBook()
        {
            GUILayout.Label("Playthrough booklet",hubTitle);
            GUILayout.BeginHorizontal();
            string[] choices=new[]{"All experiences"}.Concat(Config.Experiences.Select(e=>e.Title)).ToArray();
            int next=GUILayout.SelectionGrid(filter,choices,choices.Length,hubButton,GUILayout.Height(43));
            if(next!=filter){filter=next;bookIndex=0;RefreshBook();}GUILayout.EndHorizontal();
            GUILayout.Space(8);GUILayout.BeginHorizontal();
            GUI.enabled=bookIndex>0;Action("← Previous",()=>{bookIndex--;bookScroll=Vector2.zero;},35);
            GUI.enabled=true;GUILayout.Label(book.Count==0?"No playthroughs yet":"Run "+(bookIndex+1)+" of "+book.Count,hubBody);
            GUI.enabled=bookIndex+1<book.Count;Action("Next →",()=>{bookIndex++;bookScroll=Vector2.zero;},35);GUI.enabled=true;
            Action("Refresh",RefreshBook,35);GUILayout.EndHorizontal();
            bookScroll=GUILayout.BeginScrollView(bookScroll,GUILayout.Height(355));
            if(book.Count==0)GUILayout.Label("Play an experience to create a page here. Older detailed recordings remain in the F8 developer viewer.",hubBody);
            else
            {
                var r=book[bookIndex];
                GUILayout.Label(r.title,hubTitle);
                GUILayout.Label("Outcome: "+r.outcome+"   |   "+(r.demo?"DEMO":"PLAYTEST")+"   |   "+r.startedUtc,hubSmall);
                GUILayout.Space(8);
                GUILayout.Label("Active play time  "+Duration(r.activeSeconds)+"     Total elapsed  "+Duration(r.wallSeconds),hubBody);
                GUILayout.Label("Fuses returned  "+r.fuseReturns+"     Deaths  "+r.deaths+"     Interactions  "+r.interactions,hubBody);
                if(r.experienceId=="security")GUILayout.Label("Successful scout spots  "+r.scoutSpots,hubBody);
                GUILayout.Label("Terminal uses  "+r.terminalUses+"     Time operating terminal  "+Duration(r.terminalSeconds),hubBody);
                if(r.lightingOrder.Count>0)
                    GUILayout.Label("Lighting order: "+string.Join(" → ",r.lightingOrder.Select((v,i)=>v+(i<r.lightingTimes.Count?" ("+Duration(r.lightingTimes[i])+")":""))),hubBody);
                GUILayout.Space(10);GUILayout.Label("Feedback: "+r.feedbackStatus+(r.skipReason!=""?" / "+r.skipReason:""),hubBody);
                GUILayout.Label("Clarity "+Score(r.clarity)+"   Tension "+Score(r.tension)+"   Enjoyment "+Score(r.enjoyment),hubBody);
                if(!string.IsNullOrWhiteSpace(r.worked))GUILayout.Label("Worked: "+r.worked,hubBody);
                if(!string.IsNullOrWhiteSpace(r.confusing))GUILayout.Label("Confusing: "+r.confusing,hubBody);
                if(!string.IsNullOrWhiteSpace(r.notes))GUILayout.Label("Notes: "+r.notes,hubBody);
                GUILayout.Label("Version "+r.version+"   |   Reason: "+r.reason+"\nRun ID: "+r.id,hubSmall);
                Action("Add / edit this run’s feedback",()=>ReviewFeedback(r),35);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();Action("Main menu",GoMenu);Action("Export all runs to CSV",()=>{try{exportMessage=HubRunStore.Export();}catch(Exception e){exportMessage=e.Message;}});GUILayout.EndHorizontal();
            if(exportMessage!="")GUILayout.Label(exportMessage,hubSmall);
        }
        static string Score(int value)=>value<=0?"—":value+"/5";
    }
}








