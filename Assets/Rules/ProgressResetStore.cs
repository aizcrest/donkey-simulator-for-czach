using System.IO;
namespace DonkeyForest
{
    public static class ProgressResetStore
    {
        public static void Replace(string path,string freshJson)
        {
            // Stage first so storage failures leave the current save intact. A .reset
            // file is never a load/recovery candidate. Replace is atomic on supported filesystems.
            string staged=path+".reset";
            try
            {
                File.WriteAllText(staged,freshJson);
                // Recovery files must not bring the previous adventure back after a reset.
                File.Delete(path+".bak");File.Delete(path+".tmp");
                if(File.Exists(path))File.Replace(staged,path,null);
                else File.Move(staged,path);
            }
            finally
            {
                // Do not mask the original error if staging cleanup also fails.
                try{File.Delete(staged);}catch(IOException){}catch(System.UnauthorizedAccessException){}
            }
        }
    }
}
