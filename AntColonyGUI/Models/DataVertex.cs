using GraphX.PCL.Common.Models;
using YAXLib;

namespace AntColonyGUI
{
    /* DataVertex класс для вершин. Он содержит все данные вершины, заданные пользователем.
     * Этот класс также должен быть производным от класса VertexBase  который обеспечивает свойства и методы,
     * обязательные для правильных операций GraphX.
     * Некоторые из полезных членов VertexBase являются:
     *  - ID это свойство, которое хранит уникальный положительный идентификационный номер. Свойство должно быть заполнено пользователем.
     */

    public class DataVertex: VertexBase
    {
        /// <summary>
        /// Описание
        /// </summary>
        public string Text { get; set; }
 
        #region Calculated or static props
        [YAXDontSerialize]
        public DataVertex Self
        {
            get { return this; }
        }

        public override string ToString()
        {
            return Text;
        }

        #endregion

        /// <summary>
        /// Конструктор без параметров для этого класса
        /// (требуется для YAXLib сериализации)
        /// </summary>
        public DataVertex():this("")
        {
        }

        public DataVertex(string text = "")
        {
            Text = text;
        }
    }
}