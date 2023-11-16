## ConvertToSolution

The `ConvertToSolution` component converts objects in your starting map into interactable objects once the map has been loaded in for players.

### Fields

| Name | Description                                                                                                                                                                                                         |
| --- |---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Loaded Object` | This is the object that will be loaded in in-app. This must be a prefab                                                                                                                                             |
| `No Physics` | By default, converted objects gain physics effects causing them to fall due to gravity. This allows you to disable that. **Note:** Players can still edit physics properties themselves after loading in the theme. |
| `Weight` | The weight of the object if it has physics                                                                                                                                                                          |
| `Center of Mass Object` | Designate a custom transform to give the obejct a non-standard center of mass. By default the center of mass is derived from the colliders.                                                                         |
| `Drag` | The drag coefficient for physics                                                                                                                                                                                    |
| `Child of Root` | Indicates that this object is added to the root hierarchy of the game scene                                                                                                                                         |
| `Children` | These objects will be added as children of this main object.                                                                                                                                                        |

### Notes on `Loaded Object`

**The objects in the scene marked with `ConvertToSolution` is destroyed**.
The loaded object is instantiated during runtime when the theme is loaded, replacing the destroyed object.
Any changes in the scene relative to the prefab will be lost. 

There is no requirement that the object marked with `ConvertToSolution` is an instance of the `Loaded Object`,
but the editor does display a warning when you do this as it is usually not the intention.

All interaction in Aryzon.World with objects is done via their collider, if the `Loaded Object` does not have a collider it will be unusable in Aryzon.World.

### Note on `Center of Mass Object`

The center of mass is not stored as a separate transform object but as the location.
You do not need to refer to an object that will also be known to the loaded object.

### Notes on `Children` and `Child of Root`

If an object is not a child of another and not a `Child of Root`, then it will not be loaded in.

In newer versions these fields should become unneeded as they are confusing and should be derivable without custom input.

These fields completely ignore the hierarchy in your map prefab.

Leaving the `Child of Root` enabled and referencing the object in another object's `Chldren` array will cause it to be loaded twice,
both times at the same location.