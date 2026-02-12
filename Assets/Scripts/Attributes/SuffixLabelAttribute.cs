using System;
using UnityEngine;

namespace ChristinaCreatesGames.Attributes
{
    /// <summary>
    /// Minimal runtime attribute that matches the inspector attribute usage `SuffixLabel("sec", true)`.
    /// This prevents CS0246 when the project does not include a third-party package that provides this attribute.
    /// If you want custom inspector rendering, add an Editor `PropertyDrawer` in an `Editor` folder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SuffixLabelAttribute : PropertyAttribute
    {
        public string Suffix { get; }
        public bool AlignRight { get; }

        public SuffixLabelAttribute(string suffix, bool alignRight = false)
        {
            Suffix = suffix;
            AlignRight = alignRight;
        }
    }
}