using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ggg_dialogue_viewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            loadFileNames();
        }

        uint ptr = 0; // counts in bytes. assume byte 0 = the start of the length of Nodes for now
        byte[] data;
        DialogueGraph dg;
        List<int> baseNodes;
        string[] files;

        int GetInt()
        {
            // [ aa, bb, cc, dd ] -> 0xddccbbaa
            int val = data[ptr] | data[ptr + 1] << 8 | data[ptr + 2] << 16 | data[ptr + 3] << 24;
            ptr += 4;
            return val;
        }

        float GetFloat()
        {
            // [ aa, bb, cc, dd ] -> 0xddccbbaa
            float val = data[ptr] | data[ptr + 1] << 8 | data[ptr + 2] << 16 | data[ptr + 3] << 24;
            ptr += 4;
            return val;
        }

        string GetStr(uint strLen)
        {
            // [ l4, l4, l4, l4, c1, c1, c1 ... c1 00 00 ]
            // l4 = 4-byte len
            // c1 = 1-byte char
            // null bytes filled in for 4-byte alignment
            // assumes ptr is at c1
            if (strLen == 0)
                return "";

            char[] val = new char[strLen];
            for (uint i = 0; i < strLen; i++)
            {
                val[i] = (char)data[ptr + i];
            }

            if (strLen % 4 == 0)
                ptr += strLen;
            else
                ptr += strLen + (4 - (strLen % 4));

            string returnVal = new string(val);

            return returnVal;
        }

        bool GetBool()
        {
            int val = GetInt();
            return val == 1;
        }

        uint GetUint()
        {
            return (uint)GetInt();
        }

        void GetComparator<V>(Edge.Condition<V> cond) where V : IComparable
        {
            // gets the comparator and places it into the passed condition
            uint enumVal = GetUint();
            cond.Comparator = (Edge.Condition<V>.comparisonType)enumVal;
        }

        void fillAttributes(HasAttributes obj)
        {
            uint attrIntLen = GetUint();
            obj.AttributesInt = new HasAttributes.Attribute<int>[attrIntLen];
            for (uint i = 0; i < attrIntLen; i++)
            {
                obj.AttributesInt[i].Key = GetStr(GetUint());
                obj.AttributesInt[i].Value = GetInt();
            }

            uint attrFloatLen = GetUint();
            obj.AttributesFloat = new HasAttributes.Attribute<float>[attrFloatLen];
            for (uint i = 0; i < attrFloatLen; i++)
            {
                obj.AttributesFloat[i].Key = GetStr(GetUint());
                obj.AttributesFloat[i].Value = GetFloat();
            }

            uint attrStringLen = GetUint();
            obj.AttributesString = new HasAttributes.Attribute<string>[attrStringLen];
            for (uint i = 0; i < attrStringLen; i++)
            {
                obj.AttributesString[i].Key = GetStr(GetUint());
                obj.AttributesString[i].Value = GetStr(GetUint());
            }

            uint attrBoolLen = GetUint();
            obj.AttributesBool = new HasAttributes.Attribute<bool>[attrBoolLen];
            for (uint i = 0; i < attrBoolLen; i++)
            {
                obj.AttributesBool[i].Key = GetStr(GetUint());
                obj.AttributesBool[i].Value = GetBool();
            }

        }

        void fillEdge(Edge edge)
        {
            fillAttributes(edge);

            uint condIntLen = GetUint();
            edge.ConditionsInt = new Edge.Condition<int>[condIntLen];
            for (uint i = 0; i < condIntLen; i++)
            {
                edge.ConditionsInt[i] = new Edge.Condition<int>();
                edge.ConditionsInt[i].Key = GetStr(GetUint());
                GetComparator(edge.ConditionsInt[i]);
                edge.ConditionsInt[i].Value = GetInt();
            }

            uint condFloatLen = GetUint();
            edge.ConditionsFloat = new Edge.Condition<float>[condFloatLen];
            for (uint i = 0; i < condFloatLen; i++)
            {
                edge.ConditionsFloat[i] = new Edge.Condition<float>();
                edge.ConditionsFloat[i].Key = GetStr(GetUint());
                GetComparator(edge.ConditionsFloat[i]);
                edge.ConditionsFloat[i].Value = GetFloat();
            }

            uint condStringLen = GetUint();
            edge.ConditionsString = new Edge.Condition<string>[condStringLen];
            for (uint i = 0; i < condStringLen; i++)
            {
                edge.ConditionsString[i] = new Edge.Condition<string>();
                edge.ConditionsString[i].Key = GetStr(GetUint());
                GetComparator(edge.ConditionsString[i]);
                edge.ConditionsString[i].Value = GetStr(GetUint());
            }

            uint condBoolLen = GetUint();
            edge.ConditionsBool = new Edge.Condition<bool>[condBoolLen];
            for (uint i = 0; i < condBoolLen; i++)
            {
                edge.ConditionsBool[i] = new Edge.Condition<bool>();
                edge.ConditionsBool[i].Key = GetStr(GetUint());
                GetComparator(edge.ConditionsBool[i]);
                edge.ConditionsBool[i].Value = GetBool();
            }

            edge.OutNodeIndex = GetInt();
            edge.Priority = GetInt();
        }

        void fillNode(DialogueNode node)
        {
            fillAttributes(node);

            uint numEdges = GetUint();
            node.OutEdges = new Edge[numEdges];
            if (numEdges != 0)
            {
                for (int i = 0; i < numEdges; i++)
                {
                    node.OutEdges[i] = new Edge();
                    fillEdge(node.OutEdges[i]);
                }
            }

            uint textArrLen = GetUint();
            node.Text = new string[textArrLen];
            for (uint i = 0; i < textArrLen; i++)
                node.Text[i] = GetStr(GetUint());


            uint spriteArrLen = GetUint();
            node._spriteIndexes = new string[spriteArrLen];
            for (uint i = 0; i < spriteArrLen; i++)
                node._spriteIndexes[i] = GetStr(GetUint());

            node.LineCodeName = GetStr(GetUint());
            node.CharacterIndex = GetInt();
            node.VOLATILE_IndexFoundAtInGraph = GetInt();
            node._metaXPos = GetInt();
            node._metaYPos = GetInt();
            node._metaID = GetInt();
        }

        void parse()
        {
            dg = new DialogueGraph();

            // first 12 bytes unknown
            ptr = 12;
            dg.m_Enabled = GetUint();
            dg.m_FileID = GetUint();
            dg.m_PathID = GetUint();
            dg.m_PathID |= (ulong)GetUint() << 32;
            dg.m_Name = GetStr(GetUint());

            uint numOfNodes = GetUint();
            dg.Nodes = new DialogueNode[numOfNodes];

            for (int nodeIndex = 0; nodeIndex < numOfNodes; nodeIndex++)
            {
                dg.Nodes[nodeIndex] = new DialogueNode();
                fillNode(dg.Nodes[nodeIndex]);
            }

            uint numMetaNodes = GetUint();
            dg._metaNotes = new NoteNode[numMetaNodes];
            for (int i = 0; i < numMetaNodes; i++)
            {
                dg._metaNotes[i] = new NoteNode();
                uint numText = GetUint();

                dg._metaNotes[i].Text = new string[numText];

                for (int j = 0; j < numText; j++)
                    dg._metaNotes[i].Text[j] = GetStr(GetUint());

                dg._metaNotes[i]._metaXPos = GetFloat();
                dg._metaNotes[i]._metaYPos = GetFloat();
            }

            dg._metaImgSubPath = GetStr(GetUint());

            uint numSubPaths = GetUint();
            dg._metaImgSubPaths = new string[numSubPaths];
            for (int i = 0; i < numSubPaths; i++)
            {
                dg._metaImgSubPaths[i] = GetStr(GetUint());
            }

        }

        void loadFileNames()
        {
            try
            {
                files = Directory.GetFiles("./dialogues");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not find dialogue files.\nMake sure that you have followed the instructions in readme.txt fully!\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK);
                files = new string[0]; // just get an empty list
            }

            string[] substrings;
            foreach (string s in files)
            {
                substrings = s.Split('\\');
                listBox1.Items.Add(substrings[substrings.Length-1]);
            }
        }

        void findBaseNodes()
        {
            bool[] baseNodeTracker = new bool[dg.Nodes.Length];

            for (int i = 0; i < dg.Nodes.Length; i++)
            {
                for (int j = 0; j < dg.Nodes[i].OutEdges.Length; j++)
                {
                    if (dg.Nodes[i].OutEdges[j].OutNodeIndex < dg.Nodes.Length) // error checking
                        baseNodeTracker[dg.Nodes[i].OutEdges[j].OutNodeIndex] = true; // not base node
                }
            }

            baseNodes = new List<int>();

            for (int i = 0; i < baseNodeTracker.Length; i++)
            {
                if (!baseNodeTracker[i])
                    baseNodes.Add(i);
            }
        }

        void loopNodeRecursion(int nodeIndex, List<int> prevNodes)
        {
            for (int i = 0; i < dg.Nodes[nodeIndex].OutEdges.Length; i++)
            {
                if (dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex >= dg.Nodes.Length)
                    continue; // error catching - some destination nodes are outside of the range of nodes that exist

                if (prevNodes.Contains(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex))
                    dg.infiniteLoops.Add(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex);
                else
                {
                    prevNodes.Add(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex);
                    loopNodeRecursion(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex, prevNodes);
                }
            }

        }

        void findInfiniteLoops()
        {
            // go through each conversation based on previously-found base nodes
            // track which nodes have been visited per convo and include those on a list of nodes to not print twice in one convo
            // this avoids infinitely looping creating nodes

            dg.infiniteLoops = new List<int>();
            List<int> previousNodes = new List<int>();


            for (int i = 0; i < baseNodes.Count; i++)
            {
                previousNodes.Add(baseNodes[i]);
                loopNodeRecursion(baseNodes[i], previousNodes);
                previousNodes.Clear();
            }

            dg.clearedLoops = new bool[dg.Nodes.Length];
        }

        bool emptyConvosRecursion(int nodeIndex)
        {
            for (int i = 0; i < dg.Nodes[nodeIndex].Text.Length; i++)
            {
                if (dg.Nodes[nodeIndex].Text[i] != "")
                    return true;
            }

            bool rtn = false;

            for (int i = 0; i < dg.Nodes[nodeIndex].OutEdges.Length; i++)
            {
                // either the node isn't in the list of possible inf loops, or it's not been written out yet
                if (!dg.infiniteLoops.Contains(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex) || dg.clearedLoops[dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex] == false)
                {
                    if (dg.infiniteLoops.Contains(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex))
                        dg.clearedLoops[dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex] = true; // don't write this index out again
                    rtn = emptyConvosRecursion(dg.Nodes[nodeIndex].OutEdges[i].OutNodeIndex);
                }
            }

            return rtn;
        }

        void findEmptyConvos()
        {
            // get rid of empty conversations from the list of convo starters
            // unused
            for (int i = 0; i < baseNodes.Count; i++)
            {
                if (!emptyConvosRecursion(baseNodes[i]))
                    baseNodes.Remove(baseNodes[i]); i -= 1; // since we're removing a convo
            }

            for (int i = 0; i < dg.clearedLoops.Length; i++)
                dg.clearedLoops[i] = false;
        }

        void setDialogueTree()
        {
            // set UI stuff based on what is in dialoguegraph, basenodes and infiniteloops
            dialogueTree.Nodes.Clear();

            for (int i = 0; i < baseNodes.Count; i++)
            {
                TreeNode baseNode = dialogueTree.Nodes.Add(dg.Nodes[baseNodes[i]].ToString());
                dg.AddChildren(baseNodes[i], baseNode);

                for (int j = 0; j < dg.clearedLoops.Length; j++)
                    dg.clearedLoops[j] = false; // just in case
            }

            for (int i = 0; i < dg._metaNotes.Length; i++)
            {
                dialogueTree.Nodes.Add("DEV NOTE: " + dg._metaNotes[i].ToString());
            }

            label2.Text = "Dialogue: " + dg.m_Name;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox lb = (ListBox)sender;
            try
            {
                data = File.ReadAllBytes(".\\dialogues\\" + lb.Text);

                parse();

                findBaseNodes();
                findInfiniteLoops();
                setDialogueTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading selected dialogue.\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK);
            }
        }
    }

    public class DialogueGraph
    {
        public uint m_Enabled;
        public uint m_FileID;
        public ulong m_PathID;
        public string m_Name;

        public DialogueNode[] Nodes;
        public NoteNode[] _metaNotes;
        public string _metaImgSubPath;
        public string[] _metaImgSubPaths;

        public List<int> infiniteLoops; // nodes that correspond to infinite loops in dialogue
        public bool[] clearedLoops; // loops that have been completed while writing out dialogue

        public void AddChildren(int nodeIndex, TreeNode node)
        {
            for (int i = 0; i < Nodes[nodeIndex].OutEdges.Length; i++)
            {
                if (Nodes[nodeIndex].OutEdges[i].OutNodeIndex >= Nodes.Length)
                    continue; // some destinations are outside of the range of nodes that exist

                if (!infiniteLoops.Contains(Nodes[nodeIndex].OutEdges[i].OutNodeIndex) || clearedLoops[Nodes[nodeIndex].OutEdges[i].OutNodeIndex] == false)
                {
                    StringBuilder nextNode = new StringBuilder(Nodes[nodeIndex].OutEdges[i].ToString() + Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].ToString());
                    if (Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].OutEdges.Length == 0)
                        nextNode.Append("  |  END");

                    TreeNode baseNode = node.Nodes.Add(nextNode.ToString());
                    if (infiniteLoops.Contains(Nodes[nodeIndex].OutEdges[i].OutNodeIndex))
                        clearedLoops[Nodes[nodeIndex].OutEdges[i].OutNodeIndex] = true; // don't write this node out again
                    AddChildren(Nodes[nodeIndex].OutEdges[i].OutNodeIndex, baseNode);
                }
                else
                {
                    // get the next node(s) after the loop node
                    if (Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].OutEdges.Length > 0)
                    {
                        StringBuilder nextNodes = new StringBuilder();
                        for (int j = 0; j < Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].OutEdges.Length; j++)
                        {
                            if (j > 0)
                                nextNodes.Append("; ");

                            nextNodes.Append(Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].OutEdges[j].ToString() + "ID " + Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].OutEdges[j].OutNodeIndex.ToString());
                        }

                        TreeNode baseNode = node.Nodes.Add(Nodes[nodeIndex].OutEdges[i].ToString() + Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].ToString() + "  |  NEXT: " + nextNodes.ToString());
                    }
                    else
                    {
                        // end node
                        TreeNode baseNode = node.Nodes.Add(Nodes[nodeIndex].OutEdges[i].ToString() + Nodes[Nodes[nodeIndex].OutEdges[i].OutNodeIndex].ToString() + "  |  END");
                    }
                }
            }
        }

    }

    public class HasAttributes
    {
        public Attribute<int>[] AttributesInt;
        public Attribute<float>[] AttributesFloat;
        public Attribute<string>[] AttributesString;
        public Attribute<bool>[] AttributesBool;

        [Serializable]
        public struct Attribute<V>
        {
            public string Key;
            public V Value;
        }
    }

    public class DialogueNode : HasAttributes
    {

        public Edge[] OutEdges;
        public string[] Text;
        public string[] _spriteIndexes;
        public string LineCodeName;
        public int CharacterIndex;
        public int VOLATILE_IndexFoundAtInGraph;
        public float _metaXPos;
        public float _metaYPos;
        public int _metaID;

        public override string ToString()
        {
            StringBuilder str = new StringBuilder("ID " + VOLATILE_IndexFoundAtInGraph.ToString());
            if (LineCodeName != "null" && LineCodeName != "")
                str.Append("  |  C: " + LineCodeName);

            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i] != "")
                {
                    if (i == 0)
                        str.Append("  |  ");
                    str.Append(Text[i]);
                }
            }

            str.Replace("\n", " ");

            return str.ToString();
        }

    }

    public class Edge : HasAttributes
    {
        public class Condition<V> where V : IComparable
        {
            public string Key;
            public comparisonType Comparator;
            public V Value;

            public enum comparisonType { gt, lt, gte, lte, eq, neq }
        }

        public Condition<int>[] ConditionsInt;
        public Condition<float>[] ConditionsFloat;
        public Condition<string>[] ConditionsString;
        public Condition<bool>[] ConditionsBool;

        public int OutNodeIndex;
        public int Priority;

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            bool multiple = false;

            for (int i = 0; i < ConditionsInt.Length; i++)
            {
                if (multiple)
                    str.Append("; ");
                str.Append(ConditionsInt[i].Key + " " + ConditionsInt[i].Comparator.ToString() + " " + ConditionsInt[i].Value.ToString()); multiple = true;
            }

            for (int i = 0; i < ConditionsFloat.Length; i++)
            {
                if (multiple)
                    str.Append("; ");
                str.Append(ConditionsFloat[i].Key + " " + ConditionsFloat[i].Comparator.ToString() + " " + ConditionsFloat[i].Value.ToString()); multiple = true;
            }

            for (int i = 0; i < ConditionsString.Length; i++)
            {
                if (multiple)
                    str.Append("; ");
                str.Append(ConditionsString[i].Key + " " + ConditionsString[i].Comparator.ToString() + " " + ConditionsString[i].Value.ToString()); multiple = true;
            }

            for (int i = 0; i < ConditionsBool.Length; i++)
            {
                if (multiple)
                    str.Append("; ");
                str.Append(ConditionsBool[i].Key + " " + ConditionsBool[i].Comparator.ToString() + " " + ConditionsBool[i].Value.ToString()); multiple = true;
            }

            if (str.Length != 0)
                str.Append(": ");

            return str.ToString();
        }
    }

    public class NoteNode
    {
        public string[] Text;
        public float _metaXPos;
        public float _metaYPos;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Text[0]);
            for (int i = 1; i < Text.Length; i++)
            {
                sb.Append("; " + Text[i]);
            }
            return sb.ToString();
        }
    }
}
