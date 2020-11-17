using System;
using Intel.RealSense;


namespace iuF_Alexis_Gros_Mael_Lhoutellier
{
    public static class Program
    {
        static void Main(string[] args)
        {
            RealSenseReader readerFile = new RealSenseReader("test.bag");
            while (
                readerFile.GetPlaybackDeviceStatus() 
                != PlaybackStatus.Stopped)
            {
                readerFile.WaitThenProcessFrame();
                showPixelInfos(400, 300, readerFile);
            }
            Console.Clear();
            

            RealSenseReader readerCam = new RealSenseReader();
            while (true)
            {
                readerCam.WaitThenProcessFrame();
                showPixelInfos(400, 300, readerCam);
            }

        }

        static void showColor(byte[] color)
        {
            Console.WriteLine("color: [" + color[0].ToString() + "," + color[1].ToString() + "," + color[2].ToString() + "]");
        }

        static void showDepth(UInt16 depth)
        {
            Console.WriteLine("depth: " + depth.ToString());
        }

        static void quickClear(int nbLine)
        {
            Console.SetCursorPosition(0, 4);
            for (int i = 0; i < nbLine; i++)
            {
                Console.WriteLine("                               ");
            }
            Console.SetCursorPosition(0, 4);
        }

        static void showPixelInfos(int posX, int posY, RealSenseReader reader)
        {
            var colorDepth = reader.GetPixelInfos(posX, posY);
            quickClear(3);
            Console.WriteLine("--- ("+posX+","+posY+") ---");
            showColor(colorDepth.Item1);
            showDepth(colorDepth.Item2);
        }
    }
}
