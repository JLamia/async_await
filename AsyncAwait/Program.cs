using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace AsyncAwait
{
    public class Node<T>
    {
        public T Val { get; private set; }
        public readonly int Ind;

        public Node(T val, int ind)
        {
            Val = val;
            Ind = ind;
        }
    }

    public class Graph<T>
    {
        private readonly List<Node<T>>[] _adjacencyList;
        private readonly List<Node<T>>[] _parentsList;
        private readonly Node<T>[] _allNodes;
        public readonly int Size;

        public Node<T> GetNode(int ind)
        {
            return _allNodes[ind];
        }

        public List<Node<T>> GetParents(Node<T> node)
        {
            return _parentsList[node.Ind];
        }


        public int GetParentsCount(Node<T> node)
        {
            return _parentsList[node.Ind].Count;
        }

        public int GetParentsCount(int ind)
        {
            return _parentsList[ind].Count;
        }

        public List<Node<T>> GetChildren(Node<T> node)
        {
            return _adjacencyList[node.Ind];
        }


        public Graph(int size)
        {
            Size = size;
            _adjacencyList = new List<Node<T>>[Size];
            _parentsList = new List<Node<T>>[Size];
            _allNodes = new Node<T>[Size];
        }

        public Graph(int size, Node<T>[] nodes, List<Tuple<int, int>> edges)
        {
            Size = size;

            _allNodes = new Node<T>[Size];
            _parentsList = new List<Node<T>>[Size];
            _adjacencyList = new List<Node<T>>[Size];

            for (int i = 0; i < Size; i++)
            {
                _parentsList[i] = new List<Node<T>>();
                _adjacencyList[i] = new List<Node<T>>();
            }

            _allNodes = nodes;

            foreach (var edge in edges)
            {
                var v = edge.Item1;
                var u = edge.Item2;
                _adjacencyList[v].Add(_allNodes[u]);
                _parentsList[u].Add(_allNodes[v]);
            }
        }
    }

    class Program
    {
        private static void ConsolePrint1()
        {
            Console.WriteLine(1);
            System.Threading.Thread.Sleep(Convert.ToInt32(1) * 100);
        }

        private static void ConsolePrint2()
        {
            Console.WriteLine(2);
            System.Threading.Thread.Sleep(Convert.ToInt32(2) * 100);
        }

        private static void ConsolePrint3()
        {
            Console.WriteLine(3);
            System.Threading.Thread.Sleep(Convert.ToInt32(3) * 100);
        }

        private static void ConsolePrint4()
        {
            Console.WriteLine(4);
            System.Threading.Thread.Sleep(Convert.ToInt32(4) * 100);
        }

        private static async Task ProcessInParallelAsync(Graph<Action> actions)
        {
            var allTasks = new List<Task>();
            var map = new Dictionary<Task, Node<Action>>();
            var finishedActions = new bool[actions.Size];

            for (var i = 0; i < actions.Size; i++)
            {
                if (actions.GetParentsCount(i) != 0) continue;
                var task = new Task(actions.GetNode(i).Val);
                allTasks.Add(task);
                map[task] = actions.GetNode(i);
            }

            foreach (var task in allTasks)
                task.Start();
            while (allTasks.Any())
            {
                var finished = await Task.WhenAny(allTasks);
                for (var i = 0; i < allTasks.Count; i++)
                {
                    var curTask = allTasks[i];
                    if (finished != curTask) continue;
                    var node = map[curTask];
                    finishedActions[node.Ind] = true;
                    foreach (var child in actions.GetChildren(node))
                    {
                        var canBeStarted = true;
                        foreach (var parent in actions.GetParents(child)
                            .Where(parent => !finishedActions[parent.Ind]))
                        {
                            canBeStarted = false;
                        }

                        if (!canBeStarted) continue;
                        var newTask = new Task(child.Val);
                        map[newTask] = child;
                        allTasks.Add(newTask);
                        //Console.WriteLine("Task {0} is started", child.Ind + 1);
                        allTasks.Last().Start();
                    }
                }
                allTasks.Remove(finished);
            }
        }

        static async Task Main(string[] args)
        {
            Action print1 = ConsolePrint1;
            Action print2 = ConsolePrint2;
            Action print3 = ConsolePrint3;
            Action print4 = ConsolePrint4;
            //var n = Convert.ToInt32(Console.ReadLine());
            var nodes = new Node<Action>[4];
            nodes[0] = new Node<Action>(print1, 0);
            nodes[1] = new Node<Action>(print2, 1);
            nodes[2] = new Node<Action>(print3, 2);
            nodes[3] = new Node<Action>(print4, 3);
            var aList = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(0, 1),
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(2, 3),
                //new Tuple<int, int>(2, 3),
                //new Tuple<int, int>(3, 1)
            };
            //Graph<Action> g = new Graph<Action>(4, nodes, aList);
            await ProcessInParallelAsync(new Graph<Action>(4, nodes, aList));
        }
    }
}