using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


namespace DialogueSystem.Utilities
{
    using Data;
    using Data.Save;
    using Elements;
    using ScriptableObjects;
    using Windows;

    public static class DialogueSystemIOUtility
    {
        private static DialogueSystemGraphView graphView;

        private static string graphFileName;
        private static string containerFolderPath;

        private static List<DialogueSystemGroup> groups;
        private static List<DialogueSystemNode> nodes;

        private static Dictionary<string, DialogueSystemDialogueGroupSO> createdDialogueGroups;
        private static Dictionary<string, DialogueSystemDialogueSO> createdDialogues;

        private static Dictionary<string, DialogueSystemGroup> loadedGroups;
        private static Dictionary<string, DialogueSystemNode> loadedNodes;

        public static void Initialise(DialogueSystemGraphView dialogueSystemGraphView, string graphName)
        {
            graphView = dialogueSystemGraphView;
            graphFileName = graphName;
            containerFolderPath = $"Assets/Dialogue System/Dialogues/{graphFileName}";

            groups = new List<DialogueSystemGroup>();
            nodes = new List<DialogueSystemNode>();

            createdDialogueGroups = new Dictionary<string, DialogueSystemDialogueGroupSO>();
            createdDialogues = new Dictionary<string, DialogueSystemDialogueSO>();

            loadedGroups = new Dictionary<string, DialogueSystemGroup>();
            loadedNodes = new Dictionary<string, DialogueSystemNode>();
        }

        #region Save Methods
        public static void Save()
        {
            CreateStaticFolders();
            GetElementsFromGraphView();

            DialogueSystemGraphSaveDataSO graphData = CreateAsset<DialogueSystemGraphSaveDataSO>("Assets/Editor/Dialogue System/Graphs", $"{graphFileName}");
            graphData.Initialise(graphFileName);

            DialogueSystemDialogueContainerSO dialogueContainer = CreateAsset<DialogueSystemDialogueContainerSO>(containerFolderPath, graphFileName);
            dialogueContainer.Initialize(graphFileName);

            SaveGroups(graphData, dialogueContainer);
            SaveNodes(graphData, dialogueContainer);

            SaveAsset(graphData);
            SaveAsset(dialogueContainer);
        }

        #region Groups
        private static void SaveGroups(DialogueSystemGraphSaveDataSO graphData, DialogueSystemDialogueContainerSO dialogueContainer)
        {
            List<string> groupNames = new List<string>();

            foreach (DialogueSystemGroup group in groups)
            {
                SaveGroupToGraph(group, graphData);
                SaveGroupToScitpableObject(group, dialogueContainer);

                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }

        private static void SaveGroupToGraph(DialogueSystemGroup group, DialogueSystemGraphSaveDataSO graphData)
        {
            DialogueSystemGroupSaveData groupData = new DialogueSystemGroupSaveData()
            {
                ID = group.ID,
                Name = group.title,
                Position = group.GetPosition().position
            };


            graphData.Groups.Add(groupData);
        }

        private static void SaveGroupToScitpableObject(DialogueSystemGroup group, DialogueSystemDialogueContainerSO dialogueContainer)
        {
            string groupName = group.title;
            CreateFolder($"{containerFolderPath}/Groups", groupName);
            CreateFolder($"{containerFolderPath}/Groups/{groupName}", "Dialogues");

            DialogueSystemDialogueGroupSO dialogueGroup = CreateAsset<DialogueSystemDialogueGroupSO>($"{containerFolderPath}/Groups/{groupName}", groupName);
            dialogueGroup.Initialise(groupName);

            createdDialogueGroups.Add(group.ID, dialogueGroup);
            dialogueContainer.DialogueGroups.Add(dialogueGroup, new List<DialogueSystemDialogueSO>());

            SaveAsset(dialogueGroup);
        }

        private static void UpdateOldGroups(List<string> currentGroupNames, DialogueSystemGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count != 0)
            {
                List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupNames).ToList();

                foreach (string groupToRemove in groupsToRemove)
                {
                    RemoveFolder($"{containerFolderPath}/Groups/{groupToRemove}");
                }
            }

            graphData.OldGroupNames = new List<string>(currentGroupNames);
        }
        #endregion

        #region Nodes
        private static void SaveNodes(DialogueSystemGraphSaveDataSO graphData, DialogueSystemDialogueContainerSO dialogueContainer)
        {
            SerializableDictionary<string, List<string>> groupedNodeName = new SerializableDictionary<string, List<string>>();
            List<string> ungroupedNodeNames = new List<string>();

            foreach (DialogueSystemNode node in nodes)
            {
                SaveNodeToGraph(node, graphData);
                SaveNodeToScriptableObject(node, dialogueContainer);

                if (node.Group != null) groupedNodeName.AddItem(node.Group.title, node.NodeName);
                else ungroupedNodeNames.Add(node.NodeName);
            }

            UpdateDialogueChoicesConnections();
            UpdateOldGroupedNodes(groupedNodeName, graphData);
            UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
        }

        private static void SaveNodeToGraph(DialogueSystemNode node, DialogueSystemGraphSaveDataSO graphData)
        {
            DialogueSystemNodeSaveData nodeData = new DialogueSystemNodeSaveData(node);

            graphData.Nodes.Add(nodeData);
        }

        private static void SaveNodeToScriptableObject(DialogueSystemNode node, DialogueSystemDialogueContainerSO dialogueContainer)
        {
            DialogueSystemDialogueSO dialogue;

            if (node.Group != null)
            {
                dialogue = CreateAsset<DialogueSystemDialogueSO>($"{containerFolderPath}/Groups/{node.Group.title}/Dialogues", node.NodeName);
                dialogueContainer.DialogueGroups.AddItem(createdDialogueGroups[node.Group.ID], dialogue);
            }
            else
            {
                dialogue = CreateAsset<DialogueSystemDialogueSO>($"{containerFolderPath}/Global/Dialogues", node.NodeName);
                dialogueContainer.UngroupedDialogues.Add(dialogue);
            }

            dialogue.Initialise(node.NodeName, node.NodeType, ConvertNodeChoicesToDialogueChoices(node.Choices), node.Character, node.DialogueType, node.DialogueText, node.IsStartingNode(), node.SoundEffect, node.Delay, node.Graph, node.Quest, node.QuestState);

            createdDialogues.Add(node.ID, dialogue);
            SaveAsset(dialogue);
        }

        private static List<DialogueSystemDialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DialogueSystemChoiceSaveData> nodeChoices)
        {
            List<DialogueSystemDialogueChoiceData> dialogueChoices = new List<DialogueSystemDialogueChoiceData>();

            foreach (DialogueSystemChoiceSaveData nodeChoice in nodeChoices)
            {
                DialogueSystemDialogueChoiceData choiceData = new DialogueSystemDialogueChoiceData() { Text = nodeChoice.Text };
                dialogueChoices.Add(choiceData);
            }

            return dialogueChoices;
        }

        private static void UpdateDialogueChoicesConnections()
        {
            foreach (DialogueSystemNode node in nodes)
            {
                DialogueSystemDialogueSO dialogue = createdDialogues[node.ID];

                for (int choiceIndex = 0; choiceIndex < node.Choices.Count; choiceIndex++)
                {
                    DialogueSystemChoiceSaveData nodeChoice = node.Choices[choiceIndex];

                    if (string.IsNullOrEmpty(nodeChoice.NodeID)) { continue; }

                    dialogue.Choices[choiceIndex].NextDialogue = createdDialogues[nodeChoice.NodeID];

                    SaveAsset(dialogue);
                }
            }
        }

        private static void UpdateOldGroupedNodes(SerializableDictionary<string, List<string>> currentGroupedNodeNames, DialogueSystemGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupedNodeNames != null && graphData.OldGroupedNodeNames.Count != 0)
            {
                foreach (KeyValuePair<string, List<string>> oldGroupedNode in graphData.OldGroupedNodeNames)
                {
                    List<string> nodesToRemove = new List<string>();

                    if (currentGroupedNodeNames.ContainsKey(oldGroupedNode.Key))
                    {
                        nodesToRemove = oldGroupedNode.Value.Except(currentGroupedNodeNames[oldGroupedNode.Key]).ToList();
                    }

                    foreach (string nodeToRemove in nodesToRemove)
                    {
                        RemoveAsset($"{containerFolderPath}/Groups/{oldGroupedNode.Key}/Dialogues", nodeToRemove);
                    }
                }
            }

            graphData.OldGroupedNodeNames = new SerializableDictionary<string, List<string>>(currentGroupedNodeNames);
        }

        private static void UpdateOldUngroupedNodes(List<string> currentUngroupedNodeNames, DialogueSystemGraphSaveDataSO graphData)
        {
            if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count != 0)
            {
                List<string> nodesToRemove = graphData.OldUngroupedNodeNames.Except(currentUngroupedNodeNames).ToList();

                foreach (string nodeToRemove in nodesToRemove)
                {
                    RemoveAsset($"{containerFolderPath}/Global/Dialogues", nodeToRemove);
                }
            }

            graphData.OldUngroupedNodeNames = new List<string>(currentUngroupedNodeNames);
        }
        #endregion
        #endregion

        #region Load Methods
        public static void Load()
        {
            DialogueSystemGraphSaveDataSO graphData = LoadAsset<DialogueSystemGraphSaveDataSO>("Assets/Editor/Dialogue System/Graphs", graphFileName);

            if (graphData == null)
            {
                EditorUtility.DisplayDialog(
                    "Could not find the file!",
                    "The file at the following path could not be found:\n\n" +
                    $"\"Assets/Editor/Dialogue System/Graphs/{graphFileName}\".\n\n" +
                    "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                    "Thanks!"
                );

                return;
            }

            DialogueSystemEditorWindow.UpdateFileName(graphFileName);

            LoadGroups(graphData.Groups);
            LoadNodes(graphData.Nodes);
            LoadNodesConnections();
        }

        private static void LoadGroups(List<DialogueSystemGroupSaveData> groups)
        {
            foreach (DialogueSystemGroupSaveData groupData in groups)
            {
                DialogueSystemGroup group = graphView.CreateGroup(groupData.Name, groupData.Position);

                group.ID = groupData.ID;

                loadedGroups.Add(group.ID, group);
            }
        }

        private static void LoadNodes(List<DialogueSystemNodeSaveData> nodes)
        {
            foreach (DialogueSystemNodeSaveData nodeData in nodes)
            {
                List<DialogueSystemChoiceSaveData> choices = CloneNodeChoices(nodeData.Choices);

                DialogueSystemNode node = graphView.CreateNode(nodeData);

                node.ID = nodeData.ID;

                node.Choices = choices;
                node.DialogueText = nodeData.Text;
                node.Character = nodeData.Character;

                node.SoundEffect = nodeData.SoundEffect;
                node.Delay = nodeData.Delay;
                node.Graph = nodeData.Graph;
                node.Quest = nodeData.Quest;
                node.QuestState = nodeData.QuestState;

                node.Draw();

                graphView.AddElement(node);

                loadedNodes.Add(node.ID, node);

                if (string.IsNullOrEmpty(nodeData.GroupID)) { continue; }

                DialogueSystemGroup group = loadedGroups[nodeData.GroupID];

                node.Group = group;

                group.AddElement(node);
            }
        }

        private static void LoadNodesConnections()
        {
            foreach (KeyValuePair<string, DialogueSystemNode> loadedNode in loadedNodes)
            {
                foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
                {
                    DialogueSystemChoiceSaveData choiceData = (DialogueSystemChoiceSaveData)choicePort.userData;

                    if (string.IsNullOrEmpty(choiceData.NodeID)) { continue; }

                    DialogueSystemNode nextNode = loadedNodes[choiceData.NodeID];

                    Port nextNodeInputPort = (Port)nextNode.inputContainer.Children().First();

                    Edge edge = choicePort.ConnectTo(nextNodeInputPort);

                    graphView.AddElement(edge);

                    loadedNode.Value.RefreshPorts();
                }
            }
        }
        #endregion

        #region Creation Methods
        private static void CreateStaticFolders()
        {
            CreateFolder("Assets/Editor/Dialogue System", "Graphs");
            CreateFolder("Assets", "Dialogue System");
            CreateFolder("Assets/Dialogue System", "Dialogues");
            CreateFolder("Assets/Dialogue System/Dialogues", graphFileName);

            CreateFolder(containerFolderPath, "Global");
            CreateFolder(containerFolderPath, "Groups");
            CreateFolder($"{containerFolderPath}/Global", "Dialogues");
        }
        #endregion

        #region Fetch Methods
        private static void GetElementsFromGraphView()
        {
            graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is DialogueSystemNode node)
                {
                    nodes.Add(node);
                    return;
                }

                if (graphElement is DialogueSystemGroup group)
                {
                    groups.Add(group);
                    return;
                }
            });
        }
        #endregion

        #region Utility Methods
        private static void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}")) { return; }
            AssetDatabase.CreateFolder(path, folderName);
        }

        public static void RemoveFolder(string path)
        {
            FileUtil.DeleteFileOrDirectory($"{path}.meta");
            FileUtil.DeleteFileOrDirectory($"{path}/");
        }

        public static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }

            return asset;
        }

        public static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }

        private static void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RemoveAsset(string path, string assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }

        public static List<DialogueSystemChoiceSaveData> CloneNodeChoices(List<DialogueSystemChoiceSaveData> nodeChoices)
        {
            List<DialogueSystemChoiceSaveData> choices = new List<DialogueSystemChoiceSaveData>();

            foreach (DialogueSystemChoiceSaveData choice in nodeChoices)
            {
                DialogueSystemChoiceSaveData choiceData = new DialogueSystemChoiceSaveData()
                {
                    Text = choice.Text,
                    NodeID = choice.NodeID
                };

                choices.Add(choiceData);
            }

            return choices;
        }
        #endregion
    }
}