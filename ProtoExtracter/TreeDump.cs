﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace ProtoExtracter
{
    // These classes are for debug and testing purposes only. It is frequently handy to be 
    // able to create a string representation of a complex tree-based data type. The idea
    // here is to first transform your tree into a standard "tree dumper node" tree, where
    // each node in the tree has a name, some optional data, and a sequence of child nodes.
    // Once in a standard format the tree can then be rendered in a variety of ways 
    // depending on what is most useful to you.
    //
    // I've started with two string formats. First, a "compact" format in which there is
    // exactly one line per node of the tree:
    //
    //   root
    //   ├─a1
    //   │ └─a1b1
    //   ├─a2
    //   │ ├─a2b1
    //   │ │ └─a2b1c1
    //   │ └─a2b2
    //   │   ├─a2b2c1
    //   │   │ └─a2b2c1d1
    //   │   └─a2b2c2
    //   └─a3
    //     └─a3b1
    //
    // And second, an XML format:
    //
    // <root>
    // <children>
    // <child>
    // value1
    // </child>
    // <child>
    // value2
    // </child>
    // </children>
    // </root>
    //
    // The XML format is much more verbose, but handy if you want to then pipe it into an XML tree viewer.

    /// <summary>
    /// This is ONLY used id BoundNode.cs Debug method - Dump()
    /// </summary>
    internal class TreeDumper
    {
        private readonly StringBuilder _sb;

        protected TreeDumper()
        {
            _sb = new StringBuilder();
        }

        public static string DumpCompact(TreeDumperNode root) =>
            new TreeDumper().DoDumpCompact(root);

        protected string DoDumpCompact(TreeDumperNode root)
        {
            DoDumpCompact(root, string.Empty);
            return _sb.ToString();
        }

        private void DoDumpCompact(TreeDumperNode node, string indent)
        {
            // Precondition: indentation and prefix has already been output
            // Precondition: indent is correct for node's *children*
            _sb.Append(node.Text);
            if (node.Value != null)
            {
                _sb.AppendFormat(": {0}", DumperString(node.Value));
            }

            _sb.AppendLine();
            var children = node.Children.ToList();
            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                if (child == null)
                {
                    continue;
                }

                _sb.Append(indent);
                _sb.Append(i == children.Count - 1 ? '\u2514' : '\u251C');
                _sb.Append('\u2500');

                // First precondition met; now work out the string needed to indent 
                // the child node's children:
                DoDumpCompact(child, indent + (i == children.Count - 1 ? "  " : "\u2502 "));
            }
        }

        public static string DumpXML(TreeDumperNode root, string? indent = null)
        {
            var dumper = new TreeDumper();
            dumper.DoDumpXML(root, string.Empty, indent ?? string.Empty);
            return dumper._sb.ToString();
        }

        private void DoDumpXML(TreeDumperNode node, string indent, string relativeIndent)
        {
            if (node.Children.All(child => child == null))
            {
                _sb.Append(indent);
                if (node.Value != null)
                {
                    _sb.AppendFormat("<{0}>{1}</{0}>", node.Text, DumperString(node.Value));
                }
                else
                {
                    _sb.AppendFormat("<{0} />", node.Text);
                }
                _sb.AppendLine();
            }
            else
            {
                _sb.Append(indent);
                _sb.AppendFormat("<{0}>", node.Text);
                _sb.AppendLine();
                if (node.Value != null)
                {
                    _sb.Append(indent);
                    _sb.AppendFormat("{0}", DumperString(node.Value));
                    _sb.AppendLine();
                }

                var childIndent = indent + relativeIndent;
                foreach (var child in node.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    DoDumpXML(child, childIndent, relativeIndent);
                }

                _sb.Append(indent);
                _sb.AppendFormat("</{0}>", node.Text);
                _sb.AppendLine();
            }
        }

        // an (awful) test for a null read-only-array.  Is there no better way to do this?
        private static bool IsDefaultImmutableArray(object o)
        {
            var ti = o.GetType().GetTypeInfo();
            if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                var result = ti?.GetDeclaredMethod("get_IsDefault")?.Invoke(o, Array.Empty<object>());
                return result is bool b && b;
            }

            return false;
        }

        protected virtual string DumperString(object o)
        {
            if (o == null)
            {
                return "(null)";
            }

            if (o is string str)
            {
                return str;
            }

            if (IsDefaultImmutableArray(o))
            {
                return "(null)";
            }

            if (o is IEnumerable seq)
            {
                return string.Format("{{{0}}}", string.Join(", ", seq.Cast<object>().Select(DumperString).ToArray()));
            }

            return o.ToString() ?? "";
        }
    }

    /// <summary>
    /// This is ONLY used for debugging purpose
    /// </summary>
    internal sealed class TreeDumperNode
    {
        public TreeDumperNode(string text, object? value, IEnumerable<TreeDumperNode>? children)
        {
            Text = text;
            Value = value;
            Children = children ?? new List<TreeDumperNode>();
        }

        public TreeDumperNode(string text) : this(text, null, null) { }
        public object? Value { get; }
        public string Text { get; }
        public IEnumerable<TreeDumperNode> Children { get; }
        public TreeDumperNode? this[string child] =>
            Children.FirstOrDefault(c => c.Text == child);

        // enumerates all edges of the tree yielding (parent, node) pairs. The first yielded value is (null, this).
        public IEnumerable<KeyValuePair<TreeDumperNode?, TreeDumperNode>> PreorderTraversal()
        {
            var stack = new Stack<KeyValuePair<TreeDumperNode?, TreeDumperNode>>();
            stack.Push(new KeyValuePair<TreeDumperNode?, TreeDumperNode>(null, this));
            while (stack.Count != 0)
            {
                var currentEdge = stack.Pop();
                yield return currentEdge;
                var currentNode = currentEdge.Value;
                foreach (var child in currentNode.Children.Where(x => x != null).Reverse())
                {
                    stack.Push(new KeyValuePair<TreeDumperNode?, TreeDumperNode>(currentNode, child));
                }
            }
        }
    }
}
