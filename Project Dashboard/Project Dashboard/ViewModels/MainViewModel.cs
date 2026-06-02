using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Dashboard.Models;
using Project_Dashboard.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace Project_Dashboard.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<ProjectTask> Tasks { get; } = new();

        private DateTime _timelineStartDate;
        public DateTime TimelineStartDate
        {
            get => _timelineStartDate;
            set => SetProperty(ref _timelineStartDate, value);
        }

        public ObservableCollection<string> TimelineHeaders { get; } = new();

        private int _totalTasksCount;
        public int TotalTasksCount
        {
            get => _totalTasksCount;
            set => SetProperty(ref _totalTasksCount, value);
        }

        private int _inProgressTasksCount;
        public int InProgressTasksCount
        {
            get => _inProgressTasksCount;
            set => SetProperty(ref _inProgressTasksCount, value);
        }

        private int _completedTasksCount;
        public int CompletedTasksCount
        {
            get => _completedTasksCount;
            set => SetProperty(ref _completedTasksCount, value);
        }

        private int _overdueTasksCount;
        public int OverdueTasksCount
        {
            get => _overdueTasksCount;
            set => SetProperty(ref _overdueTasksCount, value);
        }

        private double _overallProgressPercentage;
        public double OverallProgressPercentage
        {
            get => _overallProgressPercentage;
            set => SetProperty(ref _overallProgressPercentage, value);
        }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();

            try
            {
                var dbTasks = _databaseService.GetAllTasks();
                if (dbTasks.Count == 0)
                {
                    // Add demo tasks to DB and UI
                    var demo1 = new ProjectTask("Змейка 3D", "Написать логику движения камеры", DateTime.Today, DateTime.Today.AddDays(2), PriorityLevel.High) { Progress = 40 };
                    var demo2 = new ProjectTask("UI Редизайн", "Обновить стили кнопок до Windows 11", DateTime.Today.AddDays(1), DateTime.Today.AddDays(6), PriorityLevel.Medium) { Progress = 80 };
                    var demo3 = new ProjectTask("Рефакторинг", "Оптимизировать загрузку данных", DateTime.Today.AddDays(2), DateTime.Today.AddDays(12), PriorityLevel.Low) { Progress = 10 };

                    demo1.Id = _databaseService.InsertTask(demo1);
                    demo2.Id = _databaseService.InsertTask(demo2);
                    demo3.Id = _databaseService.InsertTask(demo3);

                    Tasks.Add(demo1);
                    Tasks.Add(demo2);
                    Tasks.Add(demo3);
                }
                else
                {
                    foreach (var task in dbTasks)
                    {
                        Tasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateTimeline();
        }

        private void UpdateTimeline()
        {
            DateTime start = DateTime.Today;
            foreach (var task in Tasks)
            {
                if (task.StartDate.Date < start)
                {
                    start = task.StartDate.Date;
                }
            }

            TimelineStartDate = start;

            TimelineHeaders.Clear();
            for (int i = 0; i < 30; i++)
            {
                TimelineHeaders.Add(start.AddDays(i).ToString("dd.MM"));
            }

            foreach (var task in Tasks)
            {
                task.UpdateTimelineOffsets(TimelineStartDate);
            }

            RecalculateStatistics();
        }

        private void RecalculateStatistics()
        {
            int total = Tasks.Count;
            int completed = 0;
            int inProgress = 0;
            int overdue = 0;
            double sumProgress = 0;

            DateTime today = DateTime.Today;

            foreach (var task in Tasks)
            {
                sumProgress += task.Progress;

                if (task.Progress >= 100)
                {
                    completed++;
                }
                else
                {
                    if (task.Progress > 0)
                    {
                        inProgress++;
                    }
                    
                    if (task.Deadline.Date < today)
                    {
                        overdue++;
                    }
                }
            }

            TotalTasksCount = total;
            CompletedTasksCount = completed;
            InProgressTasksCount = inProgress;
            OverdueTasksCount = overdue;
            OverallProgressPercentage = total > 0 ? sumProgress / total : 0;
        }

        [RelayCommand]
        private void AddTask()
        {
            var newTask = new ProjectTask
            {
                Title = "Новая задача",
                Description = "",
                StartDate = DateTime.Today,
                Deadline = DateTime.Today.AddDays(7),
                Priority = PriorityLevel.Medium,
                Progress = 0
            };

            var dialog = new TaskWindow(newTask)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    int id = _databaseService.InsertTask(newTask);
                    newTask.Id = id;
                    Tasks.Add(newTask);
                    UpdateTimeline();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления задачи в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void EditTask(ProjectTask task)
        {
            if (task == null) return;

            var clone = new ProjectTask
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                StartDate = task.StartDate,
                Deadline = task.Deadline,
                Priority = task.Priority,
                Progress = task.Progress
            };

            var dialog = new TaskWindow(clone)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _databaseService.UpdateTask(clone);

                    task.Title = clone.Title;
                    task.Description = clone.Description;
                    task.StartDate = clone.StartDate;
                    task.Deadline = clone.Deadline;
                    task.Priority = clone.Priority;
                    task.Progress = clone.Progress;

                    UpdateTimeline();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления задачи в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void DeleteTask(ProjectTask task)
        {
            if (task == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить задачу \"{task.Title}\"?", 
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.DeleteTask(task.Id);
                    Tasks.Remove(task);
                    UpdateTimeline();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления задачи из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                if (theme.GetBaseTheme() == BaseTheme.Dark)
                {
                    theme.SetBaseTheme(BaseTheme.Light);
                }
                else
                {
                    theme.SetBaseTheme(BaseTheme.Dark);
                }

                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка смены темы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}