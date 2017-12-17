namespace FileSharing
{
    public class FileToShare
    {
        private string filename;
        private bool isDir;
        private string path;

       
        public FileToShare(string path, string filename, bool isDir)
        {
            this.path = path;
            this.filename = filename;
            this.isDir = isDir;

        }

        public string Filename
        {
            get
            {
                return filename;
            }

            set
            {
                filename = value;
            }
        }

        public bool IsDir
        {
            get
            {
                return isDir;
            }

            set
            {
                isDir = value;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
            }
        }
    }
}