using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NameOrderEditor
{
    public class NameOrderEditorWindow : EditorWindow
    {
        [MenuItem("Window/Name Order")]
        static void Init()
        {
            var window =
                (NameOrderEditorWindow) GetWindow(typeof(NameOrderEditorWindow), true, "Name Order");
            window.Show();
        }

        private const string BasenameKey = "NameOrderEditor.Basename";
        private const string ReplaceKey = "NameOrderEditor.Replace";

        private GameObject[] selectionObjects;
        private string basenameRegexFormat = @"^(\S+)";
        private string replaceFormat = @"{0} {1:D2}";
        private int orderingOffset = 0;

        private void Awake()
        {
            var basenameFormat = EditorPrefs.GetString(BasenameKey);
            this.basenameRegexFormat = string.IsNullOrEmpty(basenameFormat) ? this.basenameRegexFormat : basenameFormat;

            var replaceFormat = EditorPrefs.GetString(this.replaceFormat);
            this.replaceFormat = string.IsNullOrEmpty(replaceFormat) ? this.replaceFormat : replaceFormat;

            this.ApplySelection();
        }

        private void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            // regex
            var regexFormat = EditorGUILayout.TextField("Basename Regex", this.basenameRegexFormat);
            if (regexFormat != this.basenameRegexFormat)
            {
                EditorPrefs.SetString(BasenameKey, regexFormat);
                this.basenameRegexFormat = regexFormat;
            }

            // replace
            var replaceFormat = EditorGUILayout.TextField("Replace Format", this.replaceFormat);
            if (replaceFormat != this.replaceFormat)
            {
                EditorPrefs.SetString(ReplaceKey, replaceFormat);
                this.replaceFormat = replaceFormat;
            }

            // offset
            this.orderingOffset = EditorGUILayout.IntField("Ordering Offset", this.orderingOffset);

            // buttons
            if (GUILayout.Button("Rename to order"))
            {
                this.Rename(this.selectionObjects);
            }

            if (GUILayout.Button("Sort"))
            {
                this.ApplySelection();
                this.Sort(this.selectionObjects);
            }
        }

        private void OnSelectionChange()
        {
            this.ApplySelection();
        }

        private void ApplySelection()
        {
            this.selectionObjects =
                Selection.instanceIDs
                    .Select(id => EditorUtility.InstanceIDToObject(id))
                    .Where(it => it is GameObject)
                    .Cast<GameObject>()
                    .ToArray();
        }

        private void Rename(GameObject[] objects)
        {
            if (objects == null) return;
            if (objects.Length < 1) return;

            var regex = new Regex(this.basenameRegexFormat);
            var match = regex.Match(objects[0].name);

            if (!match.Success)
            {
                Debug.LogWarningFormat("Not matching regex '{0}' to {1}", objects[0].name, this.basenameRegexFormat);
                return;
            }

            if (match.Groups.Count < 1)
            {
                Debug.LogWarningFormat("Basename regex does not contains regex group: {0}", this.basenameRegexFormat);
                return;
            }

            var basename = match.Groups[0];

            for (var i = 0; i < objects.Length; i++)
            {
                var index = i + this.orderingOffset;
                objects[i].name = string.Format(this.replaceFormat, basename, index);
            }
        }

        private void Sort(GameObject[] objects)
        {
            if (objects == null) return;
            if (objects.Length < 1) return;

            var p = 0;
            var sources = objects.Select(it => new {Index = p++, Data = it}).ToList();
            var dests = sources.OrderBy(it => it.Data.name).ToList();

            for (var i = 0; i < dests.Count; i++)
            {
                var src = sources[i];
                var dst = dests[i];

                if (src.Index == dst.Index) continue;

                sources[dests[i].Index] = src;
                sources[src.Index] = dst;
                this.Swap(src.Data, dst.Data);
            }

            Selection.instanceIDs = dests.Select(it => it.Data.GetInstanceID()).ToArray();
        }

        private void Swap(GameObject src, GameObject dst)
        {
            var srcIndex = src.transform.GetSiblingIndex();
            var dstIndex = dst.transform.GetSiblingIndex();
            var srcParent = src.transform.parent;
            var dstParent = dst.transform.parent;

            dst.transform.parent = srcParent;
            src.transform.parent = dstParent;
            dst.transform.SetSiblingIndex(srcIndex);
            src.transform.SetSiblingIndex(dstIndex);
        }
    }
}