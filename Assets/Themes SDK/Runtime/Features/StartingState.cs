using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using OpenUp.Interpreter.Utils;
using OpenUp.Protobuf;
using OpenUp.Protobuf.AppData;
using OpenUp.Protobuf.Updates;
using OpenUp.Updating;
using UnityEngine;
using Behaviour = UnityEngine.Behaviour;

namespace OpenUp.Environment.Features
{
    public class StartingState : ScriptableObject
    {
        public string[] SerializedStartingState;

        public List<UpdateData> GetStartingState() 
            => SerializedStartingState
              .Select(Convert.FromBase64String)
              .Select(UpdateData.Parser.ParseFrom)
              .ToList();

        public void SetStartingState(List<UpdateData> updates) 
            => SerializedStartingState = updates.Select(up => up.ToByteArray())
                                                .Select(Convert.ToBase64String)
                                                .ToArray();

        public void SetStartingState(GameObject root) 
            => SetStartingState(new GameObject[] { root });
        
        public void SetStartingState(GameObject[] roots)
        {
            StartingUpdateExtractor extractor = new StartingUpdateExtractor(roots);
            SetStartingState(extractor.ExtractUpdates());
        }
        
        private class StartingUpdateExtractor
        {
            // Model field Ids
            private const int MODEL = 1;
            private const int USE_PREFAB = 3;
            
            // Physics field ids
            private const int IGNORE_COLLISION = 1;
            private const int WEIGHT = 2;
            private const int CENTER_OF_MASS = 4;
            private const int DRAG = 5;
            
            // Default value for the lockstate flags
            private const uint DEFAULT_LOCK_STATE = 67110913U;
            
            private int newObjectId = 1;
            private int startingIdKids = 1;
            private readonly GameObject[] sceneRoots;
            private readonly Dictionary<PlayableObject, int> objectIds = new Dictionary<PlayableObject, int>();
            private readonly List<PlayableObject> rootObjects = new List<PlayableObject>();
            
            public StartingUpdateExtractor(GameObject[] sceneRoots)
            {
                this.sceneRoots = sceneRoots;
            }

            public List<UpdateData> ExtractUpdates()
            {
                foreach (GameObject sceneRoot in sceneRoots)
                {
                    GetChildConvertables(sceneRoot.transform, rootObjects);
                }
                
                return sceneRoots.SelectMany(root => root.GetComponentsInChildren<PlayableObject>())
                                 .Select(CreateUpdate)
                                 .ToList();
            }

            // Adds all root objects in parent to output 
            private void GetChildConvertables(Transform parent, List<PlayableObject> output)
            {
                foreach (Transform child in parent)
                {
                    PlayableObject convert = child.GetComponent<PlayableObject>();

                    if (convert)
                        output.Add(convert);
                    else
                        GetChildConvertables(child, output);
                } 
            }

            private int GetId(PlayableObject convertable)
            {
                if (objectIds.TryGetValue(convertable, out int id)) return id;

                return objectIds[convertable] = newObjectId++;
            }
            
            /// <summary>
            /// Creates an update that adds this object to the solution. When an environment is used for
            /// a new solution, this function is called for all instances of ConvertSolution in the environment
            /// and the resulting updates are added to the starting data of the solution.
            /// </summary>
            /// <returns></returns>
            private UpdateData CreateUpdate(PlayableObject convertable)
            {
                ObjectStructure self = GetSelfData(convertable);
                AddBehavioursTo(self, convertable);
                AddChildrenTo(self, convertable);

                UpdateData addSelf = new UpdateData
                                {
                                    Type        = UpdateType.Set,
                                    Path        = new UpdateIntPath(AppRuntimeData.ObjectsFieldNumber, self.Id),
                                    NewValueBytes = self.ToByteString()
                                };

                List<UpdateData> updates = new List<UpdateData> {addSelf};
                
                if (rootObjects.Contains(convertable)) 
                    updates.Add(AddSelfAsRootObject(self, convertable));
                
                return new UpdateData
                       {
                           Type = UpdateType.Composite,
                           Updates = { updates }
                       };
            }

            private ObjectStructure GetSelfData(PlayableObject convertable)
            {
                Visual visual;
                
                if (String.IsNullOrEmpty(convertable.source))
                {
                    visual = new Visual { Type = (int)VisualizationType.Group };
                }
                else
                {
                    visual = new Visual
                             {
                                 Type          = (int)VisualizationType.UnityObject,
                                 [USE_PREFAB] = new ValueData { Bool  = true },
                                 [MODEL]     = new ValueData { Model = new ModelData { Url = convertable.source } },
                             };
                }
                
                return new ObjectStructure
                       {
                           Id        = GetId(convertable),
                           Name      = convertable.name,
                           Transform = new TransformStructure(convertable.transform),
                           LockState = DEFAULT_LOCK_STATE,
                           Visual = visual,
                           Behaviours = new BehaviourCollection(),
                           Children = new Hierarchy()
                       };
            }

            private void AddBehavioursTo(ObjectStructure target, PlayableObject convertable)
            {
                if (!convertable.noPhysics)
                {
                    Vector3 centerOfMass = Vector3.zero;

                    if (convertable.centerOfMassObject) 
                        centerOfMass = convertable.transform.InverseTransformPoint(convertable.centerOfMassObject.position);
                    
                    target.Behaviours[(int)BehaviourType.PHYSICS] =
                        new Protobuf.AppData.Behaviour
                        {
                            Type = (int)BehaviourType.PHYSICS,
                            [IGNORE_COLLISION] = new ValueData { Bool = false },
                            [WEIGHT] = convertable.weight,
                            [CENTER_OF_MASS] = centerOfMass,
                            [DRAG] = convertable.drag
                        };
                }
            }

            private void AddChildrenTo(ObjectStructure target, PlayableObject convertable)
            {
                List<PlayableObject> children = new List<PlayableObject>();
                GetChildConvertables(convertable.transform, children);
                
                foreach (PlayableObject childConvertable in children)
                {
                    Child child = new Child
                    {
                        ChildID = startingIdKids++,
                        ObjectID = GetId(childConvertable),
                        Overrides = new ObjectStructure
                        {
                            Transform = new TransformStructure(childConvertable.transform, convertable.transform)
                        }
                    };

                    target.Children.ChildData[child.ChildID] = child;
                    target.Children.ChildOrder.Add(child.ChildID);
                }
            } 

            private UpdateData AddSelfAsRootObject(ObjectStructure self, PlayableObject convertable)
            {
                Child kid = new Child
                            {
                                ChildID  = startingIdKids++,
                                ObjectID = self.Id,
                                Overrides = new ObjectStructure
                                            {
                                                ExpectedInstanceId = GetId(convertable)
                                            }
                            };

                return new UpdateData
                       {
                           Type = UpdateType.Set,
                           Path = new UpdateIntPath(
                               AppRuntimeData.RootHierarchyFieldNumber,
                               Hierarchy.ChildDataFieldNumber,
                               kid.ChildID
                           ),
                           NewValueBytes = kid.ToByteString()
                       };
            }
        }
    }
}