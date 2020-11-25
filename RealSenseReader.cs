using System;
using System.Collections.Generic;
using System.Text;
using Intel.RealSense;

namespace iuF_Alexis_Gros_Mael_Lhoutellier
{
    /// Ouvre un flux camera et permet d'accéder aux données de la frame courante
    public class RealSenseReader
    {
        public const int CAMERA_WIDTH = 640;
        public const int CAMERA_HEIGHT = 480;

        public const int FPS = 30;

        private Pipeline pipeline;
        private PlaybackDevice playback;

        // Stocke des valeurs de couleur et de profondeur de la dernière frame
        private byte[] colorArray = new byte[CAMERA_WIDTH * CAMERA_HEIGHT * 3];
        private ushort[] depthArray = new ushort[CAMERA_WIDTH * CAMERA_HEIGHT];
        private float[] verticesArray = new float[CAMERA_WIDTH * CAMERA_HEIGHT * 3];

        // Si le nom du fichier n'es pas vide, on lit à partir d'un fichier, sinon, on lit à partir de la caméra
        public RealSenseReader(String fileName="")
        {
            if (fileName == "")
            {
                SetupDevicePipeline();
            } else
            {
                SetupFilePipeline(fileName);
            }
        }
           
        // Permet d'accéder depuis l'extérieur au status du playback pour savoir quand arrêter de demander des frames
        public PlaybackStatus GetPlaybackDeviceStatus()
        {
            return playback.Status;
        }

        // Met en place un pipeline à partir d'un device realsense
        private void SetupDevicePipeline()
        {
            var cfg = new Config();
            cfg.EnableStream(Stream.Depth, CAMERA_WIDTH, CAMERA_HEIGHT, Format.Z16, FPS);
            cfg.EnableStream(Stream.Color, CAMERA_WIDTH, CAMERA_HEIGHT, Format.Rgb8, FPS);

            pipeline = new Pipeline();
            pipeline.Start(cfg);
            Console.WriteLine("Reading from Device");
        }

        // Crée une configuration à partir d'un fichier
        private Config ConfigFromFile(Config cfg, string file) {
            cfg.EnableDeviceFromFile(file, repeat: false);
            return cfg; 
        }

        // Met en place un pipeline à partir d'un fichier
        private void SetupFilePipeline(String fileName)
        {
            pipeline = new Pipeline();
            var cfg = ConfigFromFile(new Config(), fileName);
            var pp = pipeline.Start(cfg);
            var dev = pp.Device;
            playback = PlaybackDevice.FromDevice(dev);
            
            Console.WriteLine("Reading from : " + playback.FileName);
            playback.Realtime = false;
        }

        // Attend que des frames soient disponibles puis copie leur valeurs
        // de couleur et de profondeur pour pouvoir les traiter
        public void WaitThenProcessFrame()
        {
            using (var frames = pipeline.WaitForFrames())
            {
                Align align = new Align(Stream.Color).DisposeWith(frames);
                Frame aligned = align.Process(frames).DisposeWith(frames);
                FrameSet alignedframeset = aligned.As<FrameSet>().DisposeWith(frames);
                var colorFrame = alignedframeset.ColorFrame.DisposeWith(alignedframeset);
                var depthFrame = alignedframeset.DepthFrame.DisposeWith(alignedframeset);

                colorFrame.CopyTo(colorArray);
                depthFrame.CopyTo(depthArray);
                Points points = (new PointCloud()).Process<VideoFrame>(depthFrame).As<Points>();
                points.CopyVertices(verticesArray);
            }
        }

        // Donne toutes les informations disponible sur un pixel
        public Tuple<byte[], ushort, float[]> GetPixelInfos(int posX, int posY)
        {
            var color = GetColor(posX, posY);
            var depth = GetDepth(posX, posY);
            var coord = GetVertice(posX, posY);

            return new Tuple<byte[], ushort, float[]>(color, depth, coord);
        }

        // renvoie un tableau avec les composantes r,g,b du pixel demandé
        public byte[] GetColor(int posX, int posY)
        {
            int index = (posX + (posY * CAMERA_WIDTH)) * 3;
            return new byte[] { colorArray[index], colorArray[index + 1], colorArray[index + 2] };
        }

        // Donne la profondeur de ce pixel
        public ushort GetDepth(int posX, int posY)
        {
            int index = posX + (posY * CAMERA_WIDTH);
            return depthArray[index];
        }

        // Donne les coordonnées x,y,z de ce pixel dans l'espace
        public float[] GetVertice(int posX, int posY)
        {
            int index = posX + (posY * CAMERA_WIDTH);
            return new float[] { verticesArray[index], verticesArray[index + 1], verticesArray[index + 2] };
        }

        ~RealSenseReader()
        {
            pipeline.Stop();
            pipeline.Dispose();
        }
    }
}
