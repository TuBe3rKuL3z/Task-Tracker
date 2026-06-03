using Project_Dashboard.Models;
using System;
using System.Windows;

namespace Project_Dashboard
{
    public class DependencyChoice
    {
        public int? Id { get; set; }
        public string DisplayText { get; set; }
    }

    public partial class TaskWindow : Window
    {
        public ProjectTask Task { get; }

        public TaskWindow(ProjectTask task, 
            System.Collections.Generic.IEnumerable<string> existingCategories = null,
            System.Collections.Generic.IEnumerable<ProjectTask> otherTasks = null)
        {
            InitializeComponent();
            Task = task;
            DataContext = Task;

            if (existingCategories != null)
            {
                CategoryComboBox.ItemsSource = existingCategories;
            }

            if (otherTasks != null)
            {
                var choices = new System.Collections.Generic.List<DependencyChoice>();
                choices.Add(new DependencyChoice { Id = null, DisplayText = "<Нет зависимости>" });
                foreach (var other in otherTasks)
                {
                    choices.Add(new DependencyChoice { Id = other.Id, DisplayText = other.Title });
                }
                DependsOnComboBox.ItemsSource = choices;
            }
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
