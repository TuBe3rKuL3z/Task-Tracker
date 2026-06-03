using Microsoft.Data.Sqlite;
using Project_Dashboard.Models;
using System;
using System.Collections.Generic;

namespace Project_Dashboard.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string dbFileName = "tasks.db")
        {
            _connectionString = $"Data Source={dbFileName}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjectTasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        StartDate TEXT NOT NULL,
                        Deadline TEXT NOT NULL,
                        Priority INTEGER NOT NULL,
                        Progress REAL NOT NULL,
                        Category TEXT
                    );";
                command.ExecuteNonQuery();

                // Проверить наличие колонки Category (миграция для существующей БД)
                try
                {
                    var checkCommand = connection.CreateCommand();
                    checkCommand.CommandText = "SELECT Category FROM ProjectTasks LIMIT 1;";
                    using (checkCommand.ExecuteReader()) { }
                }
                catch
                {
                    var migrateCommand = connection.CreateCommand();
                    migrateCommand.CommandText = "ALTER TABLE ProjectTasks ADD COLUMN Category TEXT DEFAULT 'Общий';";
                    migrateCommand.ExecuteNonQuery();
                }
            }
        }

        public List<ProjectTask> GetAllTasks()
        {
            var tasks = new List<ProjectTask>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Title, Description, StartDate, Deadline, Priority, Progress, Category FROM ProjectTasks";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var task = new ProjectTask
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            StartDate = DateTime.Parse(reader.GetString(3)),
                            Deadline = DateTime.Parse(reader.GetString(4)),
                            Priority = (PriorityLevel)reader.GetInt32(5),
                            Progress = reader.GetDouble(6),
                            Category = reader.IsDBNull(7) ? "Общий" : reader.GetString(7)
                        };
                        tasks.Add(task);
                    }
                }
            }
            return tasks;
        }

        public int InsertTask(ProjectTask task)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO ProjectTasks (Title, Description, StartDate, Deadline, Priority, Progress, Category)
                    VALUES ($title, $description, $startDate, $deadline, $priority, $progress, $category);
                    SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$title", task.Title);
                command.Parameters.AddWithValue("$description", task.Description ?? "");
                command.Parameters.AddWithValue("$startDate", task.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$deadline", task.Deadline.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$priority", (int)task.Priority);
                command.Parameters.AddWithValue("$progress", task.Progress);
                command.Parameters.AddWithValue("$category", task.Category ?? "Общий");
                
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void UpdateTask(ProjectTask task)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE ProjectTasks
                    SET Title = $title,
                        Description = $description,
                        StartDate = $startDate,
                        Deadline = $deadline,
                        Priority = $priority,
                        Progress = $progress,
                        Category = $category
                    WHERE Id = $id;";
                command.Parameters.AddWithValue("$title", task.Title);
                command.Parameters.AddWithValue("$description", task.Description ?? "");
                command.Parameters.AddWithValue("$startDate", task.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$deadline", task.Deadline.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$priority", (int)task.Priority);
                command.Parameters.AddWithValue("$progress", task.Progress);
                command.Parameters.AddWithValue("$category", task.Category ?? "Общий");
                command.Parameters.AddWithValue("$id", task.Id);
                
                command.ExecuteNonQuery();
            }
        }

        public void DeleteTask(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM ProjectTasks WHERE Id = $id;";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
