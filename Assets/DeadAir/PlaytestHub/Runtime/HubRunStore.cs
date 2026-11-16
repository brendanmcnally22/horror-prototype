using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeadAir.Playtesting
{
    [Serializable] public class HubRun
    {
        public int schemaVersion=1;
        public string id, experienceId, title, scenePath, startedUtc, endedUtc, version, outcome="recording", reason="";
        public double activeSeconds, wallSeconds;
        public bool demo;
        public int fuseReturns, deaths, interactions, terminalUses, scoutSpots;
        public float terminalSeconds;
        public List<string> lightingOrder=new List<string>();
        public List<float> lightingTimes=new List<float>();
        public string feedbackStatus="pending", skipReason="", worked="", confusing="", notes="";
        public int clarity, tension, enjoyment;
    }
    public static class HubRunStore
    {
        public static string Root=>Path.Combine(Application.persistentDataPath,"DeadAirPlaytests");
        public static string LastError {get;private set;}="";
        public static bool Save(HubRun run)
        {
            try
            {
                if(run==null||!Guid.TryParseExact(run.id,"N",out _))throw new IOException("Invalid playtest run ID.");
                string folder=Path.Combine(Root,run.id);Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,"hub-run.json"),temp=path+".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(run,true),new UTF8Encoding(false));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                LastError="";return true;
            }
            catch(Exception e){LastError="Run could not be saved: "+e.Message;Debug.LogWarning(LastError);return false;}
        }
        public static List<HubRun> List()
        {
            LastError="";var runs=new List<HubRun>();if(!Directory.Exists(Root))return runs;
            foreach(string folder in Directory.GetDirectories(Root))
            {
                string path=Path.Combine(folder,"hub-run.json");if(!File.Exists(path))continue;
                try
                {
                    if(new FileInfo(path).Length>1024*1024)throw new IOException("Run file too large.");
                    var r=JsonUtility.FromJson<HubRun>(File.ReadAllText(path));
                    if(r==null||r.schemaVersion!=1||!Guid.TryParseExact(r.id,"N",out _)||!PlaytestStorage.Finite(r.activeSeconds)||r.activeSeconds<0)throw new IOException("Invalid run.");
                    r.lightingOrder??=new List<string>();r.lightingTimes??=new List<float>();
                    if(r.outcome=="recording"&&(PlaytestHub.Instance==null||PlaytestHub.Instance.CurrentRun?.id!=r.id)){r.outcome="interrupted";r.reason="run_did_not_close";}
                    runs.Add(r);
                }
                catch(Exception e){LastError="Some damaged run files were skipped: "+e.Message;}
            }
            return runs.OrderByDescending(r=>r.startedUtc,StringComparer.Ordinal).ToList();
        }
        static string Q(string value)
        {
            value??="";
            if(value.Length>0&&"=+-@".Contains(value[0]))value="'"+value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }
        public static string Export()
        {
            var runs=List();string folder=Path.Combine(Root,"Exports");Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"playtest-booklet-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".csv");
            using(var writer=new StreamWriter(path,false,new UTF8Encoding(true)))
            {
                writer.WriteLine("run_id,experience,started_utc,version,outcome,active_seconds,wall_seconds,demo,fuses,deaths,interactions,terminal_uses,terminal_seconds,lighting_order,feedback_status,skip_reason,clarity,tension,enjoyment,worked,confusing,notes");
                foreach(var r in runs)writer.WriteLine(string.Join(",",new[]{r.id,r.title,r.startedUtc,r.version,r.outcome,r.activeSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),r.wallSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),r.demo.ToString(),r.fuseReturns.ToString(),r.deaths.ToString(),r.interactions.ToString(),r.terminalUses.ToString(),r.terminalSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),string.Join(" > ",r.lightingOrder),r.feedbackStatus,r.skipReason,r.clarity.ToString(),r.tension.ToString(),r.enjoyment.ToString(),r.worked,r.confusing,r.notes}.Select(Q)));
            }
            return path;
        }
    }
}

