## PlayableObject

The `PlayableObject` component converts objects in your starting map into interactable objects once the map has been loaded in for players.

### Fields

| Name | Description                                                                                                                                                                                                         |
| --- |---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Loaded Object` | This is the object that will be loaded in in-app. This must be a prefab                                                                                                                                             |
| `No Physics` | By default, converted objects gain physics effects causing them to fall due to gravity. This allows you to disable that. **Note:** Players can still edit physics properties themselves after loading in the theme. |
| `Weight` | The weight of the object if it has physics                                                                                                                                                                          |
| `Center of Mass Object` | Designate a custom transform to give the obejct a non-standard center of mass. By default the center of mass is derived from the colliders.                                                                         |
| `Drag` | The drag coefficient for physics                                                                                                                                                                                    |

### Notes on `Loaded Object`

**The objects in the scene marked with `PlayableObject` are destroyed**.
The loaded object is instantiated during runtime when the theme is loaded, replacing the destroyed object.
Any changes in the scene relative to the prefab will be lost. 

There is no requirement that the object marked with `PlayableObject` is an instance of the `Loaded Object`,
but the editor does display a warning when you do this as it is usually not the intention.

All interaction in Aryzon.World with objects is done via their collider, if the `Loaded Object` does not have a collider it will be unusable in Aryzon.World.

### Note on `Center of Mass Object`

The center of mass is not stored as a separate transform object but as the location.
You do not need to refer to an object that will also be known to the loaded object.

### Possible object duplication

If you mark a nested part of a prefab with a convert to solution you can get this warning:  
![duplication_warning.png](..%2FImages%2Fduplication_warning.png)

When you load the theme you will see the object load in twice.
This happens because the object is part of the parent object and supposed to be converted separately.
The parent will be loaded in with this object being non-interactable and once where it is interactable.

To solve this issue remove the object that is causing the issue from the prefab that the parent loads in.

**Note:** This problem can only be detected in Unity version 2022.3 and higher due to requiring `PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded` 