using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Blockout.Discovery
{
    public class DesignSession : MonoBehaviour
    {
        static DesignSession instance;
        static bool quitting;
        public static string CurrentFile=>instance?instance.file:"";
        StreamWriter writer;
        string session,file;
        double start;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){instance=null;quitting=false;}
        void Awake()
        {
            if(instance&&instance!=this){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);
            session=Guid.NewGuid().ToString("N");start=Time.realtimeSinceStartupAsDouble;
            try
            {
                string folder=System.IO.Path.Combine(Application.persistentDataPath,"PlaytestLogs");Directory.CreateDirectory(folder);
                file=System.IO.Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+session.Substring(0,8)+".csv");
                writer=new StreamWriter(file,false,new System.Text.UTF8Encoding(true)){AutoFlush=true};
                writer.WriteLine("session_id,utc,elapsed_seconds,build_version,build_guid,scene,event,zone,x,y,z,value,duration_seconds");
                Write("session_start","",Vector3.zero,0,0);
            }
            catch(Exception e){Debug.LogWarning("Playtest logging unavailable: "+e.Message);}
        }
        public static void Record(string eventName,string zone,Vector3 position,float value=0,float duration=0)
        {
            if(!Application.isPlaying||quitting)return;
            if(!instance)new GameObject("Playtest Session Log").AddComponent<DesignSession>();
            instance.Write(eventName,zone,position,value,duration);
        }
        static string Q(string s)=>"\""+(s??"").Replace("\"","\"\"")+"\"";
        static string N(double n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
        void Write(string evt,string zone,Vector3 p,float value,float duration)
        {
            if(writer==null)return;
            try{writer.WriteLine(string.Join(",",Q(session),Q(DateTime.UtcNow.ToString("O")),N(Time.realtimeSinceStartupAsDouble-start),Q(Application.version),Q(Application.buildGUID),Q(SceneManager.GetActiveScene().name),Q(evt),Q(zone),N(p.x),N(p.y),N(p.z),N(value),N(duration)));}
            catch(Exception e){Debug.LogWarning("Playtest log write failed: "+e.Message);writer.Dispose();writer=null;}
        }
        public void Flush(){writer?.Flush();}
        void OnApplicationPause(bool paused){if(paused)Flush();}
        void OnApplicationQuit(){if(instance==this){Write("session_end","",Vector3.zero,0,(float)(Time.realtimeSinceStartupAsDouble-start));quitting=true;}}
        void OnDestroy(){if(instance==this){writer?.Dispose();writer=null;instance=null;}}
    }
}
