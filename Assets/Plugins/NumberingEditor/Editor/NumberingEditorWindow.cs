using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NumberingEditor
{
    public class NumberingEditorWindow : EditorWindow
    {
        [MenuItem("Window/Numbering")]
        static void Init()
        {
            var window =
                (NumberingEditorWindow) GetWindow(typeof(NumberingEditorWindow), true, "Numbering");
            window.Show();
        }

        private const string BasenameKey = "NumberingEditor.Basename";
        private const string ReplaceKey = "NumberingEditor.Replace";

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
            if (GUILayout.Button("Numbering"))
            {
                this.Numbering(this.selectionObjects);
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

        private void Numbering(GameObject[] objects)
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

            if (match.Groups.Count < 2)
            {
                Debug.LogWarningFormat("Basename regex does not contains regex group: {0}", this.basenameRegexFormat);
                return;
            }

            var basename = match.Groups[1];

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

            var sources = Enumerable.Range(0, objects.Length)
                .Select(index => new {Index = index, Data = objects[index]})
                .ToList();
            var length = sources.Count;

            // NOTE:
            // Cost: O(N*N*log(N)).
            // It's slow, but affordable with minimize swapping cost
            for (var i = 0; i < length; i++)
            {
                var dests = Enumerable.Range(i, length - i)
                    .Select(index => new {Index = index, Data = sources[index].Data})
                    .OrderBy(it => it.Data.name)
                    .ToList();

                var src = sources[i];
                var dst = dests[0];

                if (src.Index == dst.Index) continue;

                sources[dst.Index] = src;
                sources[i] = dst;
                this.Swap(src.Data, dst.Data);
            }

            Selection.instanceIDs = sources.Select(it => it.Data.GetInstanceID()).ToArray();
        }

        private void Swap(GameObject src, GameObject dst)
        {
            var srcIndex = src.transform.GetSiblingIndex();
            var dstIndex = dst.transform.GetSiblingIndex();
            var srcParent = src.transform.parent;
            var dstParent = dst.transform.parent;

            if (dstParent.GetInstanceID() == src.transform.GetInstanceID())
            {
                dst.transform.SetParent(src.transform.parent);
                this.ChangeParentOfChildren(dst.transform, src.transform);
                src.transform.SetParent(dst.transform);
            }
            else if (srcParent.GetInstanceID() == dst.transform.GetInstanceID())
            {
                src.transform.SetParent(dst.transform.parent);
                this.ChangeParentOfChildren(src.transform, dst.transform);
                dst.transform.SetParent(src.transform);
            }
            else
            {
                dst.transform.parent = srcParent;
                src.transform.parent = dstParent;
            }

            if (srcIndex < dstIndex)
            {
                dst.transform.SetSiblingIndex(srcIndex);
                src.transform.SetSiblingIndex(dstIndex);
            }
            else
            {
                src.transform.SetSiblingIndex(dstIndex);
                dst.transform.SetSiblingIndex(srcIndex);
            }
        }

        private void ChangeParentOfChildren(Transform fromParent, Transform toParent)
        {
            var childCount = fromParent.childCount;
            var children = Enumerable.Range(0, childCount).Select(i => fromParent.GetChild(i)).ToList();

            foreach (var child in children)
            {
                child.parent = toParent;
            }
        }
    }
}
