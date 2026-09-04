#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Titipi.Editor
{
    public class MocaBuildUtils
    {
        public readonly CommandLineInputs Inputs = new();
        public readonly Logging Log = new();

        public enum BuildPlatform
        {
            UNKNOWN,
            ANDROID,
            IOS,
        }

        public string[] GetActiveScenes()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }

        public class CommandLineInputs
        {
            public readonly string AndroidSdkPath;
            public readonly string BuildOutput;
            public readonly string AndroidKeystorePath;
            public readonly string AndroidKeystoreAlias;
            public readonly string AndroidKeystorePassword;
            public readonly string AndroidKeystoreAliasPassword;
            public readonly BuildPlatform BuildPlatform;

            public CommandLineInputs()
            {
                string[] cmdArgs = Environment.GetCommandLineArgs();

                for (var i = 0; i < cmdArgs.Length; i++)
                {
                    if (cmdArgs[i].Equals("-buildPlatform"))
                        BuildPlatform = (BuildPlatform)Enum.Parse(typeof(BuildPlatform), cmdArgs[i + 1]);
                    if (cmdArgs[i].Equals("-androidSdkPath"))
                        AndroidSdkPath = cmdArgs[i + 1];
                    if (cmdArgs[i].Equals("-buildOutput"))
                        BuildOutput = cmdArgs[i + 1];
                    if (cmdArgs[i].Equals("-androidKeystorePath"))
                        AndroidKeystorePath = cmdArgs[i + 1];
                    if (cmdArgs[i].Equals("-androidKeystoreAlias"))
                        AndroidKeystoreAlias = cmdArgs[i + 1];
                    if (cmdArgs[i].Equals("-androidKeystorePassword"))
                        AndroidKeystorePassword = cmdArgs[i + 1];
                    if (cmdArgs[i].Equals("-androidKeystoreAliasPassword"))
                        AndroidKeystoreAliasPassword = cmdArgs[i + 1];
                }
            }
        }

        public class Logging
        {
            private bool _initialized;

            private void Initialize()
            {
                if (_initialized) return;

                var sw = new StreamWriter(Console.OpenStandardOutput(), System.Text.Encoding.ASCII);
                sw.AutoFlush = true;
                Console.SetOut(sw);

                _initialized = true;
            }

            public void Fail(string message)
            {
                Initialize();
                Console.WriteLine("");
                Console.WriteLine("\x1b[31m" + message + "\x1b[0m");
                Console.WriteLine("");
            }

            public void Done(string message)
            {
                Initialize();
                Console.WriteLine("");
                Console.WriteLine("\x1b[32m" + message + "\x1b[0m");
                Console.WriteLine("");
            }

            public void Info(string message)
            {
                Initialize();
                Console.WriteLine("");
                Console.WriteLine("\x1b[34m" + message + "\x1b[0m");
                Console.WriteLine("");
            }

            public void Warn(string message)
            {
                Initialize();
                Console.WriteLine("");
                Console.WriteLine("\x1b[33m" + message + "\x1b[0m");
                Console.WriteLine("");
            }

            public void Print(string message)
            {
                Initialize();
                Console.WriteLine(message);
            }
        }

        public void PrintInputs()
        {
            Log.Print(" -buildOutput: " + Inputs.BuildOutput);
            Log.Print(" -buildPlatform: " + Inputs.BuildPlatform);

#if UNITY_ANDROID
            Log.Print(" -androidSdkPath: " + Inputs.AndroidSdkPath);
            Log.Print(" -androidKeystorePath: " + Inputs.AndroidKeystorePath);
            Log.Print(" -androidKeystoreAlias: " + (string.IsNullOrEmpty(Inputs.AndroidKeystoreAlias) ? "" : "***"));
            Log.Print(" -androidKeystorePassword: " + (string.IsNullOrEmpty(Inputs.AndroidKeystorePassword) ? "" : "***"));
            Log.Print(" -androidKeystoreAliasPassword: " + (string.IsNullOrEmpty(Inputs.AndroidKeystoreAliasPassword) ? "" : "***"));
#endif

            Log.Print("");
        }
    }
}

#endif // UNITY_EDITOR
