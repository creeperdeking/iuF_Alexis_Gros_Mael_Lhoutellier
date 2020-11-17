using System;
using System.Collections.Generic;
using System.Text;
using Intel.RealSense;

namespace iuF_Alexis_Gros_Mael_Lhoutellier
{
    /// Ouvre un flux camera
    public class RealSenseReader
    {
        public const int CAMERA_WIDTH = 640;
        public const int CAMERA_HEIGHT = 480;

        public const int FPS = 30;

        private Pipeline pipeline;
        private PlaybackDevice playback;
        public PlaybackStatus GetPlaybackDeviceStatus()
        {
            return playback.Status;
        }

        // Stocke des valeurs de couleur et de profondeur de la dernière frame
        private byte[] colorArray = new byte[CAMERA_WIDTH * CAMERA_HEIGHT * 3];
        private UInt16[] depthArray = new UInt16[CAMERA_WIDTH * CAMERA_HEIGHT];

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

        private void SetupDevicePipeline()
        {
            var cfg = new Config();
            cfg.EnableStream(Stream.Depth, CAMERA_WIDTH, CAMERA_HEIGHT, Format.Z16, FPS);
            cfg.EnableStream(Stream.Color, CAMERA_WIDTH, CAMERA_HEIGHT, Format.Rgb8, FPS);

            var pipe = new Pipeline();
            pipe.Start(cfg);
            Console.WriteLine("Reading from Device");
        }

        private Config FromFile(Config cfg, string file) {
            cfg.EnableDeviceFromFile(file, repeat: false);
            return cfg; 
        }

        private void SetupFilePipeline(String fileName)
        {
            pipeline = new Pipeline();
            var cfg = FromFile(new Config(), fileName);
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
            var frames = pipeline.WaitForFrames();

            Align align = new Align(Stream.Color).DisposeWith(frames);
            Frame aligned = align.Process(frames).DisposeWith(frames);
            FrameSet alignedframeset = aligned.As<FrameSet>().DisposeWith(frames);
            var colorFrame = alignedframeset.ColorFrame.DisposeWith(alignedframeset);
            var depthFrame = alignedframeset.DepthFrame.DisposeWith(alignedframeset);

            colorFrame.CopyTo(colorArray);
            depthFrame.CopyTo(depthArray);
        }

        public Tuple<byte[], UInt16> GetPixelInfos(int posX, int posY)
        {
            var color = GetColor(posX, posY);
            var depth = GetDepth(CAMERA_WIDTH / 2, CAMERA_HEIGHT / 2);

            return new Tuple<byte[], ushort>(color, depth);
        }

        // renvoie un tableau avec les composantes r,g,b du pixel demandé
        public byte[] GetColor(int posX, int posY)
        {
            int index = (posX + (posY * CAMERA_WIDTH)) * 3;
            return new byte[] { colorArray[index], colorArray[index + 1], colorArray[index + 2] };
        }

        public UInt16 GetDepth(int posX, int posY)
        {
            int index = posX + (posY * CAMERA_WIDTH);
            return depthArray[index];
        }
    }
}
