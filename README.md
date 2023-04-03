# VR RefAssist
A set of custom attributes to automate usually time consuming references and repetitive tasks.

<img src="https://user-images.githubusercontent.com/26588846/229402515-618ca257-e24e-44f3-a114-973828bca0bc.png" width="600">

## How to install
In your Unity project, go to `Window > Package Manager` then click the top left `+`, click on `Add package from git URL` and paste this link: 
`https://github.com/LiveDimensions/VRRefAssist.git?path=/Packages/com.livedimensions.vrrefassist`

Requirements:
- UdonSharp
- Git (to install through Package Manager)

## Singleton References
`[Singleton]`

This class attribute marks an UdonSharpBehaviour as a singleton and means that any other classes with serialized references to the singleton class will be automatically set (if it is present in the scene).

#### Example
```cs
[Singleton]
public class MySingleton : UdonSharpBehaviour
{

}

public class TestClass : UdonSharpBehaviour
{
    [SerializedField] private MySingleton mySingleton; //This reference will be automatically set by VR RefAssist
}
```

## On Build Requested
### RunOnBuild
`[RunOnBuild]`

This attribute is only supported on **static** methods at the moment, and will run whenever a VRChat build is requested. There is also an optional `executionOrder` parameter

#### Example
```cs
public class BuildDate : UdonSharpBehaviour
{
    //VR RefAssist field attributes are set before any RunOnBuild methods are executed.
    [SerializedField, GetComponent] public TextMeshPro buildDateText;


    //UpdateBuildText will be run when a build is requested and will set the TextMeshPro text to the build date. 
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    [RunOnBuild]
    private static void UpdateBuildText()
    {
        BuildDate buildDate = FindObjectOfType<BuildDate>();

        buildDate.buildDateText.text = "This world was built "  + DateTime.Now;
    }
    #endif
}
```

## Field Automation
The following **field** attributes implement different functionality to automatically set any references on **serialized** fields, this means that public or private fields with `[SerializeField]` will work.

**NOTE:** Even though the names of the methods are not plural, they all support array references and will populate accordingly.

### GetComponent
`[GetComponent]`

Will run `GetComponent(<Field Type>)` on it's UdonSharpBehaviour to set that reference.
#### Example
```cs
[SerializedField, GetComponent] private Renderer myRenderer;
```

### GetComponentInChildren
`[GetComponentInChildren]`

Will run `GetComponentInChildren(<Field Type>)` on it's UdonSharpBehaviour to set that reference.
#### Example
```cs
[SerializedField, GetComponentInChildren] private Renderer myRenderer;
```

### GetComponentInParent
`[GetComponentInParent]`

Will run `GetComponentInParent(<Field Type>)` on it's UdonSharpBehaviour to set that reference.
#### Example
```cs
[SerializedField, GetComponentInParent] private Renderer myRenderer;
```

### GetComponentInDirectParent
`[GetComponentInDirectParent]`

Will run `transform.parent.GetComponent(<Field Type>)` on it's UdonSharpBehaviour to set that reference. This is one of the few attributes that does not directly translate into a Unity method, but it is still useful in some cases.
#### Example
```cs
[SerializedField, GetComponentInDirectParent] private Renderer myRenderer;
```

### FindObjectOfType
`[FindObjectOfType]`

Will run `FindObjectOfType(<Field Type>)` on it's UdonSharpBehaviour to set that reference. Optionally you can specify if you want to include disabled GameObjects when running the method. **The default value is true for includeDisabled.**
#### Example
```cs
[SerializedField, FindObjectOfType] private Renderer myRenderer;
or
[SerializedField, FindObjectOfType(false)] private Renderer myRenderer; //Will not Find disabled Renderers
```

### Find
`[Find("Search")]`

Will run `Find("Search").GetComponent(<Field Type>)` on it's UdonSharpBehaviour to set that reference. This is one of the few attributes that does not directly translates into a Unity method as it runs `GetComponent` after using `Find`.

**NOTE:** `Find` does not currently support arrays.

#### Example
```cs
[SerializedField, Find("My Renderer")] private Renderer myRenderer;
```


### FindInChildren
`[FindInChildren("Search")]`

Will run `transform.Find("Search").GetComponent(<Field Type>)` on it's UdonSharpBehaviour to set that reference. This is one of the few attributes that does not directly translates into a Unity method as it runs `GetComponent` after using `transform.Find`.

**NOTE:** `FindInChildren` does not currently support arrays.

#### Example
```cs
[SerializedField, Find("My Renderer")] private Renderer myRenderer;
```

## Miscellaneous Editor Methods
### FindObjectOfTypeIncludeDisabled
These methods are to be used in editor scripting only. These were implemented because as of Unity 2019.4.31f1 (Current VRChat version) Unitys' FindObjectOfType method does not include disabled GameObjects.

- `UnityEditorExtensions.FindObjectOfTypeIncludeDisabled<T>()`
- `UnityEditorExtensions.FindObjectOfTypeIncludeDisabled(Type type)`
- `UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled<T>()`
- `UnityEditorExtensions.FindObjectsOfTypeIncludeDisabled(Type type)`

### FullSetDirty
`FullSetDirty(Object obj)`
Simply runs
```cs
EditorUtility.SetDirty(obj);

if (PrefabUtility.IsPartOfAnyPrefab(obj))
{
     PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
}
```
