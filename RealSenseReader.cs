using System;
using System.Collections.Generic;
using System.Text;
using Intel.RealSense;

namespace iuF_Alexis_Gros_Mael_Lhoutellier
{
    /// Ouvre un flux camera
    class RealSenseReader
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

        private Config FromFile(this Config cfg, string file) {
            cfg.EnableDeviceFromFile(file, repeat: false);
            return cfg; 
        }

        private void SetupFilePipeline(String fileName)
        {
            pipeline = new Pipeline();
            using (var cfg = FromFile(new Config(), fileName))
            using (var pp = pipeline.Start(cfg))
            using (var dev = pp.Device)
            using (playback = PlaybackDevice.FromDevice(dev))
            {
                Console.WriteLine("Reading from : " + playback.FileName);
                playback.Realtime = false;
            }
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
            }
        }

        public Tuple<byte[], UInt16> getPixelInfos(int posX, int posY, byte[] colorArray, UInt16[] depthArray)
        {
            var color = getColor(posX, posY, colorArray);
            var depth = getDepth(CAMERA_WIDTH / 2, CAMERA_HEIGHT / 2, depthArray);

            return new Tuple<byte[], ushort>(color, depth);
        }

        // renvoie un tableau avec les composantes r,g,b du pixel demandé
        public byte[] getColor(int posX, int posY, byte[] colorArray)
        {
            int index = (posX + (posY * CAMERA_WIDTH)) * 3;
            return new byte[] { colorArray[index], colorArray[index + 1], colorArray[index + 2] };
        }

        public UInt16 getDepth(int posX, int posY, UInt16[] depthArray)
        {
            int index = posX + (posY * CAMERA_WIDTH);
            return depthArray[index];
        }
    }
}
