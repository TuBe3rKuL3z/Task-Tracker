using Project_Dashboard.Models;
using System;
using System.Windows;

namespace Project_Dashboard
{
    public partial class TaskWindow : Window
    {
        public ProjectTask Task { get; }

        public TaskWindow(ProjectTask task)
        {
            InitializeComponent();
            Task = task;
            DataContext = Task;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Task.Title))
            {
                MessageBox.Show("Пожалуйста, введите название задачи.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Task.Deadline.Date < Task.StartDate.Date)
            {
                MessageBox.Show("Срок исполнения не может быть раньше даты начала.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
