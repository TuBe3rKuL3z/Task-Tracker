using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Project_Dashboard.Models
{
    public enum PriorityLevel { Low, Medium, High }

    public class ProjectTask : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _deadline;
        public DateTime Deadline
        {
            get => _deadline;
            set => SetProperty(ref _deadline, value);
        }

        private PriorityLevel _priority;
        public PriorityLevel Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        private double _progressValue; // Переименовал поле, чтобы не было конфликта с System.Progress
        public double Progress
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _category = "Общий";
        public string Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    OnPropertyChanged(nameof(CategoryBrush));
                    OnPropertyChanged(nameof(CategoryColor));
                }
            }
        }

        public string CategoryBrush
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Category)) return "#E0E0E0";
                
                string[] colors = {
                    "#E8F5E9", // Light Green
                    "#E3F2FD", // Light Blue
                    "#FFF3E0", // Light Orange
                    "#F3E5F5", // Light Purple
                    "#FFE0B2", // Light Amber
                    "#F0F4C3", // Lime
                    "#E0F2F1", // Teal
                    "#FFEBEE"  // Light Red
                };
                
                int hash = 0;
                foreach (char c in Category)
                {
                    hash += (int)c;
                }
                
                return colors[Math.Abs(hash) % colors.Length];
            }
        }

        public string CategoryColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Category) || Category == "Общий") return "#6200EE"; // Default purple
                
                string[] colors = {
                    "#4CAF50", // Green
                    "#2196F3", // Blue
                    "#FF9800", // Orange
                    "#9C27B0", // Purple
                    "#00BCD4", // Cyan
                    "#E91E63", // Pink
                    "#FF5722", // Deep Orange
                    "#3F51B5"  // Indigo
                };
                
                int hash = 0;
                foreach (char c in Category)
                {
                    hash += (int)c;
                }
                
                return colors[Math.Abs(hash) % colors.Length];
            }
        }

        private int _startColumn;
        public int StartColumn
        {
            get => _startColumn;
            set => SetProperty(ref _startColumn, value);
        }

        private int _columnSpan;
        public int ColumnSpan
        {
            get => _columnSpan;
            set => SetProperty(ref _columnSpan, value);
        }

        public ProjectTask()
        {
            StartDate = DateTime.Today;
            Deadline = DateTime.Today.AddDays(7);
            Priority = PriorityLevel.Medium;
            Progress = 0;
            Category = "Общий";
        }

        public ProjectTask(string title, string description, DateTime startDate, DateTime deadline, PriorityLevel priority)
        {
            Title = title;
            Description = description;
            StartDate = startDate;
            Deadline = deadline;
            Priority = priority;
            Progress = 0;
            Category = "Общий";
        }

        public void UpdateTimelineOffsets(DateTime timelineStart)
        {
            int startDays = (StartDate.Date - timelineStart.Date).Days;
            int totalDays = (Deadline.Date - StartDate.Date).Days + 1;

            if (startDays < 0)
            {
                totalDays += startDays;
                startDays = 0;
            }

            if (startDays >= 30)
            {
                StartColumn = 29;
                ColumnSpan = 1;
                return;
            }

            StartColumn = startDays;

            if (totalDays <= 0)
            {
                totalDays = 1;
            }

            if (StartColumn + totalDays > 30)
            {
                ColumnSpan = 30 - StartColumn;
            }
            else
            {
                ColumnSpan = totalDays;
            }
        }
    }
}