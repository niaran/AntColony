using GraphX.Controls;
using GraphX.PCL.Logic.Algorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Common.Enums;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AntColonyLib;

namespace AntColonyGUI
{
    public partial class MainWindow : Window, IDisposable
    {
        private Graph graph;
        private int[] bestTrail;
        private List<DataEdge> edges;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        public void Dispose()
        {
            Area.Dispose();
        }

        private GXLogicCore GetGraphArea(Graph graph)
        {
            var LogicCore = new GXLogicCore() { Graph = graph };

            LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;

            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            LogicCore.DefaultOverlapRemovalAlgorithmParams = LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 100;
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 100;

            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            LogicCore.AsyncAlgorithmCompute = true;
            return LogicCore;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var antColony = new AntColony();
            antColony.InitializationCompletedOn += AntColony_InitializationCompletedOn;
            antColony.FoundNewBestTrailOn += AntColony_FoundNewBestTrailOn;
            
            graph = new Graph();
            Area.SetEdgesDashStyle(EdgeDashStyle.Dot);
            Area.LogicCore = GetGraphArea(graph);

            antColony.Start(60, 4, 200, 1, 9);
        }

        private void AntColony_InitializationCompletedOn(int[][] ants, int[][] dists, int[] bestTrail, double bestDistance)
        {
            this.bestTrail = bestTrail;
            
            //Количество вершин равное количеству городов
            for (int i = 1; i < dists.Length + 1; i++)
            {
                graph.AddVertex(new DataVertex(i.ToString()) { ID = i });
            }

            var listVertex = graph.Vertices.ToList();
            //Соединим виршины каждую с каждой
            /*
            for (int i = 1; i < listVertex.Count + 1; i++)
            {
                for (int j = i; j < listVertex.Count + 1; j++)
                {
                    if (i != j)
                        graph.AddEdge(new DataEdge(listVertex[i - 1], listVertex[j - 1])
                        { Weight = dists[i - 1][j - 1] });
                }
                
            }*/
            edges = new List<DataEdge>(bestTrail.Length - 1);
            for (int i = 0; i < bestTrail.Length - 1; i++)
            {
                var vertA = listVertex[bestTrail[i]];
                var vertB = listVertex[bestTrail[i + 1]];
                var edge = new DataEdge(vertA, vertB);
                graph.AddEdge(edge);
                edges.Add(edge);
            }

            Area.GenerateGraph(true, true);
            Area.GenerateGraphFinished += Area_GenerateGraphFinished;
        }

        private void Area_GenerateGraphFinished(object sender, EventArgs e)
        {
            foreach (var control in Area.EdgesList.Values)
            {
                control.ShowArrows = false;
                //control.DashStyle = EdgeDashStyle.Dot;
                control.ShowLabel = false;
            }
            foreach (var edge in edges)
            {
                var control = Area.EdgesList[edge];
                control.ShowArrows = true;
                //control.DashStyle = EdgeDashStyle.Solid;
                control.ShowLabel = true;
            }

            zoomctrl.ZoomToOriginal();
        }

        private void AntColony_FoundNewBestTrailOn(double bestDistance, int attempt, long timerElapsedMilliseconds)
        {
            throw new NotImplementedException();
        }

        void gg_but_relayout_Click(object sender, RoutedEventArgs e)
        {
            //Этот метод инициирует relayout графа, процесс, который включает в себя последовательный вызов ко всем выбранным алгоритмам.
            //Он ведет себя как GenerateGraph() метод, за исключением того, что он не создает визуальный объект. Только обновяет уже имеющийся, используя текущий граф данных Area.Graph.
            Area.RelayoutGraph();
            zoomctrl.ZoomToFill();
        }
    }
}