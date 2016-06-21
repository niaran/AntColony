using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AntColonyLib
{
    public class AntColony
    {
        private static Random random = new Random(0);
        private Thread thread;
        /// <summary>
        /// Количество городов
        /// </summary>
        private static int numCities;

        public delegate void InitializationCompletedHandler(
            int[][] dists, int[] bestTrail, double bestDistance);
        /// <summary>
        /// Информирует о завершении инциализации алгоритма.
        /// Несет пути муравьев и растояния между городами. 
        /// Путь и дистанцию лучшего муравья.
        /// </summary>
        public event InitializationCompletedHandler InitializationCompletedOn;
        public delegate void FoundNewBestTrailHandler(double bestDistance,
            int[] bestTrail, int attempt, long timerElapsedMilliseconds);
        /// <summary>
        /// Информирует о нахождении нового лучшего пути.
        /// Несет лучший маршрут, его длину и затраченное время на поиск в мс.
        /// </summary>
        public event FoundNewBestTrailHandler FoundNewBestTrailOn;
        public delegate void EndAlgorithmHandler(double bestDistance,
            int[] bestTrail, long timerElapsedMilliseconds);
        /// <summary>
        /// Информирует о конце работы алгоритма.
        /// Несет лучший маршрут, его длину и общее время поиска в мс.
        /// </summary>
        public event EndAlgorithmHandler EndAlgorithmOn;
        /// <summary>
        /// Влияние феромона. Значение по умолчанию 3.
        /// </summary>
        public static int ALPHA { get; private set; } = 3;
        /// <summary>
        /// Влияние на привлекательность пути. Значение по умолчанию 2.
        /// </summary>
        public static int BETA { get; private set; } = 2;
        /// <summary>
        /// Коэффициент испарения феромона. Значение по умолчанию 0.01.
        /// </summary>
        public static double RHO { get; private set; } = 0.01;
        /// <summary>
        /// Фактор вклада феромона. Значение по умолчанию 2.0.
        /// </summary>
        public static double Q { get; private set; } = 2.0;
        /// <summary>
        /// Минимальное растояние между городами.
        /// Для разброса при случайной инициализации.
        /// Значение по умолчанию 1.
        /// </summary>
        public static int scatterSD { get; private set; } = 1;
        /// <summary>
        /// Максимальное растояние между городами.
        /// Для разброса при случайной инициализации.
        /// Значение по умолчанию 9.
        /// </summary>
        public static int scatterED { get; private set; } = 9;
        
        /// <summary>
        /// Останавливает работу алгоритма.
        /// </summary>
        public void Stop()
        {
            thread.Abort();
            thread.Join(500);
        }
        /// <summary>
        /// Библиотека реализующая алгоритм оптимизации подражанием муравьиной колонии (АСО),
        /// для решения задачи коммивояжера (TSP).
        /// Решение состоит в нахождении минимального пути для посещения всех городов.
        /// Предпологаеться, что все города связаны друг с другом.
        /// Использует параметры по умолчанию.
        /// </summary>
        public AntColony()
        {
        }
        /// <summary>
        /// Библиотека реализующая алгоритм оптимизации подражанием муравьиной колонии (АСО),
        /// для решения задачи коммивояжера (TSP).
        /// Решение состоит в нахождении минимального пути для посещения всех городов.
        /// Предпологаеться, что все города связаны друг с другом.
        /// </summary>
        /// <param name="alpha">Влияние феромона.</param>
        /// <param name="beta">Влияние на привлекательность пути.</param>
        /// <param name="rho">Коэффициент испарения феромона.</param>
        /// <param name="q">Фактор вклада феромона.</param>
        public AntColony(int alpha, int beta, double rho, double q)
        {
            if (alpha <= 0 && beta <= 0 && rho <= 0 && q <= 0)
                throw new AntColonyException("Допускаються только положительные значения переданные в "
                    + "конструктор AntColonyLib");

            ALPHA = alpha;
            BETA = beta;
            RHO = rho;
            Q = q;
        }

        public void Start()
        {
            thread.Start();
        }

        public void Initialization(int numberCities, int numberAnts, int numberAttempts,
            int scatterStartDistance, int scatterEndDistance)
        {
            if(numberCities <= 0 && numberAnts <= 0 && numberAttempts <= 0 &&
                scatterStartDistance <= 0 && scatterEndDistance <= 0)
                throw new AntColonyException("Допускаються только положительные значения переданные методу Start");
            
            numCities = numberCities;
            scatterSD = scatterStartDistance;
            scatterED = scatterEndDistance;

            int[][] dists = MakeGraphDistances(numCities);
            int[][] ants = InitAnts(numberAnts, numCities);
            double[][] pheromones = InitPheromones(numCities);

            int[] bestTrail = BestTrail(ants, dists);
            double bestDistance = Length(bestTrail, dists);

            InitializationCompletedOn(dists, bestTrail, bestDistance);

            int attempt = 0;
            Stopwatch timer = Stopwatch.StartNew();
            thread = new Thread(() =>
            {
                while (attempt < numberAttempts)
                {
                    UpdateAnts(ants, pheromones, dists);
                    UpdatePheromones(pheromones, ants, dists);

                    int[] currBestTrail = BestTrail(ants, dists);
                    double currBestDistance = Length(currBestTrail, dists);
                    if (currBestDistance < bestDistance)
                    {
                        bestDistance = currBestDistance;
                        bestTrail = currBestTrail;
                        FoundNewBestTrailOn(bestDistance, bestTrail, attempt, timer.ElapsedMilliseconds);
                    }
                    attempt += 1;
                }
                timer.Stop();
                EndAlgorithmOn(bestDistance, bestTrail, timer.ElapsedMilliseconds);
            });

        }
        #region Стартовая инициализация
        /// <summary>
        /// Инициализирует случайные пути муравьёв.
        /// </summary>
        /// <param name="numAnts">Количество муравьёв.</param>
        /// <param name="numCities">Количество городов.</param>
        /// <returns></returns>
        private static int[][] InitAnts(int numAnts, int numCities)
        {
            int[][] ants = new int[numAnts][];
            for (int k = 0; k <= numAnts - 1; k++)
            {
                int start = random.Next(0, numCities);
                ants[k] = RandomTrail(start, numCities);
            }
            return ants;
        }
        /// <summary>
        /// Помощник для метода InitAnts
        /// </summary>
        /// <param name="start"></param>
        /// <param name="numCities"></param>
        /// <returns></returns>
        private static int[] RandomTrail(int start, int numCities)
        {
            int[] trail = new int[numCities];
            //Последовательный
            for (int i = 0; i <= numCities - 1; i++)
            {
                trail[i] = i;
            }
            //Тасование Fisher-Yates
            for (int i = 0; i <= numCities - 1; i++)
            {
                int r = random.Next(i, numCities);
                int tmp = trail[r];
                trail[r] = trail[i];
                trail[i] = tmp;
            }
            int idx = IndexOfTarget(trail, start);
            //Стартовый город на первом месте
            int temp = trail[0];
            trail[0] = trail[idx];
            trail[idx] = temp;

            return trail;
        }
        /// <summary>
        /// Инициализирует массив ферамонов начальным значением в 0,01.
        /// </summary>
        /// <param name="numCities">Количество городов.</param>
        /// <returns>Массив массивов ферамонов по всем путям.</returns>
        private static double[][] InitPheromones(int numCities)
        {
            double[][] pheromones = new double[numCities][];
            for (int i = 0; i <= numCities - 1; i++)
            {
                pheromones[i] = new double[numCities];
            }
            for (int i = 0; i <= pheromones.Length - 1; i++)
            {
                for (int j = 0; j <= pheromones[i].Length - 1; j++)
                {
                    pheromones[i][j] = 0.01;
                }
            }
            return pheromones;
        }
        #endregion
        #region Основной алгоритм
        /// <summary>
        /// Обновляет путь каждого муравья. Выберает случайный стартовый город
        /// и строит новый путь.
        /// </summary>
        /// <param name="ants"></param>
        /// <param name="pheromones"></param>
        /// <param name="dists"></param>
        private static void UpdateAnts(int[][] ants, double[][] pheromones, int[][] dists)
        {
            Parallel.For(0, ants.Length - 1, k =>
            {
                int start = random.Next(0, numCities);
                int[] newTrail = BuildTrail(k, start, pheromones, dists);
                ants[k] = newTrail;
            });
        }
        /// <summary>
        /// Помощник для UpdateAnts
        /// </summary>
        /// <param name="k"></param>
        /// <param name="start"></param>
        /// <param name="pheromones"></param>
        /// <param name="dists"></param>
        /// <returns></returns>
        private static int[] BuildTrail(int k, int start, double[][] pheromones, int[][] dists)
        {
            int[] trail = new int[numCities];
            bool[] visited = new bool[numCities];
            trail[0] = start;
            visited[start] = true;
            for (int i = 0; i <= numCities - 2; i++)
            {
                int cityX = trail[i];
                int next = NextCity(k, cityX, visited, pheromones, dists);
                trail[i + 1] = next;
                visited[next] = true;
            }
            return trail;
        }
        /// <summary>
        /// Помощник для BuildTrail. Выбирает следующий город в пути для любого k с visited[] в cityX.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="cityX"></param>
        /// <param name="visited"></param>
        /// <param name="pheromones"></param>
        /// <param name="dists"></param>
        /// <returns></returns>
        private static int NextCity(int k, int cityX, bool[] visited, double[][] pheromones, int[][] dists)
        {
            double[] probs = MoveProbs(k, cityX, visited, pheromones, dists);

            double[] cumul = new double[probs.Length + 1];
            for (int i = 0; i <= probs.Length - 1; i++)
            {
                cumul[i + 1] = cumul[i] + probs[i];
            }

            double p = random.NextDouble();

            for (int i = 0; i <= cumul.Length - 2; i++)
            {
                if (p >= cumul[i] && p < cumul[i + 1])
                {
                    return i;
                }
            }
            throw new AntColonyException("Отказ возврата действительного города в NextCity");
        }
        private static double[] MoveProbs(int k, int cityX, bool[] visited, double[][] pheromones, int[][] dists)
        {
            double[] taueta = new double[numCities];
            double sum = 0.0;
            for (int i = 0; i <= taueta.Length - 1; i++)
            {
                if (i == cityX)
                {
                    taueta[i] = 0.0;
                }
                else if (visited[i] == true)
                {
                    taueta[i] = 0.0;
                }
                else
                {
                    taueta[i] = Math.Pow(pheromones[cityX][i], ALPHA) * Math.Pow((1.0 / Distance(cityX, i, dists)), BETA);
                    if (taueta[i] < 0.0001)
                    {
                        taueta[i] = 0.0001;
                    }
                    else if (taueta[i] > (double.MaxValue / (numCities * 100)))
                    {
                        taueta[i] = double.MaxValue / (numCities * 100);
                    }
                }
                sum += taueta[i];
            }

            double[] probs = new double[numCities];
            for (int i = 0; i <= probs.Length - 1; i++)
            {
                probs[i] = taueta[i] / sum;
            }
            return probs;
        }
        /// <summary>
        /// Обновляет ферамоны на пути из каждого города в город.
        /// </summary>
        /// <param name="pheromones"></param>
        /// <param name="ants"></param>
        /// <param name="dists"></param>
        private static void UpdatePheromones(double[][] pheromones, int[][] ants, int[][] dists)
        {
            for (int i = 0; i <= pheromones.Length - 1; i++)
            {
                for (int j = i + 1; j <= pheromones[i].Length - 1; j++)
                {
                    for (int k = 0; k <= ants.Length - 1; k++)
                    {
                        double length = Length(ants[k], dists);
                        double decrease = (1.0 - RHO) * pheromones[i][j];
                        double increase = 0.0;
                        if (EdgeInTrail(i, j, ants[k]) == true)
                        {
                            increase = (Q / length);
                        }

                        pheromones[i][j] = decrease + increase;

                        if (pheromones[i][j] < 0.0001)
                        {
                            pheromones[i][j] = 0.0001;
                        }
                        else if (pheromones[i][j] > 100000.0)
                        {
                            pheromones[i][j] = 100000.0;
                        }

                        pheromones[j][i] = pheromones[i][j];
                    }
                }
            }
        }
        /// <summary>
        /// Определяет есть ли связь между городами на текущем пути муравья.
        /// </summary>
        /// <param name="cityX"></param>
        /// <param name="cityY"></param>
        /// <param name="trail"></param>
        /// <returns></returns>
        private static bool EdgeInTrail(int cityX, int cityY, int[] trail)
        {
            int lastIndex = trail.Length - 1;
            int idx = IndexOfTarget(trail, cityX);

            if (idx == 0 && trail[1] == cityY)
            {
                return true;
            }
            else if (idx == 0 && trail[lastIndex] == cityY)
            {
                return true;
            }
            else if (idx == 0)
            {
                return false;
            }
            else if (idx == lastIndex && trail[lastIndex - 1] == cityY)
            {
                return true;
            }
            else if (idx == lastIndex && trail[0] == cityY)
            {
                return true;
            }
            else if (idx == lastIndex)
            {
                return false;
            }
            else if (trail[idx - 1] == cityY)
            {
                return true;
            }
            else if (trail[idx + 1] == cityY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
        #region Вспомогательные методы
        /// <summary>
        /// Инициализирует пути между городами.
        /// </summary>
        /// <param name="numCities">Количество городов в пути.</param>
        /// <returns>Массив массивов в котором номер пути и растояние между
        /// городами в этом пути.</returns>
        private static int[][] MakeGraphDistances(int numCities)
        {
            int[][] dists = new int[numCities][];
            for (int i = 0; i <= dists.Length - 1; i++)
            {
                dists[i] = new int[numCities];
            }
            for (int i = 0; i <= numCities - 1; i++)
            {
                for (int j = i + 1; j <= numCities - 1; j++)
                {
                    int d = random.Next(scatterSD, scatterED);
                    dists[i][j] = d;
                    dists[j][i] = d;
                }
            }
            return dists;
        }
        /// <summary>
        /// Помощник для метода RandomTrail
        /// </summary>
        /// <param name="trail"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int IndexOfTarget(int[] trail, int target)
        {
            for (int i = 0; i <= trail.Length - 1; i++)
            {
                if (trail[i] == target)
                {
                    return i;
                }
            }
            throw new AntColonyException("Целевой объект не найден в IndexOfTarget");
        }
        /// <summary>
        /// Суммирует дистанции между городами.
        /// </summary>
        /// <param name="trail">Массив номеров городов в пути.</param>
        /// <param name="dists">Массив массивов дистанций между городами.</param>
        /// <returns>Всю длину маршрута.</returns>
        private static double Length(int[] trail, int[][] dists)
        {
            double result = 0.0;
            for (int i = 0; i <= trail.Length - 2; i++)
            {
                result += Distance(trail[i], trail[i + 1], dists);
            }
            return result;
        }
        /// <summary>
        /// Возвращает дистанцию между городами.
        /// </summary>
        /// <param name="cityX">Номер первого города в пути.</param>
        /// <param name="cityY">Номер второго города в пути.</param>
        /// <param name="dists">Массив растояний между городами в пути.</param>
        /// <returns></returns>
        private static double Distance(int cityX, int cityY, int[][] dists)
        {
            return dists[cityX][cityY];
        }
        /// <summary>
        /// Выберает лучший маршрут на основе общей длины дистанции.
        /// </summary>
        /// <param name="ants"></param>
        /// <param name="dists"></param>
        /// <returns>Лучший маршрут имеющий кратчайшую дистанцию.</returns>
        private static int[] BestTrail(int[][] ants, int[][] dists)
        {
            double bestLength = Length(ants[0], dists);
            int idxBestLength = 0;
            for (int k = 1; k <= ants.Length - 1; k++)
            {
                double len = Length(ants[k], dists);
                if (len < bestLength)
                {
                    bestLength = len;
                    idxBestLength = k;
                }
            }
            int[] bestTrail_Renamed = new int[numCities];
            ants[idxBestLength].CopyTo(bestTrail_Renamed, 0);
            return bestTrail_Renamed;
        }
    }
    #endregion
    public class AntColonyException : Exception
    {
        public AntColonyException(string message) : base(message)
        {
        }
    }
}