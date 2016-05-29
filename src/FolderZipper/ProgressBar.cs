using System;

namespace FolderZipper
{
    public class ProgressBar
    {
        private const int Blocks = 50;
        private const int Total = 100;
        private const float Onechunk = (1.0f * Blocks) / Total;
        private const string PreFix = "Progress : ";

        private int lastPosistion = -1;

        /// <summary>
        /// Draw a progress bar at the current cursor position.
        /// Be careful not to Console.WriteLine or anything whilst using this to show progress!
        /// </summary>
        /// <param name="progress">The position of the bar</param>
        public void Draw(int progress)
        {
            if (progress < 0)
            {
                throw new ArgumentException("", "progress");
            }

            // draw filled part
            int position = (int)(Onechunk * progress);

            if (position != lastPosistion)
            {
                Console.Write("\r");
                Console.Write(PreFix);
                Console.Write("["); //start
                Console.Write(new string('-', position));
                Console.Write(new string(' ', Blocks - position));
                Console.Write("] "); //end

                if (position == Blocks)
                {
                    Console.WriteLine();
                }

                lastPosistion = position;
            }
        }
    }
}