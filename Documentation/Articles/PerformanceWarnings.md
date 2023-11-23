## Performance Warnings

This is a list of warnings that the performance helping tools can generate together with what can be done to minimize the issue.

<details>
<summary>Details</summary>

- [Root map exceeds the recommended maximum vertex count](#root-map-exceeds-the-recommended-maximum-vertex-count)
- [Root map contains highly complex collision meshes](#root-map-contains-highly-complex-collision-meshes)

</details>

### Root map exceeds the recommended maximum vertex count

#### What is it about?

This warning indicates that the objects in the root map have too many vertices.
The objects re too complex and some lower-power devices will have trouble rendering the map smoothly.

#### What to do about it?

Reduce the number of vertices in the meshes of the objects.
To help with this you can use the [hierarchy annotation tool](PerformanceTools.md#hierarchy-annotation) to find the worst offender in terms of vertex count.

### Root map contains highly complex collision meshes

#### What is this about?

The theme root map contains objects with [Mesh Colliders](https://docs.unity3d.com/Manual/class-MeshCollider.html) that are using high vertex count meshes.
Mesh colliders are very heavy on the phsyics engine and should be used only when absolutely needed.

#### What to do about it?

To resolve this, you either need to have very low vertex meshes for mesh collision or switch to using primitive colliders.
Using primitive colliders involves adding multiple box colliders and shaping those to approximate the shape of the mesh.

You can also use sphere- or capsule colliders for this task.
These are not resolved as meshes and don't get calculated as if they are high vertex meshes.  