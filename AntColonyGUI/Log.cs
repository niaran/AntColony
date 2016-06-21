using System;
using System.ComponentModel;


namespace AntColonyGUI
{
    public class Log : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string time;
        private string message;
        private string attempt;
        
        public string Time
        {
            get { return time; }
            set { time = value; RaisePropertyChanged("Time"); }
        }
        public string Message
        {
            get { return message; }
            set { message = value; RaisePropertyChanged("Message"); }
        }
        public string Attempt
        {
            get { return attempt; }
            set { attempt = value; RaisePropertyChanged("Attempt"); }
        }

        protected virtual void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
        protected void RaisePropertyChanged(string propertyName)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}