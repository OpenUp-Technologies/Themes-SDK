## Performance Tools

The Aryzon.World themes SDK contains a few tools to help keep track of aspects of created themes that can make a map or theme slow and choppy when loaded in.
This mainly focusses on vertex count and physics logic but is being worked on to account for more and more.

### Hierarchy Annotation

One of the key ways the performance tools can help is that they can create an overview in your Unity hierarchy telling you what the heavy objects are.

![hierarchy_percents.png](..%2FImages%2Fhierarchy_percents.png)

To do this you can right click on the object you want to analyse and choose `Analysis > mode`.
You can also access these options at the top menu bar under `OpenUp > Analysis > mode`.
This top bar will always analyse all objects in the open scene.
This is also where you can hide the hierarchy annotation once you're done with it.

![hierarchy_view_options.png](..%2FImages%2Fhierarchy_view_options.png)

There are five modes you can use to analyse your objects and scene.

#### Percentages

This gives you the percentage of vertices a single object has a part of the whole.
For example, if the analysed object has 20'000 vertices, then a sub object with 1'000 verts would have percentage value 5%.

The options `Full` and `Individual` determine if the counts include child objects.

When `Full` is chosen, then the vertex count of any object includes the vertices of all child objects.
If `Individual` is chosen, only the vertices of meshes on the actual object are taken into account.

#### Vertices

This view gives an exact vertex count for each object.
It also has a background colour to indicate how heavy the given vertex count is for the Aryzon.World platform.

Like percentages, this mode as a `Full` and an `Individual` variant.
The `Full` variant also count all vertices in child objects, the `Individual` variant does not.

#### Density Level

This view gives a value for how 'dense' its vertices are, measured as vertices per cubic meter.
This is effectively a value for how highly detailed the objects are.
This value scales logarithmically, an object with a density level of `4` has ten times as many vertices per cubic meter as an object with density level `3`.

This mode is designed to allow you to pick out the objects that have more detail than needed.
Bear in mind that high-detail objects are desired when they are being looked at closely by players.
High density is fine when the object is being held in a player's hand, but not desirable for some screw hidden in a wall that a player will never look at.

### Theme Stats View

![theme_stats.png](..%2FImages%2Ftheme_stats.png)

The theme stats view gives you an overview of all performance affecting data of your theme.

| Field | Meaning |
| --- | --- |
| Vertices | The total ammount of vertices in the root map |
| Total Objects | The total amount of game objects |
| Interactable Objects | The number of in-game objects that will be created when the map is loaded in as starting map. |
| Colliders | The amount of coliders in the root map. More colliders makes the physics harder to run. |
| Mesh Collision Faces | The total number of faces of mesh colliders. Mesh colliders should be used sparingly as they put a heavy strain on the physics engine. |

#### Warnings

![theme_stats_warning.png](..%2FImages%2Ftheme_stats_warning.png)

The stats window will also display warnings if it detects aspects of a theme that exceed recommended values.

an overview of the warnings and what to do about them can be found [here](PerformanceWarnings.md) 