using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// InspectableAttributeのついたコンポーネントをヒエラルキーから探して
/// InspectableAttributeの追加Field, Propertyを探しInspectorへ表示する
/// </summary>
[CustomEditor(typeof(DebuggingInspector))]
public class DebuggingInspectorDrawer : Editor
{
    /// <summary>
    /// The empty options.
    /// </summary>
    readonly static GUILayoutOption[] emptyOptions = new GUILayoutOption[0];

    /// <summary>
    /// The type of the inspectable.
    /// </summary>
    readonly static Type InspectableType = typeof(InspectableAttribute);

    /// <summary>
    /// Typeに応じたInspector表示ロジックを格納
    /// 拡張したい場合はこいつに追加する
    /// </summary>
    /// <value>The inspect action dict.</value>
    public static Dictionary<Type, Action<string, object>> InspectActionDict { get; private set; }

    /// <summary>
    /// The is auto refresh.
    /// </summary>
    bool isAutoRefresh = true;

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="DebuggingInspectorDrawer"/> auto re draw.
    /// </summary>
    /// <value><c>true</c> if auto re draw; otherwise, <c>false</c>.</value>
    bool IsAutoRefresh { get { return isAutoRefresh; } set { isAutoRefresh = value; } }

    /// <summary>
    /// Initializes the <see cref="DebuggingInspectorDrawer"/> class.
    /// </summary>
    static DebuggingInspectorDrawer()
    {
        InspectActionDict = new Dictionary<Type, Action<string, object>>();

        // Registe default actions
        RegisterInspectAction(typeof(Int32), (name, obj) => EditorGUILayout.IntField(name, (Int32) obj, emptyOptions));
        RegisterInspectAction(typeof(String), (name, obj) => EditorGUILayout.TextField(name, (String) obj, emptyOptions));
        RegisterInspectAction(typeof(Boolean), (name, obj) => EditorGUILayout.Toggle(name, (Boolean) obj, emptyOptions));
        RegisterInspectAction(typeof(Single), (name, obj) => EditorGUILayout.FloatField(name, (Single) obj, emptyOptions));
        RegisterInspectAction(typeof(Vector2), (name, obj) => EditorGUILayout.Vector2Field(name, (Vector2) obj, emptyOptions));
        RegisterInspectAction(typeof(Vector3), (name, obj) => EditorGUILayout.Vector3Field(name, (Vector3) obj, emptyOptions));
        RegisterInspectAction(typeof(Vector4), (name, obj) => EditorGUILayout.Vector4Field(name, (Vector4) obj, emptyOptions));
        RegisterInspectAction(typeof(Color), (name, obj) => EditorGUILayout.ColorField(name, (Color) obj, emptyOptions));
    }

    /// <summary>
    /// Registers the inspect action.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="action">Action.</param>
    public static void RegisterInspectAction(Type type, Action<string, object> action)
    {
        InspectActionDict.Add(type, action);
    }

    /// <summary>
    /// Scenes the root transforms.
    /// </summary>
    /// <returns>The root transforms.</returns>
    static IEnumerable<Transform> SceneRootTransforms()
    {
        var prop = new HierarchyProperty(HierarchyType.GameObjects);
        var expanded = new int[0];
        while (prop.Next(expanded))
        {
            var go = prop.pptrValue as GameObject;
            yield return go.transform;
        }
    }

    /// <summary>
    /// Gets the inspectable components.
    /// </summary>
    /// <returns>The inspectable components.</returns>
    /// <param name="transform">Transform.</param>
    static IEnumerable<Component> GetInspectableComponents(Transform transform)
    {
        foreach (var co in transform.GetComponentsInChildren(typeof(Component)))
        {
            if (IsInspectableComponent(co))
            {
                yield return co;
            }
        }
    }

    /// <summary>
    /// Determines if is inspectable component the specified component.
    /// </summary>
    /// <returns><c>true</c> if is inspectable component the specified component; otherwise, <c>false</c>.</returns>
    /// <param name="component">Component.</param>
    static bool IsInspectableComponent(Component component)
    {
        if (component == null)
        {
            return false;
        }
        var type = component.GetType();
        var attrs = type.GetCustomAttributes(false);
        return attrs.Any(attr => attr.GetType() == InspectableType);
    }

    /// <summary>
    /// Gets all inspectable components.
    /// </summary>
    /// <returns>The all inspectable components.</returns>
    static IEnumerable<Component> GetAllInspectableComponents()
    {
        foreach (var transform in SceneRootTransforms())
        {
            foreach (var co in GetInspectableComponents(transform))
            {
                yield return co;
            }
        }
    }

    /// <summary>
    /// 受け取ったobjectの値をインスペクターへ表示
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="type">Type.</param>
    /// <param name="_object">Object.</param>
    static void InspectObject(String name, Type type, object _object)
    {
        // Registered types in InspectActionDict.
        if (InspectActionDict.ContainsKey(type))
        {
            InspectActionDict[type].Invoke(name, _object);
            return;
        }

        // Unity object
        var unityObject = _object as UnityEngine.Object;
        if (unityObject != null)
        {
            EditorGUILayout.ObjectField(name, unityObject, type, false, emptyOptions);
            return;
        }

        // Enum
        if (type.IsEnum)
        {
            EditorGUILayout.EnumPopup(name, (Enum) _object, emptyOptions);
            return;
        }

        // null check
        if (_object == null)
        {
            EditorGUILayout.TextField(name, null, emptyOptions);
            return;
        }

        // object has Inspectable attribute
        if (_object.GetType().GetCustomAttributes(true).Any(attr => attr.GetType() == InspectableType))
        {
            EditorGUILayout.LabelField(name);
            EditorGUI.indentLevel += 1;
            Inspect(_object);
            EditorGUI.indentLevel -= 1;
            return;
        }

        // Unregistered Types
        EditorGUILayout.TextField(name, _object.ToString(), emptyOptions);
    }

    /// <summary>
    /// Extracts the inspectables.
    /// </summary>
    /// <returns>The inspectables.</returns>
    /// <param name="type">Type.</param>
    static IEnumerable<MemberInfo> ExtractInspectableMembers(IReflect type)
    {
        var members = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var member in members)
        {
            var attrs = member.GetCustomAttributes(true);

            if (attrs.Any(a => a.GetType() == InspectableType))
            {
                yield return member;
            }
        }
    }

    /// <summary>
    /// 指定されたObjectのField, Propertyを取得しInspectableAtrributeがついてたらInspectorへ表示
    /// </summary>
    /// <param name="obj">object.</param>
    public static void Inspect(object obj)
    {
        var type = obj.GetType();

        foreach (var member in ExtractInspectableMembers(type))
        {
            if (member.MemberType == MemberTypes.Field)
            {
                var field = (FieldInfo) member;
                InspectObject(field.Name, field.FieldType, field.GetValue(obj));
                continue;
            }

            if (member.MemberType == MemberTypes.Property)
            {
                var property = (PropertyInfo) member;
                InspectObject(property.Name, property.PropertyType, property.GetValue(obj, null));
                continue;
            }
        }
    }

    /// <summary>
    /// Raises the inspector GU event.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        IsAutoRefresh = EditorGUILayout.Toggle("AutoRefresh", IsAutoRefresh);
        if (GUILayout.Button("Refresh"))
        {
            OnInspectorGUI();
        }
        EditorGUILayout.EndHorizontal();

        // centered label
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("--Debugging Inspector--");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Start inspection
        foreach (var component in GetAllInspectableComponents())
        {
            EditorGUILayout.BeginVertical(emptyOptions);
            EditorGUILayout.BeginHorizontal(emptyOptions);
            EditorGUILayout.ObjectField(component.gameObject.name, component, typeof(MonoScript), false);
            EditorGUILayout.EndHorizontal();
            Inspect(component);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (IsAutoRefresh)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
