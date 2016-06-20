using GraphX.Controls;
using QuickGraph;

namespace AntColonyGUI
{
    /// <summary>
    /// Это пользовательское представление GraphArea с использованием пользовательских типов данных.
    /// GraphArea является компонентом визуальной панели отвечающим за отрисовку вершин и рёбер.
    /// Он также предоставляет множество глобальных настроек и методов, что делает GraphX таким настраиваемым и удобным.
    /// </summary>
    public class GraphArea : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }
}