using System;
using System.Collections.Generic;
using System.Text;

namespace Ormico.DbPatchManager.Logic
{
    class ProgressUi
    {
        private static readonly char[] spinner = { '-', '\\', '|', '/' };
        private static int spinnerPosition = -1;

        public static void PrintProgress()
        {
            spinnerPosition++;
            if (spinnerPosition < 0 || spinnerPosition > spinner.Length - 1)
            {
                spinnerPosition = 0;
            }

            if (Console.CursorLeft > 0)
            {
                try
                {
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
                catch (Exception e)
                {
                    // in case console position cannot be set
                }
            }
            Console.Write(spinner[spinnerPosition]);
        }
    }
}
