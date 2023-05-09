using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace EditorTools.ShaderPropertiesConverter
{
    public enum PropertyType{None, Texture, Float, Vector, Color}

    /// <summary>
    /// PropertyData class
    /// </summary>
    /// <remarks>
    /// This class is used to store the data of shader properties.
    /// </remarks>
    [Serializable]
    public class PropertyData
    {
        /// <value>Property <paramref name="source"/> represents the name of source shader property.</value>
        public string source{ get; set; }
        /// <value>Property <paramref name="dest"/> represents the name of destination shader property.</value>
        public string dest { get; set ; } = "None";
        /// <value>Property <paramref name="type"/> represents the type of shader property.</value>
        public PropertyType type{ get; set; }
    }

    public class ShaderPropertiesConverterWindow : EditorWindow
    {
        Shader shaderSource;
        Shader shaderDest;

        static List<PropertyData> propertyData = new List<PropertyData>();
        List<string> shaderDestPro = new List<string>();

        Vector2 scrollPos;

        [MenuItem("Tools/Shader Properties Converter")]
        static void Init()
        {
            ShaderPropertiesConverterWindow window = (ShaderPropertiesConverterWindow)EditorWindow.GetWindow(typeof(ShaderPropertiesConverterWindow));
            window.titleContent = new GUIContent("Shader Properties Converter", EditorGUIUtility.IconContent("Shader Icon").image);
            window.Show();
        }

        void OnEnable()
        {
            propertyData.Clear();
            shaderDestPro.Clear();
            shaderDestPro.Add("None");
            scrollPos = Vector2.zero;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            shaderSource = (Shader)EditorGUILayout.ObjectField("Shader From", shaderSource, typeof(Shader), false);
            shaderDest = (Shader)EditorGUILayout.ObjectField("Shader To", shaderDest, typeof(Shader), false);
            if (EditorGUI.EndChangeCheck())
            {
                propertyData.Clear();
                shaderDestPro.Clear();
                if (shaderSource == null || shaderDest == null)
                    return;

                shaderDestPro.Add("None");

                for (int i = 0; i < shaderSource.GetPropertyCount(); i++)
                {
                    propertyData.Add(new PropertyData());
                }

                for (int i = 0; i < shaderDest.GetPropertyCount(); i++)
                {
                    shaderDestPro.Add(shaderDest.GetPropertyName(i));
                }
            }

            if (shaderSource == null || shaderDest == null)
                return;

            if (propertyData.Count == shaderSource.GetPropertyCount())
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Source Shader Properties", new GUIStyle("Label") { fontStyle = FontStyle.Bold });
                EditorGUILayout.LabelField("Dest Shader Properties", new GUIStyle("Label") { fontStyle = FontStyle.Bold });
                EditorGUILayout.LabelField("Type", new GUIStyle("Label") { fontStyle = FontStyle.Bold }, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < shaderSource.GetPropertyCount(); i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    propertyData[i].source = shaderSource.GetPropertyName(i);
                    EditorGUILayout.LabelField(propertyData[i].source, GUILayout.Width(150));
                    if (GUILayout.Button(propertyData[i].dest, EditorStyles.popup))
                    {
                        var index = i;
                        SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), new StringListSearchProvider(shaderDestPro, (x) => propertyData[index].dest = x));
                    }
                    propertyData[i].type = (PropertyType)EditorGUILayout.EnumPopup(propertyData[i].type, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button(new GUIContent("Upgrade Project Folder", "This option will scan the whole project folder")))
                {
                    var spc = new ShaderPropertiesConverter();

                    spc.UpgradeProjectFolder(propertyData, shaderSource.name, shaderDest.name);
                }

                if (GUILayout.Button(new GUIContent("Upgrade Selection", "If there are materials selected, only the chosen materials will be changed. Otherwise nothing will be changed")))
                {
                    var spc = new ShaderPropertiesConverter();

                    if (Selection.objects == null)
                        return;
                    
                    var selection = Selection.objects;
                    spc.UpgradeSelection(selection, propertyData, shaderSource.name, shaderDest.name);
                }
                
                    
                
            }
        }
    }

    /// <summary>
    /// ShaderPropertiesConverter class
    /// </summary>
    /// <remarks>
    /// This class is used to convert shader properties from one shader to another.
    /// </remarks>
    public class ShaderPropertiesConverter
    {
        /// <summary>
        /// Upgrade all materials in the project folder
        /// (<paramref name="properties"/>,  <paramref name="oldShader"/>, <paramref name="newShader"/>)
        /// </summary>
        /// <param name="properties">List of properties to be converted</param>
        /// <param name="oldShader">Old shader name</param>
        /// <param name="newShader">New shader name</param>
        public void UpgradeProjectFolder(List<PropertyData> properties, string oldShader, string newShader)
        {
            if ((!Application.isBatchMode) && (!EditorUtility.DisplayDialog("Material Upgrader", "The upgrade will overwrite materials in your project. " + "Make sure to have a project backup before proceeding", "Proceed", "Cancel")))
                return;

            int totalMaterialCount = 0;
            foreach (string s in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(s))
                    totalMaterialCount++;
            }

            int materialIndex = 0;
            foreach (string path in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(path))
                {
                    materialIndex++;
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Updating", string.Format("({0} of {1}) {2}", materialIndex, totalMaterialCount, path), (float)materialIndex / (float)totalMaterialCount))
                        break;

                    Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) as Material;

                    if (!ShouldUpgradeShader(m, oldShader))
                        continue;

                    Upgrade(m, newShader, properties);
                    SaveAssetsAndFreeMemory();
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Upgrade all materials in the selection
        /// (<paramref name="properties"/>, <paramref name="oldShader"/>, <paramref name="newShader"/>)
        /// </summary>
        /// <param name="selection">List of objects to be converted</param>
        /// <param name="properties">List of properties to be converted</param>
        /// <param name="oldShader">Old shader name</param>
        /// <param name="newShader">New shader name</param>
        public void UpgradeSelection(UnityEngine.Object[] selection, List<PropertyData> properties, string oldShader, string newShader)
        {
            if (selection != null)
            {
                if ((!Application.isBatchMode) && (!EditorUtility.DisplayDialog("Material Upgrader", "The upgrade will overwrite materials in your project. " + "Make sure to have a project backup before proceeding", "Proceed", "Cancel")))
                    return;
                    
                int totalMaterialCount = 0;
                foreach (var obj in selection)
                {
                    if (obj.GetType() == typeof(Material))
                        totalMaterialCount++;
                }
                
                int materialIndex = 0;
                foreach (var obj in selection)
                {
                    if (obj.GetType() == typeof(Material))
                    {
                        materialIndex++;
                        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Updating", string.Format("({0} of {1}) {2}", materialIndex, totalMaterialCount, obj.name), (float)materialIndex / (float)totalMaterialCount))
                            break;

                        Material m = obj as Material;

                        if (!ShouldUpgradeShader(m, oldShader))
                            continue;

                        Upgrade(m, newShader, properties);
                        SaveAssetsAndFreeMemory();
                    }
                }
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }
        
        static bool IsMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            return path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
        }

        static bool ShouldUpgradeShader(Material material, string shaderNames)
        {
            if (material == null)
                return false;

            if (material.shader == null)
                return false;

            return shaderNames.Contains(material.shader.name);
        }

        internal void Upgrade(Material material, string newShader, List<PropertyData> properties)
        {
            Material newMaterial;
            newMaterial = UnityEngine.Object.Instantiate(material) as Material;
            newMaterial.shader = Shader.Find(newShader);

            Convert(material, newMaterial, properties);

            material.shader = Shader.Find(newShader);
            material.CopyPropertiesFromMaterial(newMaterial);
            UnityEngine.Object.DestroyImmediate(newMaterial);
        }

        public virtual void Convert(Material srcMaterial, Material dstMaterial, List<PropertyData> properties)
        {
            foreach (var property in properties)
            {
                //Debug.Log(property.dest);
                if (property.dest == null || property.dest == "None")
                    continue;
                
                //Debug.Log(property.dest);
                switch (property.type)
                {
                    case PropertyType.None:
                        break;
                    case PropertyType.Texture: RenameTexture(srcMaterial, dstMaterial, property);
                        break;
                    case PropertyType.Float: RenameFloat(srcMaterial, dstMaterial,property);
                        break;
                    case PropertyType.Vector: RenameVector(srcMaterial, dstMaterial,property);
                        break;
                    case PropertyType.Color: RenameColor(srcMaterial, dstMaterial,property);
                        break;
                }
            }

        }

        #region Rename Property
        void RenameTexture(Material srcMaterial, Material dstMaterial, PropertyData propertyData)
        {
            dstMaterial.SetTextureScale(propertyData.dest, srcMaterial.GetTextureScale(propertyData.source));
            dstMaterial.SetTextureOffset(propertyData.dest, srcMaterial.GetTextureOffset(propertyData.source));
            dstMaterial.SetTexture(propertyData.dest, srcMaterial.GetTexture(propertyData.source));
        }

        void RenameFloat(Material srcMaterial, Material dstMaterial, PropertyData propertyData)
        {
            dstMaterial.SetFloat(propertyData.dest, srcMaterial.GetFloat(propertyData.source));
        }

        void RenameVector(Material srcMaterial, Material dstMaterial, PropertyData propertyData)
        {
            dstMaterial.SetVector(propertyData.dest, srcMaterial.GetVector(propertyData.source));
        }

        void RenameColor(Material srcMaterial, Material dstMaterial, PropertyData propertyData)
        {
            dstMaterial.SetColor(propertyData.dest, srcMaterial.GetColor(propertyData.source));
        }
        #endregion

        static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// StringListSearchProvider class
    /// </summary>
    /// <remarks>
    /// This class is used to create a search window for string list
    /// </remarks>
    public class StringListSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private List<string> listItems;
        private Action<string> onSetIndexCallback;

        /// <summary>
        /// Create a new search provider for string list
        /// (<paramref name="listItems"/>, <paramref name="Callback"/>)
        /// </summary>
        /// <param name="listItems">List of items to be shown in the search window</param>
        /// <param name="Callback">Callback function to be called when an item is selected</param>
        public StringListSearchProvider(List<string> listItems, Action<string> Callback)
        {
            this.listItems = listItems;
            this.onSetIndexCallback = Callback;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> list = new List<SearchTreeEntry>();
            list.Add(new SearchTreeGroupEntry(new GUIContent("Properties List"), 0));

            foreach (var item in listItems)
            {
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(item));
                entry.level = 1;
                entry.userData = item;
                list.Add(entry);
            }

            return list;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            onSetIndexCallback?.Invoke((string)SearchTreeEntry.userData);
            return true;
        }
    }
}