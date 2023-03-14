using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PrivateServerConnectTool
{
    class Patch
    {
        Dictionary<string, string> RSAPatchMD5;
        Dictionary<string, string> mhypbaseMD5;
        Dictionary<string, string> patchFilename;

        public enum PatchOpeartionStatus
        {
            PATCH_FILE_NOT_EXIST,
            mhypbase_FILE_NOT_EXIST,
            PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION,
            SUCCESS,
            NOT_PATCHED,
            PATCHED
        }
        public Patch()
        { 
            RSAPatchMD5 = new Dictionary<string, string>();
            mhypbaseMD5 = new Dictionary<string, string>();
            patchFilename = new Dictionary<string, string>();

            RSAPatchMD5.Add("v3.4", "505665eec269d92cc7aee7fba0da01fd");
            mhypbaseMD5.Add("v3.4", "cec8d9e5d9f2c4eeb4c60a958a9e419c");
            patchFilename.Add("v3.4", "RSAPatch_v3.4.dll");

            RSAPatchMD5.Add("v3.5", "f3466b6fc8bd2c32c137580f40c25e7c");
            mhypbaseMD5.Add("v3.5", "948327bf35efdafe2869062aa940c864");
            patchFilename.Add("v3.5", "RSAPatch_v3.5.dll");
        }

        public PatchOpeartionStatus GetPatchStatus(string mhypbasePath)
        {
            if (!File.Exists(mhypbasePath))
            {
                return PatchOpeartionStatus.mhypbase_FILE_NOT_EXIST;
            }

            var current_mhypbase_MD5 = Utilities.CalculateMD5(mhypbasePath);
            current_mhypbase_MD5.ToUpper();

            bool mhypbase_supportFind = false;
            string version = "";

            foreach (var mhypbase_support in mhypbaseMD5)
            {
                version = mhypbase_support.Key;
                var mhypbase_support_MD5 = mhypbase_support.Value;
                mhypbase_support_MD5 = mhypbase_support_MD5.ToUpper();

                if (mhypbase_support_MD5 == current_mhypbase_MD5)
                {
                    mhypbase_supportFind = true;
                }
            }

            if (mhypbase_supportFind)
            {
                return PatchOpeartionStatus.NOT_PATCHED;
            }
            else
            {
                bool RSAPatchSupportFind = false;

                foreach (var RSAPatchSupport in RSAPatchMD5)
                {
                    version = RSAPatchSupport.Key;
                    var RSAPatchMD5 = RSAPatchSupport.Value;
                    RSAPatchMD5 = RSAPatchMD5.ToUpper();

                    if (RSAPatchMD5 == current_mhypbase_MD5)
                    {
                        RSAPatchSupportFind = true;
                    }
                }

                if(RSAPatchSupportFind)
                {
                    return PatchOpeartionStatus.PATCHED;
                }
                else
                {
                    return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
                }
            }
        }

        public PatchOpeartionStatus DoPatch(string RSAPatchPath, string mhypbasePath)
        {
            bool patchFind = false;
            string version = "";

            foreach (var item in mhypbaseMD5)
            {
                version = item.Key;
                var support_mhypbase_MD5 = item.Value;
                support_mhypbase_MD5 = support_mhypbase_MD5.ToUpper();

                var current_mhypbase_MD5 = Utilities.CalculateMD5(mhypbasePath);
                current_mhypbase_MD5.ToUpper();

                if (current_mhypbase_MD5 == support_mhypbase_MD5)
                {
                    patchFind = true;
                }
            }

            if (!patchFind)
            {
                return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
            }

            string RSAFilename;
            bool RSAFilenameIsFind = patchFilename.TryGetValue(version, out RSAFilename);
            var RSAPatchFullpath = Path.Combine(RSAPatchPath, RSAFilename);
            
            if (!RSAFilenameIsFind)
            {
                return PatchOpeartionStatus.PATCH_FILE_NOT_EXIST;
            }

            if (!File.Exists(RSAPatchFullpath))
            {
                return PatchOpeartionStatus.PATCH_FILE_NOT_EXIST;
            }
            if (!File.Exists(mhypbasePath))
            {
                return PatchOpeartionStatus.mhypbase_FILE_NOT_EXIST;
            }

            string RSASupportMD5;
            bool RSAPatchFind = RSAPatchMD5.TryGetValue(version,out RSASupportMD5);
            if (RSAPatchFind)
            {
                var currentRSAMD5 = Utilities.CalculateMD5(RSAPatchFullpath);
                currentRSAMD5 = currentRSAMD5.ToUpper();

                RSASupportMD5 = RSASupportMD5.ToUpper();

                if (currentRSAMD5 == RSASupportMD5)
                {
  
                        File.Copy(mhypbasePath, mhypbasePath + ".backup", true);
                        File.Copy(RSAPatchFullpath, mhypbasePath, true);

                        return PatchOpeartionStatus.SUCCESS;
                }
                else
                {
                    return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
                }
            }
            return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
        }

        public PatchOpeartionStatus DoUnpatch(string RSAPatchPath, string mhypbasePath)
        {
            if (!File.Exists(RSAPatchPath))
            {
                return PatchOpeartionStatus.PATCH_FILE_NOT_EXIST;
            }
            if (!File.Exists(mhypbasePath))
            {
                return PatchOpeartionStatus.mhypbase_FILE_NOT_EXIST;
            }

            bool patchFind = false;
            string version = "";

            foreach (var item in RSAPatchMD5)
            {
                version = item.Key;
                var supportRSAPatchMD5 = item.Value;
                supportRSAPatchMD5 = supportRSAPatchMD5.ToUpper();

                var currentRSAPatchMD5 = Utilities.CalculateMD5(RSAPatchPath);
                currentRSAPatchMD5 = currentRSAPatchMD5.ToUpper();

                if (currentRSAPatchMD5 == supportRSAPatchMD5)
                {
                    patchFind = true;
                }
            }

            if (!patchFind)
            {
                return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
            }

            string mhypbase_support_MD5;
            bool mhypbase_support_Find = mhypbaseMD5.TryGetValue(version, out mhypbase_support_MD5);
            if (mhypbase_support_Find)
            {
                var current_mhypbase_MD5 = Utilities.CalculateMD5(mhypbasePath);
                current_mhypbase_MD5 = current_mhypbase_MD5.ToUpper();

                mhypbase_support_MD5 = mhypbase_support_MD5.ToUpper();

                if (current_mhypbase_MD5 == mhypbase_support_MD5)
                {
                    File.Delete(RSAPatchPath);
                    File.Copy(mhypbasePath, RSAPatchPath);
                    File.Delete(mhypbasePath);

                    return PatchOpeartionStatus.SUCCESS;
                }
                else
                {
                    return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
                }
            }
            return PatchOpeartionStatus.PATCH_NOT_SUPPORT_CURRENT_GAME_VERSION;
        }
    }
}
