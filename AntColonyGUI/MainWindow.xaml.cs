using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using AntColonyLib;
using DialogManagement;
using DialogManagement.Contracts;
using System.Collections.ObjectModel;

namespace AntColonyGUI
{
    public partial class MainWindow : Window, IDisposable
    {
        private Graph graph;
        private AntColony antColony;
        private Message message;
        private int[] bestTrail;
        public ObservableCollection<Log> Logs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Area.GenerateGraphFinished += Area_GenerateGraphFinished;
        }

        public void Dispose()
        {
            Area.Dispose();
        }
        //Настройки GraphArea
        private GXLogicCore GetGraphArea(Graph graph)
        {
            var LogicCore = new GXLogicCore() { Graph = graph };

            LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.SimpleRandom;

            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;

            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            LogicCore.AsyncAlgorithmCompute = true;
            return LogicCore;
        }

        //После инициализации AntColony можем генерировать граф.
        private void AntColony_InitializationCompletedOn(int[][] dists, int[] bestTrail, double bestDistance)
        {
            this.bestTrail = bestTrail;
            
            Logs.Add(new Log() { Time = "Инициализация алгоритма.",
                Message = "Наименьшая длина пути -> "
                + bestDistance, Attempt = "Начинаем оптимизацию."
            });
            //Количество вершин равное количеству городов
            for (int i = 1; i < dists.Length + 1; i++)
            {
                graph.AddVertex(new DataVertex(i.ToString()) { ID = i });
            }

            var listVertex = graph.Vertices.ToList();
            //Соединим виршины каждую с каждой
            for (int i = 1; i < listVertex.Count + 1; i++)
            {
                for (int j = i; j < listVertex.Count + 1; j++)
                {
                    if (i != j)
                    {
                        graph.AddEdge(new DataEdge(listVertex[i - 1], listVertex[j - 1])
                        { Weight = dists[i - 1][j - 1]});
                        graph.AddEdge(new DataEdge(listVertex[j - 1], listVertex[i - 1])
                        { Weight = dists[i - 1][j - 1]} );
                    }
                }
            }
            //Генерируем граф
            Area.GenerateGraph(true, true);
        }

        //После генерации графа покажем лучший путь.
        private void Area_GenerateGraphFinished(object sender, EventArgs e)
        {
            //Убираем стрелки
            foreach (var control in Area.EdgesList.Values)
            {
                control.ShowArrows = false;
                control.Foreground = Brushes.Gray;
            }
            //Выделяем лучший маршрут
            for (int i = 0; i < bestTrail.Length - 1; i++)
            {
                var edge = from edg in Area.EdgesList.Keys
                           where edg.Source.ID == bestTrail[i] + 1
                           where edg.Target.ID == bestTrail[i + 1] + 1
                           select edg;

                var control = Area.EdgesList[edge.First()];
                control.ShowArrows = true;
                control.DashStyle = EdgeDashStyle.Solid;
                control.ShowLabel = true;
                control.Foreground = Brushes.Black;
            }

            zoomctrl.ZoomToFill();

            antColony.FoundNewBestTrailOn += AntColony_FoundNewBestTrailOn;
            antColony.EndAlgorithmOn += AntColony_EndAlgorithmOn;
            antColony.Start();
            
            message.Close();
        }
        
        //После отображения лучшего пути, начинаем искать новые лучшие пути.
        private void AntColony_FoundNewBestTrailOn(double bestDistance, int[] bestTrail,
            int attempt, long timerElapsedMilliseconds)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Убираем предыдущий лучший маршрут
                foreach (var control in Area.EdgesList.Values)
                {
                    control.ShowArrows = false;
                    control.ShowLabel = false;
                    control.DashStyle = EdgeDashStyle.Dot;
                    control.Foreground = Brushes.Gray;
                }
                //Выделяем новый лучший маршрут
                for (int i = 0; i < bestTrail.Length - 1; i++)
                {
                    var edge = from edg in Area.EdgesList.Keys
                               where edg.Source.ID == bestTrail[i] + 1
                               where edg.Target.ID == bestTrail[i + 1] + 1
                               select edg;

                    var control = Area.EdgesList[edge.First()];
                    control.ShowArrows = true;
                    control.DashStyle = EdgeDashStyle.Solid;
                    control.ShowLabel = true;
                    control.Foreground = Brushes.Black;
                }

                Logs.Add(new Log()
                {
                    Time = "За время: " + timerElapsedMilliseconds.ToString() + " мс.",
                    Message = "Во время попытки № " + attempt,
                    Attempt = "Новая длина пути -> " + bestDistance
                });
                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                zoomctrl.ZoomToFill();
            }));
        }

        private void AntColony_EndAlgorithmOn(double bestDistance, int[] bestTrail, long timerElapsedMilliseconds)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Убираем предыдущий лучший маршрут
                foreach (var control in Area.EdgesList.Values)
                {
                    control.ShowArrows = false;
                    control.ShowLabel = false;
                    control.DashStyle = EdgeDashStyle.Dot;
                    control.Foreground = Brushes.Gray;
                }
                //Выделяем новый лучший маршрут
                for (int i = 0; i < bestTrail.Length - 1; i++)
                {
                    var edge = from edg in Area.EdgesList.Keys
                               where edg.Source.ID == bestTrail[i] + 1
                               where edg.Target.ID == bestTrail[i + 1] + 1
                               select edg;

                    var control = Area.EdgesList[edge.First()];
                    control.ShowArrows = true;
                    control.DashStyle = EdgeDashStyle.Solid;
                    control.ShowLabel = true;
                    control.Foreground = Brushes.Red;
                }

                Logs.Add(new Log()
                {
                    Time = "Время завершения: " + timerElapsedMilliseconds.ToString() + " мс.",
                    Message = "Наименьшая длина пути -> " + bestDistance,
                    Attempt = "Конец алгоритма."
                });
                zoomctrl.ZoomToFill();
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
            }));
        }

        private void bStart_Click(object sender, RoutedEventArgs e)
        {
            bStart.IsEnabled = false;
            bStop.IsEnabled = true;

            if (tbAlpha.Text == "" &&
                tbBeta.Text == "" &&
                tbQ.Text == "" &&
                tbRho.Text == ""
                )
            {
                message = new Message("Не правильно", "Перед началом заполните все коэффициенты.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }
            if (tbDE.Text == "" &&
                tbDS.Text == "" &&
                tbNumberAnts.Text == "" &&
                tbNumberCity.Text == "" &&
                tbAttempts.Text == ""
                )
            {
                message = new Message("Не правильно", "Перед началом заполните поиск.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }

            int alpha;
            int beta;
            double Q;
            double rho;
            try
            {
                alpha = Convert.ToInt32(tbAlpha.Text);
                beta = Convert.ToInt32(tbBeta.Text);
                Q = Convert.ToDouble(tbQ.Text);
                rho = Convert.ToDouble(tbRho.Text);
            }
            catch (FormatException)
            {
                message = new Message("Не правильно", "Неверный формат у коэффициентов.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }
            catch (OverflowException)
            {
                message = new Message("Не правильно", "Введенные коэффициенты слишком большие.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }
            if (alpha <= 0 &&
                beta <= 0 &&
                Q <= 0 &&
                rho <= 0
                )
            {
                message = new Message("Не правильно", "Коэффициенты могут быть только положительными.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }

            int de;
            int ds;
            int numberAnts;
            int attempts;
            int numberCity;
            try
            {
                de = Convert.ToInt32(tbDE.Text);
                ds = Convert.ToInt32(tbDS.Text);
                numberAnts = Convert.ToInt32(tbNumberAnts.Text);
                attempts = Convert.ToInt32(tbAttempts.Text);
                numberCity = Convert.ToInt32(tbNumberCity.Text);
            }
            catch (FormatException)
            {
                message = new Message("Не правильно", "Неверный формат у поиска.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }
            catch (OverflowException)
            {
                message = new Message("Не правильно", "Данные введенные в поиск слишком большие.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }
            if (de <= 0 &&
                ds <= 0 &&
                numberAnts <= 0 &&
                attempts <= 0 &&
                numberCity <= 0
                )
            {
                message = new Message("Не правильно", "Данные в поиске могут быть только положительными.");
                message.CreateMessageDialog(new DialogManager(this, Dispatcher));
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
                return;
            }


            message = new Message("Пожалуйста подождите...", "Пока идет генерация графа.");
            message.CreateWaitDialog(new DialogManager(this, Dispatcher),
                () =>
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        antColony = new AntColony(alpha, beta, rho, Q);
                        antColony.InitializationCompletedOn += AntColony_InitializationCompletedOn;

                        graph = new Graph();
                        Area.SetEdgesDashStyle(EdgeDashStyle.Dot);
                        Area.LogicCore = GetGraphArea(graph);

                        antColony.Initialization(numberCity, numberAnts, attempts, ds, de);
                    })));
        }

        private void bStop_Click(object sender, RoutedEventArgs e)
        {
            if (antColony != null)
            {
                antColony.Stop();
                bStart.IsEnabled = true;
                bStop.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logs = new ObservableCollection<Log>();
            DataContext = this;
        }
    }

    public class Message
    {
        private IMessageDialog _dialogManager;
        private string _caption;
        private string _message;


        public Message(string caption, string message)
        {
            _caption = caption;
            _message = message;
        }

        public void Close()
        {
            _dialogManager.Close();
        }

        public void CreateWaitDialog(IDialogManager dialogManager, Action worker = null, Action workerReady = null)
        {
            var waitDialog = dialogManager.CreateWaitDialog(_message, DialogMode.None);
            waitDialog.Caption = _caption;
            if (workerReady != null)
                waitDialog.WorkerReady += workerReady;
            waitDialog.CloseWhenWorkerFinished = false;
            _dialogManager = waitDialog;
            if (worker != null)
                waitDialog.Show(worker);
            else
                waitDialog.Show();
        }

        public void CreateMessageDialog(IDialogManager dialogManager, Action workerReady = null)
        {
            var messageDialog = dialogManager.CreateMessageDialog(_message, _caption, DialogMode.Ok);
            if (workerReady != null)
                messageDialog.Ok += workerReady;
            _dialogManager = messageDialog;
            messageDialog.Show();
        }

        public void CreateQuestioningDialog(IDialogManager dialogManager, Action workerYes, Action workerNo = null)
        {
            var messageDialog = dialogManager.CreateMessageDialog(_message, _caption, DialogMode.YesNo);
            messageDialog.Yes += workerYes;
            if (workerNo != null)
                messageDialog.No += workerNo;
            _dialogManager = messageDialog;
            messageDialog.Show();
        }
    }
}