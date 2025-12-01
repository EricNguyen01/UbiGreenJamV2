using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HelperFunction
{
    public static string FormatCostWithDots(string costText)
    {
        var digits = new string(costText.Where(char.IsDigit).ToArray());

        if (digits.Length == 0) return costText;

        var chars = digits.Reverse().ToList();

        var result = new List<char>();

        for (int i = 0; i < chars.Count; i++)
        {
            if (i > 0 && i % 3 == 0)
                result.Add('.');

            result.Add(chars[i]);
        }

        result.Reverse();

        return new string(result.ToArray());
    }

    public static void AddUniqueMaterial(Renderer renderer, Material newMat)
    {
        if (!renderer || !newMat) return;

        var mats = renderer.materials;

        if (mats.Contains(newMat)) return;

        System.Array.Resize(ref mats, mats.Length + 1);

        mats[mats.Length - 1] = newMat;

        renderer.materials = mats;
    }

    public static void SetLayerDeep(GameObject root, int newLayer)
    {
        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            current.gameObject.layer = newLayer;

            foreach (Transform child in current)
                stack.Push(child);
        }
    }
}
