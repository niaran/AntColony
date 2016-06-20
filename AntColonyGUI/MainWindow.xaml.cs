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

            LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.FR;

            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            LogicCore.DefaultOverlapRemovalAlgorithmParams = LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;

            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            LogicCore.AsyncAlgorithmCompute = true;
            return LogicCore;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var antColony = new AntColony();
            antColony.InitializationCompletedOn += AntColony_InitializationCompletedOn;
            antColony.FoundNewBestTrailOn += AntColony_FoundNewBestTrailOn;
            antColony.Start(6, 4, 200, 1, 9);
        }

        private void AntColony_InitializationCompletedOn(int[][] ants, int[][] dists, int[] bestTrail, double bestDistance)
        {
            graph = new Graph();
            //Количество вершин равное количеству городов
            for (int i = 0; i < dists.Length; i++)
            {
                graph.AddVertex(new DataVertex(i.ToString()) { ID = i });
            }

            var listVertex = graph.Vertices.ToList();
            //Соединим виршины каждую с каждой

            for (int i = 0; i < (listVertex.Count * (listVertex.Count - 1) / 2); i++)
            {
                graph.AddEdge(new DataEdge(listVertex[i], listVertex[i + 1]));
            }
            
            for (int i = 0; i < bestTrail.Length - 1; i++)
            {
                graph.AddEdge(new DataEdge(listVertex[bestTrail[i]], listVertex[bestTrail[i + 1]]));
            }

            Area.LogicCore = GetGraphArea(graph);
            Area.GenerateGraph(true, true);
            Area.SetEdgesDashStyle(EdgeDashStyle.Dot);
            Area.ShowAllEdgesArrows(false);
            Area.ShowAllEdgesLabels(true);

            var edges = Area.EdgesList.Values;
            foreach (var edge in edges.Skip(Area.EdgesList.Count - bestTrail.Length))
            {
                edge.ShowArrows = true;
                edge.DashStyle = EdgeDashStyle.Solid;
            }

            ZoomControl.SetViewFinderVisibility(zoomctrl, System.Windows.Visibility.Visible);
            zoomctrl.ZoomToFill();
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