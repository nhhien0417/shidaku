#if UNITY_ANDROID

using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

namespace CostCenter.Editor {
    public class CCModifyUnityAndroidAppManifest : IPostGenerateGradleAndroidProject
    {

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            // If needed, add condition checks on whether you need to run the modification routine.
            // For example, specific configuration/app options enabled

            var androidManifest = new AndroidManifest(GetManifestPath(basePath));

            androidManifest.AddRequiredPermissions();

            // Add your XML manipulation routines

            androidManifest.Save();
        }

        public int callbackOrder { get { return 1; } }

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }
            return _manifestFilePath;
        }
    }


    internal class AndroidXmlDocument : XmlDocument
    {
        private string m_Path;
        protected XmlNamespaceManager nsMgr;
        public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        public AndroidXmlDocument(string path)
        {
            m_Path = path;
            using (var reader = new XmlTextReader(m_Path))
            {
                reader.Read();
                Load(reader);
            }
            nsMgr = new XmlNamespaceManager(NameTable);
            nsMgr.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save()
        {
            return SaveAs(m_Path);
        }

        public string SaveAs(string path)
        {
            using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }


    internal class AndroidManifest : AndroidXmlDocument
    {
        private const string INSTALL_REFERRER_PERMISSION_ATTR = "com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE";
        private const string AD_ID_PERMISSION_ATTR = "com.google.android.gms.permission.AD_ID";
        private readonly XmlElement ApplicationElement;

        public AndroidManifest(string path) : base(path)
        {
            ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;
        }

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            XmlAttribute attr = CreateAttribute("android", key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }

        internal void AddRequiredPermissions()
        {
            var manifest = SelectSingleNode("/manifest");
            XmlElement child = CreateElement("uses-permission");
            manifest.AppendChild(child);
            child.Attributes.Append(CreateAndroidAttribute("name", INSTALL_REFERRER_PERMISSION_ATTR));
            child.Attributes.Append(CreateAndroidAttribute("name", AD_ID_PERMISSION_ATTR));
        }
    }
}

#endif
