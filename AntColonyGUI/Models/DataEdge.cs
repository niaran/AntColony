using GraphX.PCL.Common.Models;
using YAXLib;

namespace AntColonyGUI
{
	/* DataEdge класс для ребер. Он содержит все данные ребра, заданные пользователем.
	 * Этот класс также должен быть производным от класса EdgeBase который обеспечивает свойства и методы,
	 * обязательные для правильных операций GraphX.
	 * Некоторые из полезных членов EdgeBase являются:
	 *  - ID это свойство, которое хранит уникальный положительный идентификационный номер. Свойство должно быть заполнено пользователем.
	 *  - IsSelfLoop это логическое свойство, которое указывает, если это ребро петлевое (например, имеющее одинаковое целевое и исходные вершины) 
	 *  - RoutingPoints набор точек используется для создания маршрута пути. Если Null, то прямая линия будет использоваться для рисования ребра.
	 *  В большинстве случаев это выполняется автоматически GraphX.
	 *  - Источник это свойство, которое имеет исходная вершина.
	 *  - Цель это свойство, которое имеет целевая вершина.
	 *  - Вес это свойство, которое имеет дополнительное значение веса ребра, которое может быть использовано в некоторых алгоритмах разметки.
	 */

	public class DataEdge : EdgeBase<DataVertex>
	{
		/// <summary>
		/// Конструктор по умолчанию. Нам нужно установить по крайней мере, источник и цель ребра.
		/// </summary>
		/// <param name="source">Источник ребра</param>
		/// <param name="target">Цель ребра</param>
		/// <param name="weight">Необязательно, вес ребра</param>
		public DataEdge(DataVertex source, DataVertex target, double weight = 1)
			: base(source, target, weight)
		{
		}
		/// <summary>
		/// Конструктор без параметров (для совместимости с сериализацией)
		/// </summary>
		public DataEdge()
			: base(null, null, 1)
		{
		}

		/// <summary>
		/// Пользовательское строковое свойство
		/// </summary>
		public string Text { get; set; }

		#region GET members
		public override string ToString()
		{
			return Text;
		}

		[YAXDontSerialize]
		public DataEdge Self
		{
			get { return this; }
		}
		#endregion
	}
}