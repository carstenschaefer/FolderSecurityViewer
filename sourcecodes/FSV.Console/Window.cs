// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.Console
{
    using System;
    using CS = System.Console;

    public static class Window
    {
        private const ConsoleColor InfoColor = ConsoleColor.Yellow;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor TextColor = ConsoleColor.White;
        private const ConsoleColor ProgressColor = ConsoleColor.Magenta;

        internal static void Info<T>(T value)
        {
            ChangeForeColor(InfoColor);
            CS.WriteLine(value);
        }

        internal static void Info(string format, params object[] parameters)
        {
            ChangeForeColor(InfoColor);
            CS.WriteLine(format, parameters);
        }

        internal static void Error<T>(T value)
        {
            ChangeForeColor(ErrorColor);
            CS.WriteLine(value);
        }

        internal static void Error(string format, params object[] parameters)
        {
            ChangeForeColor(ErrorColor);
            CS.WriteLine(format, parameters);
        }

        internal static void Text<T>(T value)
        {
            ChangeForeColor(TextColor);
            CS.WriteLine(value);
        }

        internal static void Text(string format, params object[] parameters)
        {
            ChangeForeColor(TextColor);
            CS.WriteLine(format, parameters);
        }

        internal static void Progress<T>(T value)
        {
            ChangeForeColor(ProgressColor);
            CS.Write(value);
        }

        internal static void Progress(string format, params object[] parameters)
        {
            ChangeForeColor(ProgressColor);
            CS.Write(format, parameters);
        }

        internal static void Line()
        {
            Line(1);
        }

        internal static void Line(int lines)
        {
            CS.Write(new string('\n', lines));
        }

        internal static void ResetColor()
        {
            CS.ResetColor();
        }

        // internal static void ReplacePrevious() => CS.Write("\r");
        internal static void ReplacePreviousLine()
        {
            int currentLineCursor = CS.CursorTop;
            CS.SetCursorPosition(0, CS.CursorTop);
            CS.Write(new string(' ', CS.WindowWidth));
            CS.SetCursorPosition(0, currentLineCursor);
        }

        internal static int Read()
        {
            return CS.Read();
        }

        internal static string ReadLine()
        {
            return CS.ReadLine();
        }

        private static void ChangeForeColor(ConsoleColor color)
        {
            CS.ForegroundColor = color;
        }
    }
}