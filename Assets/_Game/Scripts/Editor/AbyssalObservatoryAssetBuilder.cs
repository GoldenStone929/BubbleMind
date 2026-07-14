using System;
using UnityEditor;
using UnityEngine;

namespace GenericGachaRPG.Editor
{
    public static class AbyssalObservatoryAssetBuilder
    {
        public const string BackdropMaterialPath =
            "Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Materials/Resources/MAT_AbyssalObservatoryBackdrop.mat";

        public static Material EnsureAssets()
        {
            EnsureFolder("Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Materials/Resources");

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                throw new InvalidOperationException("No supported unlit shader is available for the arena backdrop.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(BackdropMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MAT_AbyssalObservatoryBackdrop" };
                AssetDatabase.CreateAsset(material, BackdropMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
