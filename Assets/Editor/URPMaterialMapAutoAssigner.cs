#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class URPMaterialMapAutoAssigner : EditorWindow
{
    private static readonly MapDefinition[] MapDefinitions =
    {
        new MapDefinition(
            "_BaseMap",
            new[]
            {
                "AlbedoTransparency",
                "Albedo",
                "BaseColor",
                "BaseMap",
                "Base_Color",
                "Base_Color_Transparency",
                "Diffuse",
                "Color"
            },
            false),

        new MapDefinition(
            "_BumpMap",
            new[]
            {
                "Normal",
                "NormalMap",
                "Normal_OpenGL",
                "Normal_DirectX"
            },
            true),

        new MapDefinition(
            "_MetallicGlossMap",
            new[]
            {
                "MetallicSmoothness",
                "Metallic",
                "Metalness",
                "MetallicGloss",
                "MetallicGlossiness"
            },
            false),

        new MapDefinition(
            "_OcclusionMap",
            new[]
            {
                "AO",
                "Ao",
                "AmbientOcclusion",
                "Ambient_Occlusion",
                "Occlusion"
            },
            false),

        new MapDefinition(
            "_ParallaxMap",
            new[]
            {
                "Height",
                "HeightMap",
                "Displacement",
                "Displace"
            },
            false),

        new MapDefinition(
            "_EmissionMap",
            new[]
            {
                "Emission",
                "EmissionMap",
                "Emissive"
            },
            false)
    };

    private Vector2 scrollPosition;

    [MenuItem("Tools/Materials/URP Material Map Auto Assigner")]
    private static void OpenWindow()
    {
        GetWindow<URPMaterialMapAutoAssigner>(
            "Material Map Assigner");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "URP Material Map Auto Assigner",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "Select multiple Material assets in the Project window, " +
            "then click Apply Maps.\n\n" +
            "The tool finds an already assigned texture, uses its folder, " +
            "and searches that folder for matching Albedo, Normal, " +
            "Metallic/Smoothness, AO, Height and Emission maps.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        int materialCount = GetSelectedMaterials().Count;

        EditorGUILayout.LabelField(
            "Selected Materials",
            materialCount.ToString());

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Supported Map Naming",
            EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.MinHeight(150));

        foreach (MapDefinition definition in MapDefinitions)
        {
            EditorGUILayout.LabelField(
                GetDisplayName(definition.ShaderProperty),
                string.Join(", ", definition.Suffixes));
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        GUI.enabled = materialCount > 0;

        if (GUILayout.Button(
                $"Apply Maps To {materialCount} Material(s)",
                GUILayout.Height(40)))
        {
            ApplyMapsToSelectedMaterials();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Refresh Selection"))
        {
            Repaint();
        }
    }

    private static List<Material> GetSelectedMaterials()
    {
        List<Material> materials = new List<Material>();

        foreach (UnityEngine.Object selectedObject in Selection.objects)
        {
            if (selectedObject is Material material)
            {
                materials.Add(material);
            }
        }

        return materials;
    }

    private static void ApplyMapsToSelectedMaterials()
    {
        List<Material> materials = GetSelectedMaterials();

        if (materials.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No Materials Selected",
                "Please select one or more Material assets in the Project window.",
                "OK");

            return;
        }

        int processedMaterials = 0;
        int assignedMaps = 0;
        int skippedMaterials = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                int mapsAssignedForMaterial = ProcessMaterial(material);

                if (mapsAssignedForMaterial > 0)
                {
                    processedMaterials++;
                    assignedMaps += mapsAssignedForMaterial;
                }
                else
                {
                    skippedMaterials++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Material Maps Applied",
            $"Materials processed: {processedMaterials}\n" +
            $"Maps assigned: {assignedMaps}\n" +
            $"Materials skipped: {skippedMaterials}",
            "OK");
    }

    private static int ProcessMaterial(Material material)
    {
        List<Texture> assignedTextures =
            GetAssignedTextures(material);

        if (assignedTextures.Count == 0)
        {
            Debug.LogWarning(
                $"[URP Material Map Assigner] " +
                $"No assigned textures found on material '{material.name}'.",
                material);

            return 0;
        }

        Texture seedTexture = FindBestSeedTexture(
            material,
            assignedTextures);

        if (seedTexture == null)
        {
            return 0;
        }

        string seedTexturePath =
            AssetDatabase.GetAssetPath(seedTexture);

        if (string.IsNullOrEmpty(seedTexturePath))
        {
            return 0;
        }

        string folderPath =
            Path.GetDirectoryName(seedTexturePath);

        if (string.IsNullOrEmpty(folderPath))
        {
            return 0;
        }

        string seedFileName =
            Path.GetFileNameWithoutExtension(seedTexturePath);

        string baseName =
            RemoveKnownMapSuffix(seedFileName);

        if (string.IsNullOrEmpty(baseName))
        {
            return 0;
        }

        string[] folderAssets = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { folderPath });

        int assignedCount = 0;

        foreach (MapDefinition definition in MapDefinitions)
        {
            if (!material.HasProperty(
                    definition.ShaderProperty))
            {
                continue;
            }

            Texture existingTexture =
                material.GetTexture(
                    definition.ShaderProperty);

            Texture matchingTexture =
                FindMatchingTexture(
                    folderAssets,
                    baseName,
                    definition);

            if (matchingTexture == null)
            {
                continue;
            }

            if (existingTexture == matchingTexture)
            {
                continue;
            }

            Undo.RecordObject(
                material,
                "Assign Material Texture Maps");

            material.SetTexture(
                definition.ShaderProperty,
                matchingTexture);

            if (definition.IsNormalMap)
            {
                SetNormalMapImportSettings(
                    matchingTexture);
            }

            assignedCount++;

            Debug.Log(
                $"[URP Material Map Assigner] " +
                $"Assigned '{matchingTexture.name}' → " +
                $"'{definition.ShaderProperty}' on '{material.name}'.",
                material);
        }

        if (assignedCount > 0)
        {
            EditorUtility.SetDirty(material);
        }

        return assignedCount;
    }

    private static List<Texture> GetAssignedTextures(
        Material material)
    {
        List<Texture> textures =
            new List<Texture>();

        foreach (MapDefinition definition in MapDefinitions)
        {
            if (!material.HasProperty(
                    definition.ShaderProperty))
            {
                continue;
            }

            Texture texture =
                material.GetTexture(
                    definition.ShaderProperty);

            if (texture != null &&
                !textures.Contains(texture))
            {
                textures.Add(texture);
            }
        }

        return textures;
    }

    private static Texture FindBestSeedTexture(
        Material material,
        List<Texture> assignedTextures)
    {
        foreach (MapDefinition definition in MapDefinitions)
        {
            if (!material.HasProperty(
                    definition.ShaderProperty))
            {
                continue;
            }

            Texture texture =
                material.GetTexture(
                    definition.ShaderProperty);

            if (texture != null)
            {
                return texture;
            }
        }

        return assignedTextures.Count > 0
            ? assignedTextures[0]
            : null;
    }

    private static Texture FindMatchingTexture(
        string[] folderAssets,
        string baseName,
        MapDefinition definition)
    {
        foreach (string guid in folderAssets)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            string fileName =
                Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            if (!IsMatchingMapName(
                    fileName,
                    baseName,
                    definition.Suffixes))
            {
                continue;
            }

            Texture texture =
                AssetDatabase.LoadAssetAtPath<Texture>(
                    assetPath);

            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static bool IsMatchingMapName(
        string fileName,
        string baseName,
        string[] suffixes)
    {
        if (string.Equals(
                fileName,
                baseName,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (string suffix in suffixes)
        {
            string expectedName =
                baseName + "_" + suffix;

            if (string.Equals(
                    fileName,
                    expectedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string compactExpectedName =
                baseName + suffix;

            if (string.Equals(
                    fileName,
                    compactExpectedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveKnownMapSuffix(
        string fileName)
    {
        foreach (MapDefinition definition in MapDefinitions)
        {
            foreach (string suffix in definition.Suffixes)
            {
                string suffixWithSeparator =
                    "_" + suffix;

                if (fileName.EndsWith(
                        suffixWithSeparator,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return fileName.Substring(
                        0,
                        fileName.Length -
                        suffixWithSeparator.Length);
                }

                if (fileName.EndsWith(
                        suffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    int suffixStart =
                        fileName.Length - suffix.Length;

                    if (suffixStart > 0)
                    {
                        char previousCharacter =
                            fileName[suffixStart - 1];

                        if (previousCharacter == '_' ||
                            char.IsLetterOrDigit(previousCharacter))
                        {
                            return fileName.Substring(
                                0,
                                suffixStart).TrimEnd('_');
                        }
                    }
                }
            }
        }

        return fileName;
    }

    private static void SetNormalMapImportSettings(
        Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        string assetPath =
            AssetDatabase.GetAssetPath(texture);

        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath)
            as TextureImporter;

        if (importer == null)
        {
            return;
        }

        if (importer.textureType !=
            TextureImporterType.NormalMap)
        {
            importer.textureType =
                TextureImporterType.NormalMap;

            importer.SaveAndReimport();
        }
    }

    private static string GetDisplayName(
        string shaderProperty)
    {
        switch (shaderProperty)
        {
            case "_BaseMap":
                return "Base Map";

            case "_BumpMap":
                return "Normal Map";

            case "_MetallicGlossMap":
                return "Metallic / Smoothness";

            case "_OcclusionMap":
                return "Ambient Occlusion";

            case "_ParallaxMap":
                return "Height / Displacement";

            case "_EmissionMap":
                return "Emission";

            default:
                return shaderProperty;
        }
    }

    private sealed class MapDefinition
    {
        public readonly string ShaderProperty;
        public readonly string[] Suffixes;
        public readonly bool IsNormalMap;

        public MapDefinition(
            string shaderProperty,
            string[] suffixes,
            bool isNormalMap)
        {
            ShaderProperty = shaderProperty;
            Suffixes = suffixes;
            IsNormalMap = isNormalMap;
        }
    }
}

#endif