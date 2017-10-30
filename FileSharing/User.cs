using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileSharing
{
    public class User :IEquatable<User> , INotifyPropertyChanged
    {
        string ip { get; set; }
        string name { get; set; }
        Bitmap img { get; set; }
        ImageBrush ib { get; set; }
        DateTime timestamp { get; set; }
        int progress { get; set; }
        int time_left { get; set; } 
        string label_time { get; set; }

        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public int Time_left
        {
            get { return time_left; }
            set
            {
                time_left = value;
                label_time = value.ToString();
                this.NotifyPropertyChanged("Time_left");
            }
        }

        public string Label_time
        {
            get { return label_time; }
            set
            {
                label_time = "Tempo rimanente stimato: "+value+ "sec.";
                this.NotifyPropertyChanged("Label_time");
            }

        }
        public int Progress {

            get { return progress; }
            set { progress = value;
                this.NotifyPropertyChanged("Progress");
            }
        }

        public ImageBrush Brush {

            get { return ib; }
            set { ib = value;  }

        }

        public string Address
        {
            get { return ip; }
            set { ip = value;
                this.NotifyPropertyChanged("Address");
            }
        }

        public string Name
        {
            get { return name; }
            set { name = value;
                 this.NotifyPropertyChanged("Name");
                  }
        }
        public Bitmap Image
        {
            get { return img; }
            set { img = value;
                this.NotifyPropertyChanged("Image");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public User(string address, string name, DateTime timestamp, Bitmap img,ImageBrush ib) 
        {

            this.ip = address;
            this.name = name;
            this.timestamp = timestamp;
            this.img = img;
            this.ib = ib;

        }


        public override bool Equals(object obj)
        {
            return this.Equals(obj as User);
        }

        public bool Equals(User u)
        {
            // If parameter is null, return false.
            if (Object.ReferenceEquals(u, null))
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, u))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != u.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (this.ip == u.ip);
        }

        public override int GetHashCode()
        {
            return   0x00010000;
        }
  

    }
}