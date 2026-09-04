using System.IO;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace CostCenter.Editor {
    public class CCBuildPostProcessor
    {
        #if UNITY_IOS && !UNITY_EDITOR
        [PostProcessBuildAttribute(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.iOS)
            {
                // Read.
                string projectPath = PBXProject.GetPBXProjectPath(path);
                PBXProject project = new PBXProject();
                project.ReadFromString(File.ReadAllText(projectPath));
                string targetGUID = project.TargetGuidByName("UnityFramework");

                AddFrameworks(project, targetGUID);

                // Write.
                File.WriteAllText(projectPath, project.WriteToString());

                // Add plist values
                // AddPListValues(path);

                // // Get plist
                // string plistPath = path + "/Info.plist";
                // PlistDocument plist = new PlistDocument();
                // plist.ReadFromString(File.ReadAllText(plistPath));
        
                // // Get root
                // PlistElementDict rootDict = plist.root;
        
                // // Change value of CFBundleVersion in Xcode plist
                // rootDict.SetString("CFBundleVersion", "2.3.4");
        
                // // Write to file
                // File.WriteAllText(plistPath, plist.WriteToString());
            }
        }

        static void AddPListValues(string pathToXcode) {
            // Retrieve the plist file from the Xcode project directory:
            string plistPath = pathToXcode + "/Info.plist";
            PlistDocument plistObj = new PlistDocument();
    
    
            // Read the values from the plist file:
            plistObj.ReadFromString(File.ReadAllText(plistPath));
    
            // Set values from the root object:
            PlistElementDict plistRoot = plistObj.root;
    
            // Set the description key-value in the plist:
            plistRoot.SetString("NSUserTrackingUsageDescription", "Your data will be used to deliver personalized ads to you");
    
            // Save changes to the plist:
            File.WriteAllText(plistPath, plistObj.WriteToString());
        }

        static void AddFrameworks(PBXProject project, string targetGUID)
        {
            project.AddFrameworkToProject(targetGUID, "AdServices.framework", false);
            project.AddFrameworkToProject(targetGUID, "AdSupport.framework", false);
            project.AddFrameworkToProject(targetGUID, "AppTrackingTransparency.framework", false);

            // Add `-ObjC` to "Other Linker Flags".
            // project.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC");

            // Disable bitcode
            // project.SetBuildProperty(targetGUID, "ENABLE_BITCODE", "false");
        }
        #endif
    }
}
