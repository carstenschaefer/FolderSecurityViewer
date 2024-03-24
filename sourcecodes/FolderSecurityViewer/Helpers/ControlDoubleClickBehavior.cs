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

namespace FolderSecurityViewer.Helpers
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public static class ControlDoubleClickBehavior
    {
        public static readonly DependencyProperty ExecuteCommand = DependencyProperty.RegisterAttached("ExecuteCommand",
            typeof(ICommand), typeof(ControlDoubleClickBehavior),
            new UIPropertyMetadata(null, OnExecuteCommandChanged));

        public static readonly DependencyProperty ExecuteCommandParameter = DependencyProperty.RegisterAttached("ExecuteCommandParameter",
            typeof(Window), typeof(ControlDoubleClickBehavior));

        public static ICommand GetExecuteCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ExecuteCommand);
        }

        public static void SetExecuteCommand(DependencyObject obj, ICommand command)
        {
            obj.SetValue(ExecuteCommand, command);
        }

        public static Window GetExecuteCommandParameter(DependencyObject obj)
        {
            return (Window)obj.GetValue(ExecuteCommandParameter);
        }

        public static void SetExecuteCommandParameter(DependencyObject obj, ICommand command)
        {
            obj.SetValue(ExecuteCommandParameter, command);
        }

        private static void OnExecuteCommandChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Control control)
            {
                control.MouseDoubleClick += ControlMouseDoubleClick;
            }
            else if (sender is Border border)
            {
                border.MouseLeftButtonDown += ControlMouseDoubleClick;
            }
        }

        private static void ControlMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Control control)
            {
                var command = control.GetValue(ExecuteCommand) as ICommand;
                object commandParameter = control.GetValue(ExecuteCommandParameter);

                if (command.CanExecute(e))
                {
                    command.Execute(commandParameter);
                }
            }
            else if (sender is Border border && e.ClickCount == 2)
            {
                var command = border.GetValue(ExecuteCommand) as ICommand;
                object commandParameter = border.GetValue(ExecuteCommandParameter);

                if (command.CanExecute(e))
                {
                    command.Execute(commandParameter);
                }
            }
        }
    }
}