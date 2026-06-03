using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Dashboard.Models;
using Project_Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Windows.Data;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using System.Linq;

namespace Project_Dashboard.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<ProjectTask> Tasks { get; } = new();

        public ObservableCollection<string> ExistingCategories { get; } = new();

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

        public ICollectionView TasksView { get; }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    TasksView.Refresh();
                }
            }
        }

        private string _selectedPriorityFilter = "Все";
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    TasksView.Refresh();
                }
            }
        }

        private string _selectedStatusFilter = "Все";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    TasksView.Refresh();
                }
            }
        }

        private string _selectedSortProperty = "Без сортировки";
        public string SelectedSortProperty
        {
            get => _selectedSortProperty;
            set
            {
                if (SetProperty(ref _selectedSortProperty, value))
                {
                    ApplySorting();
                }
            }
        }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            TasksView = CollectionViewSource.GetDefaultView(Tasks);
            TasksView.Filter = FilterTask;

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

            // Link dependencies in memory
            foreach (var task in Tasks)
            {
                task.DependsOnTask = task.DependsOnTaskId.HasValue
                    ? Tasks.FirstOrDefault(t => t.Id == task.DependsOnTaskId.Value)
                    : null;
            }

            foreach (var task in Tasks)
            {
                task.UpdateTimelineOffsets(TimelineStartDate);
            }

            RecalculateStatistics();
            UpdateCategories();
            TasksView?.Refresh();
        }

        private void UpdateCategories()
        {
            var uniqueCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            uniqueCategories.Add("Общий");
            foreach (var task in Tasks)
            {
                if (!string.IsNullOrWhiteSpace(task.Category))
                {
                    uniqueCategories.Add(task.Category.Trim());
                }
            }
            ExistingCategories.Clear();
            foreach (var cat in uniqueCategories.OrderBy(c => c))
            {
                ExistingCategories.Add(cat);
            }
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

        private bool FilterTask(object obj)
        {
            if (obj is not ProjectTask task) return false;

            // 1. Поиск по тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim().ToLower();
                bool matchTitle = task.Title != null && task.Title.ToLower().Contains(search);
                bool matchDesc = task.Description != null && task.Description.ToLower().Contains(search);
                if (!matchTitle && !matchDesc) return false;
            }

            // 2. Фильтр по приоритету
            if (SelectedPriorityFilter != "Все")
            {
                if (task.Priority.ToString() != SelectedPriorityFilter)
                    return false;
            }

            // 3. Фильтр по статусу
            if (SelectedStatusFilter != "Все")
            {
                if (SelectedStatusFilter == "Выполненные" && task.Progress < 100)
                    return false;
                if (SelectedStatusFilter == "В процессе" && task.Progress >= 100)
                    return false;
            }

            return true;
        }

        private void ApplySorting()
        {
            TasksView.SortDescriptions.Clear();
            if (SelectedSortProperty == "Дата начала")
            {
                TasksView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            }
            else if (SelectedSortProperty == "Дедлайн")
            {
                TasksView.SortDescriptions.Add(new SortDescription("Deadline", ListSortDirection.Ascending));
            }
            else if (SelectedSortProperty == "Прогресс")
            {
                TasksView.SortDescriptions.Add(new SortDescription("Progress", ListSortDirection.Descending));
            }
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
                Progress = 0,
                Category = "Общий",
                DependsOnTaskId = null
            };

            var dialog = new TaskWindow(newTask, ExistingCategories, Tasks)
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
                Progress = task.Progress,
                Category = task.Category,
                DependsOnTaskId = task.DependsOnTaskId
            };

            var eligibleTasks = Tasks.Where(t => t.Id != task.Id).ToList();
            var dialog = new TaskWindow(clone, ExistingCategories, eligibleTasks)
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
                    task.Category = clone.Category;
                    task.DependsOnTaskId = clone.DependsOnTaskId;

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
        private void ExportTasks()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = "tasks_export.json",
                    Title = "Экспорт задач в JSON"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(Tasks, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonString);
                    MessageBox.Show("Экспорт успешно завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ImportTasks()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Импорт задач из JSON"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string jsonString = File.ReadAllText(openFileDialog.FileName);
                    var importedTasks = JsonSerializer.Deserialize<List<ProjectTask>>(jsonString);

                    if (importedTasks == null || importedTasks.Count == 0)
                    {
                        MessageBox.Show("В файле нет задач для импорта или неверный формат.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Импортировать {importedTasks.Count} задач? Существующие задачи сохранятся.", 
                        "Подтверждение импорта", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var task in importedTasks)
                        {
                            task.Id = 0;
                            if (string.IsNullOrWhiteSpace(task.Category))
                            {
                                task.Category = "Общий";
                            }
                            int newId = _databaseService.InsertTask(task);
                            task.Id = newId;
                            Tasks.Add(task);
                        }
                        UpdateTimeline();
                        MessageBox.Show("Импорт успешно завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ResetDatabase()
        {
            var result = MessageBox.Show("Вы действительно хотите удалить ВСЕ задачи из базы данных? Это действие необратимо.", 
                "Подтверждение сброса", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.ClearAllTasks();
                    Tasks.Clear();
                    UpdateTimeline();
                    MessageBox.Show("База данных успешно очищена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при очистке базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void GenerateDemoData()
        {
            var result = MessageBox.Show("Вы хотите добавить новый набор демонстрационных задач? Существующие задачи сохранятся.", 
                "Генерация демо-данных", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var demo1 = new ProjectTask("Змейка 3D", "Написать логику движения камеры", DateTime.Today, DateTime.Today.AddDays(2), PriorityLevel.High) { Progress = 40, Category = "Учеба" };
                    var demo2 = new ProjectTask("UI Редизайн", "Обновить стили кнопок до Windows 11", DateTime.Today.AddDays(1), DateTime.Today.AddDays(6), PriorityLevel.Medium) { Progress = 80, Category = "Работа" };
                    var demo3 = new ProjectTask("Рефакторинг", "Оптимизировать загрузку данных", DateTime.Today.AddDays(2), DateTime.Today.AddDays(12), PriorityLevel.Low) { Progress = 10, Category = "Работа" };
                    var demo4 = new ProjectTask("Уборка дома", "Навести порядок на рабочем столе", DateTime.Today, DateTime.Today.AddDays(1), PriorityLevel.Low) { Progress = 100, Category = "Личное" };

                    demo1.Id = _databaseService.InsertTask(demo1);
                    demo2.Id = _databaseService.InsertTask(demo2);
                    demo3.Id = _databaseService.InsertTask(demo3);
                    demo4.Id = _databaseService.InsertTask(demo4);

                    Tasks.Add(demo1);
                    Tasks.Add(demo2);
                    Tasks.Add(demo3);
                    Tasks.Add(demo4);

                    UpdateTimeline();
                    MessageBox.Show("Демонстрационные задачи успешно созданы!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при генерации демо-данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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