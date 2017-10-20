using System;
using System.Drawing;
using System.Net;

namespace FileSharing
{
    internal class User :IEquatable<User>
    {
        string ip { get; set; }
        string name { get; set; }
        Bitmap img { get; set; }
        DateTime timestamp { get; set; }
        bool is_selected { get; set; }

        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public string Address
        {
            get { return ip; }
            set { ip = value; }
        }

        public bool isSelected
        {
            get { return is_selected; }
            set { is_selected = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public Bitmap Image
        {
            get { return img; }
            set { img = value; }
        }


        public User(string address, string name, DateTime timestamp, Bitmap img)
        {

            this.ip = address;
            this.name = name;
            this.timestamp = timestamp;
            this.img = img;

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