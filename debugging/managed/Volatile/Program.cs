using System;
using System.Threading.Tasks;

namespace Volatile
{
    class Program
    {
        private static int a = 0;
        
        // Firstly run in Debug mode, then in Release
        static void Main(string[] args)
        {
            Task.Delay(1).ContinueWith(_ => a = 1);

            var iterations = 0;
            while (a != 1)
            {
                iterations++;
            }

            Console.WriteLine(iterations);
        }
    }
}