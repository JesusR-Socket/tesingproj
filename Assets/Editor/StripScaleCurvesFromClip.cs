using UnityEditor;
using UnityEngine;

public static class StripScaleCurvesFromClip
{
    [MenuItem("Tools/Animation/Strip Scale Curves From Selected Clips")]
    private static void StripScaleCurves()
    {
        Object[] selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        int processed = 0;

        foreach (Object obj in selected)
        {
            if (obj is not AnimationClip clip)
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(clip, "Strip Scale Curves");

            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                if (binding.propertyName.Contains("m_LocalScale"))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                }
            }

            EditorUtility.SetDirty(clip);
            processed++;
            Debug.Log($"Removed scale curves from: {clip.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Done. Processed clips: {processed}");
    }
}