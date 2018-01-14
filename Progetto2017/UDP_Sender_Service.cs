using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace Progetto2017
{
    class UDP_Sender_Service
    {
        public const long MAX_PHOTO_SIZE = 4000;

        public static void sender(string userName, string userImage) {

            var Client = new UdpClient();

            try
            {

                byte[] bytes = null;

                string path = userImage;

                var photo = System.IO.Path.GetFileNameWithoutExtension(path);

                var fi = new FileInfo(path);

                if (fi.Length > MAX_PHOTO_SIZE)
                {
                    using (var stream = DownscaleImage(System.Drawing.Image.FromFile(path)))
                    {
                        // salva in /..../<progetto>/bin/Debug
                        string newPath = photo + "-smaller.jpg";
                        string savePath = photo + "-RESIZED.jpg";
                        using (var file = File.Create(newPath))
                        {
                            stream.CopyTo(file);

                            Bitmap img = new Bitmap(file);
                            img.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                            Message.Udp_message dp = new Message.Udp_message { name = userName, image = img };

                            using (var ms = new MemoryStream())
                            {
                                var bf = new BinaryFormatter();
                                bf.Serialize(ms, dp);
                                bytes = ms.ToArray();
                            }
                        }
                    }
                }

                while (true)
                {


                    if (!Popup.isPrivate)
                    {

                        //Client.EnableBroadcast = true;
                        Client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 8889));

                    }
                    Thread.Sleep(1500);
                }
            }
            catch (Exception )
            {
                Client.Close();
            }
        }


        private static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, System.Drawing.Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (System.Drawing.Image)b;
        }

        private static MemoryStream DownscaleImage(System.Drawing.Image photo)
        {
            MemoryStream resizedPhotoStream = new MemoryStream();

            long resizedSize = 0;
            var quality = 95;
            long lastSizeDifference = 0;
            do
            {
                resizedPhotoStream.SetLength(0);

                EncoderParameters eps = new EncoderParameters(1);
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                ImageCodecInfo ici = GetEncoderInfo("image/jpeg");

                photo.Save(resizedPhotoStream, ici, eps);
                resizedSize = resizedPhotoStream.Length;

                long sizeDifference = resizedSize - MAX_PHOTO_SIZE;
                // Console.WriteLine(resizedSize + "(" + sizeDifference + " " + (lastSizeDifference - sizeDifference) + ")");
                lastSizeDifference = sizeDifference;
                quality--;

            } while (resizedSize > MAX_PHOTO_SIZE);

            resizedPhotoStream.Seek(0, SeekOrigin.Begin);
            //Console.WriteLine("Fine DownScale");
            return resizedPhotoStream;
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

    }
}
