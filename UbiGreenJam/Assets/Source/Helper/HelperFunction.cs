using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HelperFunction
{
    public static string InsertDotsEvery3CharInNum(string inputNum)
    {
        var digits = new string(inputNum.Where(char.IsDigit).ToArray());

        if (digits.Length == 0) return inputNum;

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
}
