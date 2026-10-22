using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DeadAir.Playtesting
{
    [Serializable] public sealed class AreaDefinition { public string id,displayName,category; }
    [Serializable] public sealed class AreaVisit { public string areaId; public double entered,exited; }
    [Serializable] public sealed class PlaytestEvent
    {
        public double time; public string type,objectId,metadata; public Vector3 position;
        public float value,duration;
    }
    [Serializable] public sealed class SessionMetadata
    {
        public int schemaVersion=1;
        public string id,startedUtc,endedUtc,sceneName,scenePath,version,buildGuid,unityVersion;
        public string status="recording",endReason="";
        public double duration,wallDuration;
        public int sampleCount,eventCount;
        public List<string> movementStates=new List<string>();
        public List<AreaDefinition> areas=new List<AreaDefinition>();
        public List<AreaVisit> visits=new List<AreaVisit>();
    }
    public struct PositionSample
    {
        public double time;
        public Vector3 position;
        public Quaternion rotation,camera;
        public int area,state,flags;
    }
    public sealed class LoadedSession
    {
        public SessionMetadata metadata;
        public List<PositionSample> samples=new List<PositionSample>();
        public List<PlaytestEvent> events=new List<PlaytestEvent>();
        public string folder,warning="";
    }
    public static class PlaytestStorage
    {
        public const int Magic=0x44415031,RecordBytes=64;
        public static string Root=>Path.Combine(Application.persistentDataPath,"DeadAirPlaytests");
        public static string Exports=>Path.Combine(Root,"Exports");
        public static void SaveMetadata(string folder,SessionMetadata data)
        {
            string path=Path.Combine(folder,"session.json"),temp=path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(data,true),new UTF8Encoding(false));
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public static List<SessionMetadata> ListSessions(out string warning)
        {
            warning="";var result=new List<SessionMetadata>();if(!Directory.Exists(Root))return result;
            foreach(string folder in Directory.GetDirectories(Root))
            {
                string path=Path.Combine(folder,"session.json");if(!File.Exists(path))continue;
                try { var data=ReadMetadata(folder);result.Add(data); }
                catch(Exception e){warning+="Skipped "+Path.GetFileName(folder)+": "+e.Message+"\n";}
            }
            result.Sort((a,b)=>string.CompareOrdinal(b.startedUtc,a.startedUtc));return result;
        }
        public static SessionMetadata ReadMetadata(string folder)
        {
            string path=Path.Combine(folder,"session.json");if(new FileInfo(path).Length>8*1024*1024)throw new IOException("Metadata exceeds limit.");
            var data=JsonUtility.FromJson<SessionMetadata>(File.ReadAllText(path));
            if(data==null||data.schemaVersion!=1||!Guid.TryParseExact(data.id,"N",out _)||!Finite(data.duration)||data.duration<0)throw new IOException("Invalid session metadata.");
            data.areas??=new List<AreaDefinition>();data.visits??=new List<AreaVisit>();data.movementStates??=new List<string>();
            return data;
        }
        public static LoadedSession Load(string id)
        {
            if(!Guid.TryParseExact(id,"N",out _))throw new IOException("Invalid session ID.");
            return LoadFolder(Path.Combine(Root,id));
        }
        public static LoadedSession LoadFolder(string folder)
        {
            var loaded=new LoadedSession{folder=folder,metadata=ReadMetadata(folder)};
            string path=Path.Combine(folder,"positions.bin");
            if(File.Exists(path))using(var reader=new BinaryReader(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
            {
                if(reader.BaseStream.Length<8||reader.ReadInt32()!=Magic||reader.ReadInt32()!=1)throw new IOException("Unsupported position recording.");
                long count=(reader.BaseStream.Length-8)/RecordBytes;if(count>2000000)throw new IOException("Recording exceeds safe viewer limit (2 million samples).");
                if((reader.BaseStream.Length-8)%RecordBytes!=0)loaded.warning+="Incomplete final sample ignored. ";
                loaded.samples.Capacity=(int)count;double previous=-1;
                for(long i=0;i<count;i++)
                {
                    var s=Read(reader);
                    if(!Finite(s.time)||s.time<previous||!Finite(s.position)||!Finite(s.rotation)||!Finite(s.camera))throw new IOException("Invalid sample at index "+i);
                    previous=s.time;loaded.samples.Add(s);
                }
            }
            else loaded.warning+="Position recording missing. ";
            path=Path.Combine(folder,"events.jsonl");
            if(File.Exists(path))using(var reader=new StreamReader(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)))
            {
                string line;while((line=reader.ReadLine())!=null&&loaded.events.Count<100000)
                    try{var e=JsonUtility.FromJson<PlaytestEvent>(line);if(e!=null&&e.type!=null&&Finite(e.time)&&e.time>=0)loaded.events.Add(e);else loaded.warning="Some invalid events skipped. ";}
                    catch{loaded.warning="Some invalid events skipped. ";}
            }
            if(loaded.samples.Count>0)loaded.metadata.duration=Math.Max(loaded.metadata.duration,loaded.samples[loaded.samples.Count-1].time);
            if(loaded.metadata.status=="recording"&&(PlaytestRecorder.Instance==null||PlaytestRecorder.Instance.Metadata?.id!=loaded.metadata.id))
            {loaded.metadata.status="interrupted";loaded.warning+="Session did not close normally; recovered buffered data. ";}
            return loaded;
        }
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
        static bool Finite(Quaternion q)=>Finite(q.x)&&Finite(q.y)&&Finite(q.z)&&Finite(q.w);
        public static void Write(BinaryWriter w,PositionSample s)
        {
            w.Write(s.time);w.Write(s.position.x);w.Write(s.position.y);w.Write(s.position.z);
            Write(w,s.rotation);Write(w,s.camera);w.Write(s.area);w.Write(s.state);w.Write(s.flags);
        }
        static void Write(BinaryWriter w,Quaternion q){w.Write(q.x);w.Write(q.y);w.Write(q.z);w.Write(q.w);}
        public static PositionSample Read(BinaryReader r)=>new PositionSample{time=r.ReadDouble(),position=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()),rotation=Q(r),camera=Q(r),area=r.ReadInt32(),state=r.ReadInt32(),flags=r.ReadInt32()};
        static Quaternion Q(BinaryReader r)=>new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
        public static PositionSample Evaluate(IReadOnlyList<PositionSample> samples,double time,out int index)
        {
            index=0;if(samples.Count==0)return default;
            int lo=0,hi=samples.Count-1;while(lo<hi){int mid=(lo+hi+1)/2;if(samples[mid].time<=time)lo=mid;else hi=mid-1;}index=lo;
            var a=samples[lo];if(lo==samples.Count-1)return a;var b=samples[lo+1];
            if((b.flags&1)!=0||b.time<=a.time)return a;
            float t=Mathf.Clamp01((float)((time-a.time)/(b.time-a.time)));a.position=Vector3.Lerp(a.position,b.position,t);a.rotation=Quaternion.Slerp(a.rotation,b.rotation,t);a.camera=Quaternion.Slerp(a.camera,b.camera,t);a.time=time;return a;
        }
    }
}
