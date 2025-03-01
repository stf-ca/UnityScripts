#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

// copyright 2025 ---------- stf

namespace rd.BTUtility
{
    public class BTPolyCounterWindow : EditorWindow
    {
        [MenuItem("Tools/rd/BTUtility")]
        public static void ShowWindow()
        {
            GetWindow<BTPolyCounterWindow>("BT GameObj Utility");
        }

        private Vector2 scrollPos;
        private List<AnalyzedObject> analyzedObjects = new List<AnalyzedObject>();
        private GameObject previousSelection;
        private string[] sortOptions = { "Object Name", "Triangles", "Materials", "Textures", "BlendShapes", "Memory Size" };
        private int selectedSortOption = 1;
        private bool hideZeroSizeItems = true;

        private static string version = "1.3.1";
        private static string versiondet = "release";

        private GUIContent discordButton = null;
        private GUIContent webButton = null;
        private static string discordURL = "https://discord.rd.art/";
        private static string webURL = "https://rd.art/";

        private void OnGUI()
        {
            GUILayout.Label("- BT GameObject Statistics - https://rd.art - " + version + " " + versiondet, EditorStyles.boldLabel);

            // selection auto-update (validation)
            if (Selection.activeGameObject != previousSelection)
            {
                previousSelection = Selection.activeGameObject;
                if (previousSelection != null)
                {
                    AnalyzeSelectedGameObject();
                }
            }
            // -=-=

            selectedSortOption = EditorGUILayout.Popup("Sort by", selectedSortOption, sortOptions);
            hideZeroSizeItems = EditorGUILayout.Toggle("Hide Zero Size Items", hideZeroSizeItems);

            // pick
            if (analyzedObjects != null && analyzedObjects.Count > 0)
            {
                SortAnalyzedObjects();
                DisplayResults();
                DisplaySummary();
            }
            // -=-=
        }

        private void DisplayResults()
        {
            // gui ui
            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            string namelabel = "Please select a GameObject";
            if (previousSelection != null) namelabel = "Analysis Results of: " + previousSelection.name;
            GUILayout.Label(namelabel, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawColumnHeader("Object Name", 200);
            DrawColumnHeader("Triangles", 70);
            DrawColumnHeader("Materials", 70);
            DrawColumnHeader("Textures", 70);
            DrawColumnHeader("BlendShapes", 100);
            DrawColumnHeader("Disk Size (MB)", 130);
            EditorGUILayout.EndHorizontal();
            // -=-=

            // populate
            bool isGray = false;
            foreach (var obj in analyzedObjects)
            {
                if (hideZeroSizeItems && obj.memorySize == 0)
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = isGray ? Color.gray : Color.white;

                if (GUILayout.Button(obj.name, GUILayout.Width(200)))
                {
                    Selection.activeGameObject = obj.reference;
                }

                GUILayout.Label(obj.triangleCount.ToString(), GUILayout.Width(70));
                GUILayout.Label(obj.materialCount.ToString(), GUILayout.Width(70));
                GUILayout.Label(obj.textureCount.ToString(), GUILayout.Width(70));
                GUILayout.Label(obj.blendShapeCount.ToString(), GUILayout.Width(100));
                GUILayout.Label((obj.memorySize / (1024.0f * 1024.0f)).ToString("F2"), GUILayout.Width(130)); // convert to MB

                EditorGUILayout.EndHorizontal();
                isGray = !isGray;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
            // -=-=
        }

        private void DrawColumnHeader(string name, float width)
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { background = Texture2D.grayTexture }
            };
            GUILayout.Label(name, headerStyle, GUILayout.Width(width));
        }

        // selection validation stage 2
        private void AnalyzeSelectedGameObject()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Please select a GameObject first.");
                analyzedObjects = null;
                return;
            }

            analyzedObjects = new List<AnalyzedObject>();
            AnalyzeGameObject(selected);
        }

        private void AnalyzeGameObject(GameObject gameObject)
        {
            Mesh mesh = null;
            Renderer renderer = null;
            int blendShapeCount = 0;
            long meshMemorySize = 0;
            long textureMemorySize = 0;

            // collect information pre-mem
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
                renderer = gameObject.GetComponent<Renderer>();
                meshMemorySize = CalculateMeshMemorySize(mesh);
            }

            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
                renderer = skinnedMeshRenderer;
                blendShapeCount = mesh != null ? mesh.blendShapeCount : 0;
                meshMemorySize = CalculateMeshMemorySize(mesh);
            }

            AnalyzedObject obj = new AnalyzedObject
            {
                name = gameObject.name,
                triangleCount = mesh != null ? mesh.triangles.Length / 3 : 0,
                blendShapeCount = blendShapeCount,
                materialCount = 0,
                textureCount = 0,
                memorySize = meshMemorySize,
                reference = gameObject
            };
            // -=-=

            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                HashSet<Texture> textureSet = new HashSet<Texture>();

                obj.materialCount = materials.Length;
                foreach (var mat in materials)
                {
                    if (mat != null)
                    {
                        foreach (var propertyName in mat.GetTexturePropertyNames())
                        {
                            Texture texture = mat.GetTexture(propertyName);
                            if (texture != null && textureSet.Add(texture))
                            {
                                textureMemorySize += CalculateTextureMemorySize(texture);
                            }
                        }
                    }
                }

                obj.textureCount = textureSet.Count;
            }

            obj.memorySize += textureMemorySize;
            analyzedObjects.Add(obj);

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeGameObject(child.gameObject);
            }
        }

        // mesh estimation
        private long CalculateMeshMemorySize(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0;
            }
            long size = 0;
            size += mesh.vertexCount * 12; // estimated, as each vertex = Vector3 (~12 bytes)
            size += mesh.triangles.Length * 2; // for triangle indices (2 bytes / 16 bit)
            return size;
        }

        // texture estimation
        private long CalculateTextureMemorySize(Texture texture)
        {
            if (texture == null)
            {
                return 0;
            }

            int width = texture.width;
            int height = texture.height;

            return ApproximateCompressedSize(width, height, true);
        }

        // basic heuristic approximation
        private long ApproximateCompressedSize(int width, int height, bool applyCompression)
        {
            return (applyCompression) ? (long)(width * height * 0.5) : (long)(width * height * 4);
        }

        // types of sort
        private void SortAnalyzedObjects()
        {
            switch (selectedSortOption)
            {
                case 0:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.name).ToList();
                    break;
                case 1:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.triangleCount).ToList();
                    break;
                case 2:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.materialCount).ToList();
                    break;
                case 3:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.textureCount).ToList();
                    break;
                case 4:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.blendShapeCount).ToList();
                    break;
                case 5:
                    analyzedObjects = analyzedObjects.OrderByDescending(x => x.memorySize).ToList();
                    break;
                default:
                    break;
            }
        }

        // footer
        private void DisplaySummary()
        {
            if (analyzedObjects == null || analyzedObjects.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Summary", EditorStyles.boldLabel);

            int totalTriangles = analyzedObjects.Sum(obj => obj.triangleCount);
            int totalMaterials = analyzedObjects.Sum(obj => obj.materialCount);
            int totalTextures = analyzedObjects.Sum(obj => obj.textureCount);
            int totalBlendShapes = analyzedObjects.Sum(obj => obj.blendShapeCount);
            long totalMemorySize = analyzedObjects.Sum(obj => obj.memorySize);

            GUILayout.Label($"Total Triangles: {totalTriangles}");
            GUILayout.Label($"Total Materials: {totalMaterials}");
            GUILayout.Label($"Total Textures: {totalTextures}");
            GUILayout.Label($"Total BlendShapes: {totalBlendShapes}");
            GUILayout.Label($"Total Memory Size: {(totalMemorySize / (1024.0f * 1024.0f)):F2} MB");
            GUILayout.Label($"https://rd.art - Memory size is approximate.");

            // buttons
            discordButton = new GUIContent(" Join Discord", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image);
            webButton = new GUIContent(" Open Website", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image);
            GUILayout.BeginHorizontal( GUILayout.ExpandWidth( true ) );
			{
                if (GUILayout.Button(discordButton, GUILayout.ExpandWidth(true)))
                {
                    Application.OpenURL(discordURL);
                }
                if (GUILayout.Button(webButton, GUILayout.ExpandWidth(true)))
                {
                    Application.OpenURL(webURL);
                }
            }
            GUILayout.EndHorizontal();
            // -=-=
        }

        private class AnalyzedObject
        {
            public string name;
            public int triangleCount;
            public int materialCount;
            public int textureCount;
            public int blendShapeCount;
            public long memorySize;
            public GameObject reference;
        }
    }
}
#endif
