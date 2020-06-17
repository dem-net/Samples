using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DEM.Net.Graph.WeightedGraph
{
    /// <summary>
    /// Source https://stackoverflow.com/a/15310845/1818237
    /// </summary>
    public class Node
    {
        public string Name;
        public List<Arc> Arcs = new List<Arc>();

        public Node(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Create a new arc, connecting this Node to the Nod passed in the parameter
        /// Also, it creates the inversed node in the passed node
        /// </summary>
        public Node AddArc(Node child, float w)
        {
            Arcs.Add(new Arc
            {
                Parent = this,
                Child = child,
                Weigth = w
            });

            if (!child.Arcs.Exists(a => a.Parent == child && a.Child == this))
            {
                child.AddArc(this, w);
            }

            return this;
        }
    }

    public class Arc
    {
        public float Weigth;
        public Node Parent;
        public Node Child;

        public override string ToString()
        {
            return $"{Parent.Name} -> {Child.Name} ({Weigth})";
        }
    }

    public class Graph
    {
        public Node Root;
        public List<Node> AllNodes = new List<Node>();

        public Node CreateRoot(string name)
        {
            Root = CreateNode(name);
            return Root;
        }

        public Node CreateNode(string name)
        {
            var n = new Node(name);
            AllNodes.Add(n);
            return n;
        }

        public float?[,] CreateAdjMatrix()
        {
            float?[,] adj = new float?[AllNodes.Count, AllNodes.Count];

            for (int i = 0; i < AllNodes.Count; i++)
            {
                Node n1 = AllNodes[i];

                for (int j = 0; j < AllNodes.Count; j++)
                {
                    Node n2 = AllNodes[j];

                    var arc = n1.Arcs.FirstOrDefault(a => a.Child == n2);

                    if (arc != null)
                    {
                        adj[i, j] = arc.Weigth;
                    }
                }
            }
            return adj;
        }
    }

    static class GraphUtils
    {
        public static void PrintMatrix(Graph graph)
        {
            float?[,] adj = graph.CreateAdjMatrix();

            File.WriteAllText("matrix.txt", PrintMatrix(ref adj, graph.AllNodes.Count));
        }


        private static string PrintMatrix(ref float?[,] matrix, int Count)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("       ");
            for (int i = 0; i < Count; i++)
            {
                sb.AppendFormat("{0}  ", (char)('A' + i));
            }

            sb.AppendLine();

            for (int i = 0; i < Count; i++)
            {
                sb.AppendFormat("{0} | [ ", (char)('A' + i));

                for (int j = 0; j < Count; j++)
                {
                    if (i == j)
                    {
                        sb.Append(" &,");
                    }
                    else if (matrix[i, j] == null)
                    {
                        sb.Append(" .,");
                    }
                    else
                    {
                        sb.AppendFormat(" {0},", matrix[i, j]);
                    }

                }
                sb.AppendLine(" ]");
            }
            sb.AppendLine();

            return sb.ToString();
        }

    }
}