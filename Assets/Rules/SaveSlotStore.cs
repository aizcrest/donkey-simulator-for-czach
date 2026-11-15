using System;
using System.IO;
namespace DonkeyForest
{
    // File storage is independent of Unity so isolation and recovery can be tested on disk.
    public sealed class SaveSlotStore
    {
        public const int Count=5;
        readonly string directory;
        string ActivePath=>Path.Combine(directory,"forest-active-slot.txt");
        public SaveSlotStore(string directory){this.directory=directory;}
        public string PathFor(int slot)
        {
            if(slot<1||slot>Count)throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory,"forest-slot-"+slot+".json");
        }
        public int ActiveSlot
        {
            get
            {
                try { if(int.TryParse(File.ReadAllText(ActivePath),out int slot)&&slot>=1&&slot<=Count)return slot; }
                catch(IOException){}catch(UnauthorizedAccessException){}
                return 1;
            }
        }
        public void Select(int slot){PathFor(slot);ProgressResetStore.Replace(ActivePath,slot.ToString());}
        public bool Exists(int slot)
        {
            string path=PathFor(slot);return File.Exists(path)||File.Exists(path+".bak")||File.Exists(path+".tmp");
        }
        public bool TryRead(int slot,Func<string,bool> valid,out string json)=>TryReadPath(PathFor(slot),valid,out json);
        static bool TryReadPath(string path,Func<string,bool> valid,out string json)
        {
            foreach(string candidate in new[]{path,path+".bak",path+".tmp"})
            {
                try
                {
                    if(!File.Exists(candidate))continue;
                    string value=File.ReadAllText(candidate);
                    if(valid(value)){json=value;return true;}
                }
                catch(IOException){}catch(UnauthorizedAccessException){}
            }
            json=null;return false;
        }
        public void Write(int slot,string json,bool replaceAdventure=false)
        {
            string path=PathFor(slot);
            if(replaceAdventure){ProgressResetStore.Replace(path,json);return;}
            string temporary=path+".tmp";
            File.WriteAllText(temporary,json);
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");
            else File.Move(temporary,path);
        }
        public void MigrateLegacy(Func<string,bool> valid)
        {
            // Never resurrect the legacy adventure after a slot has been created/reset.
            for(int i=1;i<=Count;i++)if(Exists(i))return;
            if(File.Exists(ActivePath))return;
            if(TryReadPath(Path.Combine(directory,"forest-rider.json"),valid,out string json))Write(1,json,true);
            // The original file is retained, untouched, for a manual rollback.
        }
    }
}
