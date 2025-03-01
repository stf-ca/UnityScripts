#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

// copyright 2025 ---------- rd'

namespace rd.BTUtility
{
    public class BTObjectPhotocopy : EditorWindow
    {
        private GameObject selectedObject;
        private string mainFolderName = "MyAvatarCopy";
        private string itemSuffix = "";
        private bool copyLogic = true;
        private bool copyMaterials = true;
        private bool copyTextures = true;
        private bool copyScripts = true;
        private bool moveAside = true;
        private bool backupScene = true;
        private float progress = 0f;
        private string progressMessage = "";
        private bool copyCompleted = false;
        private bool encounteredError = false;
        private GameObject lastSelectedObject = null;

        private string version = "1.2.0";
        private string versiondet = "alpha";
        
        private GUIContent discordButton = null;
        private GUIContent webButton = null;
        private static string discordURL = "https://discord.rd.art/";
        private static string webURL = "https://rd.art/";

        [MenuItem("Tools/rd/BTObjectPhotocopy")]
        static void Init()
        {
            BTObjectPhotocopy window = (BTObjectPhotocopy)EditorWindow.GetWindow(typeof(BTObjectPhotocopy));
            CreateRequiredFolders();
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("- BT Object Photocopy - https://rd.art - " + version + " " + versiondet, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Unlock all locked textures before proceeding.", MessageType.Warning);

            // selection auto-update
            EditorGUI.BeginChangeCheck();
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Selected Object", selectedObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                Selection.activeObject = selectedObject;
                lastSelectedObject = selectedObject;
            }
            if (Selection.activeGameObject != lastSelectedObject)
            {
                selectedObject = Selection.activeGameObject;
                lastSelectedObject = selectedObject;
                Repaint();
            }
            // -=-=

            // gui
            GUILayout.Label("Naming", EditorStyles.boldLabel);
            mainFolderName = EditorGUILayout.TextField("Main Folder Name", mainFolderName);
            itemSuffix = EditorGUILayout.TextField("Item Suffix", itemSuffix);

            GUILayout.Label("Items to Photocopy", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal(); GUILayout.Space(50); copyLogic = EditorGUILayout.Toggle("Copy Logic", copyLogic); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Space(50); copyMaterials = EditorGUILayout.Toggle("Materials", copyMaterials); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Space(50); copyTextures = EditorGUILayout.Toggle("Textures", copyTextures); GUILayout.EndHorizontal();

            GUILayout.Label("Additional Parameters", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal(); GUILayout.Space(50); copyScripts = EditorGUILayout.Toggle("Copy Scripts", copyScripts); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Space(50); moveAside = EditorGUILayout.Toggle("Move Aside", moveAside); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Space(50); backupScene = EditorGUILayout.Toggle("Backup Scene", backupScene); GUILayout.EndHorizontal();
            // -=-=

            // photocopy dupe
            if (GUILayout.Button("Photocopy Object"))
            {
                if (CheckForExistingCopy())
                {
                    int option = EditorUtility.DisplayDialogComplex("Duplicate found!",
                        "A copy of this object has already been made. Proceeding will overwrite the copied object and associated resources.",
                        "Rename Old", "Cancel", "Proceed Anyway");
                    if (option == 1) return; // Cancel
                    if (option == 0) RenameOldCopy(); // Rename Old
                }

                progress = 0f;
                copyCompleted = false;
                encounteredError = false;
                PhotocopyObject();
                copyCompleted = true;
            }
            // -=-=

            if (copyCompleted)
            {
                EditorGUILayout.HelpBox("Photocopy completed successfully!", MessageType.Info);
            }

            // progress bar (wip)
            Rect progressRect = new Rect(3, GUILayoutUtility.GetLastRect().yMax + 5, position.width - 6, 20);
            Color originalColor = GUI.color;
            if (encounteredError) GUI.color = Color.red;
            else if (copyCompleted)
            {
                GUI.color = Color.green;
                progressMessage = "Complete";
            }
            EditorGUI.ProgressBar(progressRect, progress, progressMessage);
            GUI.color = originalColor;
            GUILayout.Space(25);
            // -=-=

            // cleanup
            if (GUILayout.Button("Cleanup Photocopies"))
            {
                if (EditorUtility.DisplayDialog("Confirm Cleanup",
                    "This will delete all photocopied objects, files, and scene backups. Proceed?",
                    "Yes", "No"))
                {
                    BackupCurrentScene(); // todo: hold bu file
                    CleanupPhotocopies();
                    CreateRequiredFolders();
                }
            }
            // -=-=

            DisplaySummary();
        }

        // folders; bug avoidance
        static void CreateRequiredFolders()
        {
            string[] requiredFolders = new[]
            {
                "Assets/BT",
                "Assets/BT/ObjectPhotocopy",
                "Assets/BT/ObjectPhotocopy/SceneBackups",
                "Assets/BT/ObjectPhotocopy/Copies"
            };

            foreach (string folder in requiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parentFolder = Path.GetDirectoryName(folder);
                    string folderName = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
        }

        private void CleanupPhotocopies()
        {
            // delete photocopies
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> duplicatesToDelete = new List<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.EndsWith("_Copy"))
                {
                    duplicatesToDelete.Add(obj);
                }
            }

            foreach (GameObject obj in duplicatesToDelete)
            {
                DestroyImmediate(obj);
            }

            // delete folders
            string copiedAssetsPath = "Assets/BT/ObjectPhotocopy/Copies";
            if (Directory.Exists(copiedAssetsPath))
            {
                Directory.Delete(copiedAssetsPath, true);
                File.Delete(copiedAssetsPath + ".meta");
            }

            // delete scenes
            string backupFolderPath = "Assets/BT/ObjectPhotocopy/SceneBackups";
            if (Directory.Exists(backupFolderPath))
            {
                string[] files = Directory.GetFiles(backupFolderPath, "*.unity", SearchOption.AllDirectories);
                // do not delete enabled current scene backup (wip)
                foreach (string file in files)
                {
                    File.Delete(file);
                    File.Delete(file + ".meta");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("All photocopies and backups have been cleared.");
        }

        // previous dupes
        private bool CheckForExistingCopy()
        {
            string potentialCopyName = $"{selectedObject.name}_Copy";
            GameObject potentialCopy = GameObject.Find(potentialCopyName);
            return potentialCopy != null;
        }

        // rename to keep old dupes
        private void RenameOldCopy()
        {
            string oldFolderPath = $"Assets/BT/ObjectPhotocopy/Copies/{mainFolderName}";
            if (Directory.Exists(oldFolderPath))
            {
                int counter = 1;
                string newFolderPath;
                do
                {
                    newFolderPath = $"{oldFolderPath}_Old_{counter}";
                    counter++;
                } while (Directory.Exists(newFolderPath));

                AssetDatabase.MoveAsset(oldFolderPath, newFolderPath);
                Debug.Log($"Renamed existing copy folder to {newFolderPath}");
            }
        }

        // dupe function
        private void PhotocopyObject()
        {
            if (selectedObject == null)
            {
                Debug.LogError("No object selected!");
                return;
            }

            try
            {
                if (backupScene)
                {
                    BackupCurrentScene();
                }

                string parentFolder = $"Assets/BT/ObjectPhotocopy/Copies/{mainFolderName}";
                CreateFolderStructure(parentFolder);

                GameObject duplicateObject = Instantiate(selectedObject);
                duplicateObject.name = $"{selectedObject.name}_Copy";

                if (moveAside)
                {
                    duplicateObject.transform.position += Vector3.right;
                }

                float taskCount = 4.0f; // # tasks
                float currentTask = 0.0f; // index

                Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();

                // transfer data
                if (copyMaterials || copyTextures)
                {
                    TraverseAndCopyComponents(selectedObject, duplicateObject, parentFolder, materialMap);
                }
                currentTask++;
                UpdateProgress("Copying materials and textures", currentTask / taskCount);

                if (copyLogic)
                {
                    CopyLogicComponents(selectedObject, duplicateObject);
                }
                currentTask++;
                UpdateProgress("Copying logic components", currentTask / taskCount);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateMaterialReplacement(duplicateObject, materialMap);
                currentTask++;
                UpdateProgress("Refreshing assets", currentTask / taskCount);
                // -=-=
            }
            catch (System.Exception ex)
            {
                encounteredError = true;
                Debug.LogError("Error during photocopy: " + ex.Message);
            }
        }

        private void UpdateProgress(string message, float progressValue)
        {
            progress = progressValue;
            progressMessage = message;
            Repaint();
        }

        // transfer of components
        private void TraverseAndCopyComponents(GameObject original, GameObject clone, string parentFolder, Dictionary<Material, Material> materialMap)
        {
            string materialsFolder = $"{parentFolder}/Materials";
            string texturesFolder = $"{parentFolder}/Textures";

            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    try
                    {
                        if (copyMaterials && mat != null)
                        {
                            if (!materialMap.TryGetValue(mat, out Material newMaterial))
                            {
                                // map new mat
                                newMaterial = new Material(mat);
                                string matName = $"{mat.name}{(string.IsNullOrEmpty(itemSuffix) ? "" : "_")}{itemSuffix}";
                                string matPath = $"{materialsFolder}/{matName}_Copy.mat";

                                if (!File.Exists(matPath))
                                {
                                    AssetDatabase.CreateAsset(newMaterial, matPath);
                                }

                                materialMap[mat] = newMaterial;

                                if (copyTextures)
                                {
                                    CopyTextures(mat, newMaterial, texturesFolder);
                                }
                            }
                            newMaterials[i] = materialMap[mat];
                        }
                        else
                        {
                            newMaterials[i] = mat;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        encounteredError = true; // call error
                        Debug.LogError($"Error copying material {mat.name}: {ex.Message}");
                        newMaterials[i] = mat; // fallback to OG mat
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        // copy texture items
        private void CopyTextures(Material originalMat, Material newMat, string texturesFolder)
        {
            foreach (string propertyName in originalMat.GetTexturePropertyNames())
            {
                Texture texture = originalMat.GetTexture(propertyName);

                if (texture != null)
                {
                    try
                    {
                        string path = AssetDatabase.GetAssetPath(texture);
                        string extension = Path.GetExtension(path);
                        string textureName = $"{texture.name}{(string.IsNullOrEmpty(itemSuffix) ? "" : "_")}{itemSuffix}";
                        string newPath = $"{texturesFolder}/{textureName}_Copy{extension}";
                        if (!File.Exists(newPath))
                        {
                            AssetDatabase.CopyAsset(path, newPath);
                        }
                        Texture newTexture = (Texture)AssetDatabase.LoadAssetAtPath(newPath, typeof(Texture));
                        newMat.SetTexture(propertyName, newTexture);
                    }
                    catch (System.Exception ex)
                    {
                        encounteredError = true;
                        Debug.LogError($"Error copying texture {texture.name}: {ex.Message}");
                    }
                }
            }

            newMat.CopyPropertiesFromMaterial(originalMat);
        }

        // ensure logic is transferred (deprecated)
        private void CopyLogicComponents(GameObject original, GameObject clone)
        {
            Component[] components = original.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component is Transform || clone.GetComponent(component.GetType()) != null)
                    continue;

                Component newComponent = clone.AddComponent(component.GetType());
                CopyComponentValues(component, newComponent);
            }
        }

        // properties
        private void CopyComponentValues(Component original, Component copy)
        {
            var type = original.GetType();
            var fields = type.GetFields();
            var properties = type.GetProperties();

            foreach (var field in fields)
            {
                if (field.IsPublic && !field.IsStatic)
                {
                    field.SetValue(copy, field.GetValue(original));
                }
            }

            foreach (var property in properties)
            {
                if (property.CanWrite && property.CanRead && property.Name != "name")
                {
                    property.SetValue(copy, property.GetValue(original, null), null);
                }
            }
        }

        // dupe old scene in case of error saving
        private void BackupCurrentScene()
        {
            string backupFolder = "Assets/BT/ObjectPhotocopy/SceneBackups";

            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            string scenePath = EditorSceneManager.GetActiveScene().path;
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string uniqueBackupPath = GetUniqueSceneBackupPath(backupFolder, sceneName);
            File.Copy(scenePath, uniqueBackupPath);
            Debug.Log($"Scene backed up to {uniqueBackupPath}");
        }

        private string GetUniqueSceneBackupPath(string backupFolder, string sceneName)
        {
            int counter = 1;
            string backupPath;
            do
            {
                backupPath = $"{backupFolder}/{sceneName}_backup_{counter}.unity";
                counter++;
            } while (File.Exists(backupPath));
            return backupPath;
        }

        // folders; bug avoidance
        private void CreateFolderStructure(string parentFolder)
        {
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets/BT/ObjectPhotocopy/Copies", mainFolderName);
            }

            string[] subfolders = new[] { "Materials", "Textures" };

            foreach (string subfolder in subfolders)
            {
                string fullPath = $"{parentFolder}/{subfolder}";
                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, subfolder);
                }
            }
        }

        // check unchanged
        private void ValidateMaterialReplacement(GameObject duplicateObject, Dictionary<Material, Material> materialMap)
        {
            List<string> unchangedMaterialsInfo = new List<string>();
            Renderer[] renderers = duplicateObject.GetComponentsInChildren<Renderer>(true);

            // check for unchanged in case of error and add to warn
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && !materialMap.ContainsValue(mat))
                    {
                        unchangedMaterialsInfo.Add($"{mat.name} on {renderer.gameObject.name}");
                    }
                }
            }

            // warn if exists
            if (unchangedMaterialsInfo.Count > 0)
            {
                encounteredError = true;
                progressMessage = "Some materials were not replaced!";
                foreach (string info in unchangedMaterialsInfo)
                {
                    Debug.LogWarning($"Unchanged Material: {info}");
                }
            }
            else
            {
                progressMessage = "All materials replaced successfully.";
            }
            Repaint();
        }

        // footer
        private void DisplaySummary()
        {
            EditorGUILayout.Space();

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
    }
}
#endif
