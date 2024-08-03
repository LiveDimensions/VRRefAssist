# VR RefAssist
A set of custom attributes for Unity to automate usually time consuming references and repetitive tasks.

<img src="https://user-images.githubusercontent.com/26588846/229404067-6b437274-e2f3-48fb-9959-5d53c817715f.png" width="600">

## Features
- Auto-set Singleton references
- Auto-set usually tedious references on MonoBehaviours
- Run code any time a build is requested
- All from within editor mode, no runtime code required or executed!

## How to install
### Unity Package Manager
In your Unity project, go to `Window > Package Manager` then click the top left `+`, click on `Add package from git URL` and paste this link: 

<https://github.com/LiveDimensions/VRRefAssist.git?path=/Packages/com.livedimensions.vrrefassist>

### Unity Package
Download the latest package from the [latest release](https://github.com/LiveDimensions/VRRefAssist/releases/latest)

### VRChat Package Manager
<https://livedimensions.github.io/VRRefAssist/>

## Requirements:
- Git (to install through UnityPackage Manager)

## Notes
By default all field and method automations (RunOnBuild and FieldAutomation) will execute when building **but also when entering play mode**, if you wish to change this go to `VR RefAssist > Settings`.

At this moment there is no 'customization' options for which automations are run **per scene** this means that your console might get spammed a little bit if you change scenes and don't have the same RunOnBuild scene setup, you can of course manually detect which scene is active and go from there. 

VR RefAssist highly leans towards "baking" references within a scene before building/entering play mode, this means there's no need to use `Find` methods on `Awake()` for whatever scripts you have in your scene. This of course does not work with cross-scene references. If you are instantiating prefabs however, you can use an in-scene (inactive in hierarchy) instance of that prefab so that references are populated and instantiate that instead. Here's an example of how that looks in the hierarchy (Pre-warmed Prefabs is disabled, Enemy is enabled).

![image](https://github.com/user-attachments/assets/9a598539-9d63-47a8-b5dd-20f46bc876df)


## Singleton References
`[Singleton]`

This class attribute marks a MonoBehaviour as a singleton and means that any other classes with serialized references to the singleton class will be automatically set (if it is present in the scene).

#### Example
```cs
[Singleton]
public class MySingleton : MonoBehaviour
{

}

public class TestClass : MonoBehaviour
{
    [SerializeField] private MySingleton mySingleton; //This reference will be automatically set by VR RefAssist
}
```

## On Build Requested
### RunOnBuild
`[RunOnBuild]`

This attribute is supported on both static and instance methods! These methods will be run whenever a VRChat build is requested.

There is also an optional `executionOrder` parameter, values higher than `1000` will execute **after** field automation. Default value is `0` (Runs before any field automation).

#### Example
```cs
public class BuildDate : MonoBehaviour
{
    //VR RefAssist field attributes are set before any RunOnBuild methods are executed.
    [SerializeField, GetComponent] public TextMeshPro buildDateText;


    //UpdateBuildText will be run when a build is requested and will set the TextMeshPro text to the build date. 
    #if UNITY_EDITOR && !COMPILER_UDONSHARP //Optional
    [RunOnBuild]
    private void UpdateBuildText()
    {
        buildDateText.text = "This world was built "  + DateTime.Now;
    }
    #endif
}
```

## Field Automation
The following **field** attributes implement different functionality to automatically set any references on **serialized** fields, this means that public or private fields with `[SerializeField]` will work.
All fields have an optional bool parameter `dontOverride` which means that if a field already has a value, it will not attempt to set it again, useful if you want to override a specific field.

**NOTE:** Even though the names of the methods are not plural, they all support array references and will populate accordingly.

### GetComponent
`[GetComponent]`

Will run `GetComponent(<Field Type>)` on it's MonoBehaviour to set that reference.
#### Example
```cs
[SerializeField, GetComponent] private Renderer myRenderer;
```

### GetComponentInChildren
`[GetComponentInChildren]`

Will run `GetComponentInChildren(<Field Type>)` on it's MonoBehaviour to set that reference.
#### Example
```cs
[SerializeField, GetComponentInChildren] private Renderer myRenderer;
```

### GetComponentInParent
`[GetComponentInParent]`

Will run `GetComponentInParent(<Field Type>)` on it's MonoBehaviour to set that reference.
#### Example
```cs
[SerializeField, GetComponentInParent] private Renderer myRenderer;
```

### GetComponentInDirectParent
`[GetComponentInDirectParent]`

Will run `transform.parent.GetComponent(<Field Type>)` on it's MonoBehaviour to set that reference. This is one of the few attributes that does not directly translate into a Unity method, but it is still useful in some cases.
#### Example
```cs
[SerializeField, GetComponentInDirectParent] private Renderer myRenderer;
```

### FindObjectOfType
`[FindObjectOfType]`

Will run `FindObjectOfType(<Field Type>)` on it's MonoBehaviour to set that reference. Optionally, you can specify if you want to include disabled GameObjects when running the method. **The default value is true for includeDisabled.**
#### Example
```cs
[SerializeField, FindObjectOfType] private Renderer myRenderer;
or
[SerializeField, FindObjectOfType(false)] private Renderer myRenderer; //Will not Find disabled Renderers
```

### FindObjectWithTag
`[FindObjectWithTag("Tag")]`

Will run `GameObject.FindGameObjectWithTag(<Tag>).GetComponent(<Type>)` on it's MonoBehaviour to set that reference. If the field is an array, GetComponents is used to get all valid components on a GameObject. Optionally, you can specify if you want to include disabled GameObjects when running the method. **By default, it will include disabled GameObjects.** This will always include disabled *components.*

#### Example
```cs
//Will grab all Transforms on each GameObject with the tag "PossibleItemSpawnPoint".
[SerializeField, FindObjectsWithTag("PossibleItemSpawnPoint")] private Transform[] allPossibleItemSpawnPoints;
or
//Will grab all UdonBehaviours on each GameObject with the tag "PuzzleUdons", even if multiple UdonBehaviours are on one object or if the GameObject or UdonBehaviour is disabled.
[SerializeField, FindObjectsWithTag("PuzzleUdons")] private UdonBehaviour[] allPuzzleUdons;
or
//The same as above, but will exclude disabled GameObjects.
[SerializeField, FindObjectsWithTag("PuzzleUdons", false)] private UdonBehaviour[] allPuzzleUdons;
```

### Find
`[Find("Search")]`

Will run `Find("Search").GetComponent(<Field Type>)` on it's MonoBehaviour to set that reference. This is one of the few attributes that does not directly translates into a Unity method as it runs `GetComponent` after using `Find`.

**NOTE:** `Find` does not currently support arrays.

#### Example
```cs
[SerializeField, Find("My Renderer")] private Renderer myRenderer;
```


### FindInChildren
`[FindInChildren("Search")]`

Will run `transform.Find("Search").GetComponent(<Field Type>)` on it's MonoBehaviour to set that reference. This is one of the few attributes that does not directly translates into a Unity method as it runs `GetComponent` after using `transform.Find`.

**NOTE:** `FindInChildren` does not currently support arrays.

#### Example
```cs
[SerializeField, Find("My Renderer")] private Renderer myRenderer;
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
