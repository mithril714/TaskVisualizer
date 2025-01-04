using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskVisualizer
{
    public partial class Form1 : Form
    {
        private List<Task> tasks = new List<Task>();
        private Dictionary<Rectangle, Task> taskBounds = new Dictionary<Rectangle, Task>();
        private Dictionary<string, Color> categoryColors = new Dictionary<string, Color>();
        private Random random = new Random();
        private TextBox taskTextBox;
        private TextBox categoryTextBox;
        private NumericUpDown hoursNumeric;
        private Panel drawPanel;
        private ListBox taskListBox;
        private Rectangle totalHoursRect; // 合計時間グラフの矩形領域
        private Task selectedTask;


        public Form1()
        {
            InitializeComponent();
            this.Text = "Task Visualizer";
            this.Size = new Size(900, 600);

            // 入力エリア構築
            var inputPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            inputPanel.FlowDirection = FlowDirection.LeftToRight;

            // タスク内容入力
            var taskLabel = new Label { Text = "タスク内容:", AutoSize = true };
            taskTextBox = new TextBox { Text = "タスク内容", ForeColor = Color.Gray, Width = 200 };
            taskTextBox.GotFocus += RemovePlaceholder;
            taskTextBox.LostFocus += SetPlaceholder;

            // 分類入力
            var categoryLabel = new Label { Text = "分類:", AutoSize = true };
            categoryTextBox = new TextBox { Text = "分類", ForeColor = Color.Gray, Width = 150 };
            categoryTextBox.GotFocus += RemovePlaceholder;
            categoryTextBox.LostFocus += SetPlaceholder;

            // 工数入力
            var hoursLabel = new Label { Text = "予定工数:", AutoSize = true };
            hoursNumeric = new NumericUpDown
            {
                Minimum = 0.1M,
                Maximum = 100M,
                DecimalPlaces = 1,
                Increment = 0.1M,
                Width = 70
            };

            // 追加ボタン
            var addButton = new Button { Text = "追加", Width = 100 };
            addButton.Click += AddTask;

            // 削除ボタン
            var deleteButton = new Button { Text = "削除", Width = 100 };
            deleteButton.Click += DeleteSelectedTask;

            // 入力エリアにコントロールを追加
            inputPanel.Controls.Add(taskLabel);
            inputPanel.Controls.Add(taskTextBox);
            inputPanel.Controls.Add(categoryLabel);
            inputPanel.Controls.Add(categoryTextBox);
            inputPanel.Controls.Add(hoursLabel);
            inputPanel.Controls.Add(hoursNumeric);
            inputPanel.Controls.Add(addButton);
            inputPanel.Controls.Add(deleteButton);
            Controls.Add(inputPanel);

            // タスクリストボックス
            taskListBox = new ListBox { Dock = DockStyle.Left, Width = 250 };
            taskListBox.DisplayMember = "Content"; // タスクの内容を表示
            taskListBox.SelectedIndexChanged += (s, e) =>
            {
                selectedTask = taskListBox.SelectedItem as Task; // 選択されたタスクを記録
            };
            Controls.Add(taskListBox);

            // ボタンエリア
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.TopDown,
                Width = 120,
                Padding = new Padding(10)
            };

            // 保存ボタン
            var saveButton = new Button { Text = "保存", Width = 100 };
            saveButton.Click += SaveTasksToFile;

            // 読み込みボタン
            var loadButton = new Button { Text = "読み込み", Width = 100 };
            loadButton.Click += LoadTasksFromFile;

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(loadButton);
            Controls.Add(buttonPanel);

            // 描画エリア構築
            drawPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            drawPanel.Paint += DrawPanel_Paint;
            drawPanel.MouseClick += DrawPanel_MouseClick;
            Controls.Add(drawPanel);

            UpdateTaskListBox(); // 初期状態でリストを更新
        }


        public class Task
        {
            public string Content { get; set; }
            public string Category { get; set; }
            public float EstimatedHours { get; set; }
        }
        private void AddTask(object sender, EventArgs e)
        {
            string category = categoryTextBox.Text;
            if (string.IsNullOrWhiteSpace(category) || category == "分類") return;

            if (!categoryColors.ContainsKey(category))
            {
                categoryColors[category] = Color.FromArgb(
                    random.Next(100, 256),
                    random.Next(100, 256),
                    random.Next(100, 256)
                );
            }

            tasks.Add(new Task
            {
                Content = taskTextBox.Text == "タスク内容" ? "" : taskTextBox.Text,
                Category = category,
                EstimatedHours = (float)hoursNumeric.Value
            });

            taskTextBox.Text = "タスク内容"; taskTextBox.ForeColor = Color.Gray;
            categoryTextBox.Text = "分類"; categoryTextBox.ForeColor = Color.Gray;
            hoursNumeric.Value = 1;

            drawPanel.Invalidate(); // 再描画
            UpdateTaskListBox();
        }

        private void DeleteSelectedTask(object sender, EventArgs e)
        {
            if (selectedTask != null)
            {
                tasks.Remove(selectedTask); // タスクリストから削除
                selectedTask = null; // 選択解除
                drawPanel.Invalidate(); // 再描画
                UpdateTaskListBox(); // リストボックス更新
            }
        }

        private void SaveTasksToFile(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        foreach (var task in tasks)
                        {
                            writer.WriteLine($"{task.Content},{task.Category},{task.EstimatedHours:F1}");
                        }
                    }
                    MessageBox.Show("タスクを保存しました。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LoadTasksFromFile(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    tasks.Clear();
                    categoryColors.Clear();

                    using (StreamReader reader = new StreamReader(openFileDialog.FileName))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 3 && float.TryParse(parts[2], out float hours))
                            {
                                string category = parts[1];
                                if (!categoryColors.ContainsKey(category))
                                {
                                    categoryColors[category] = Color.FromArgb(
                                        random.Next(100, 256),
                                        random.Next(100, 256),
                                        random.Next(100, 256)
                                    );
                                }

                                tasks.Add(new Task
                                {
                                    Content = parts[0],
                                    Category = category,
                                    EstimatedHours = hours
                                });
                            }
                        }
                    }
                    drawPanel.Invalidate(); // 再描画
                    UpdateTaskListBox();
                    MessageBox.Show("タスクを読み込みました。", "読み込み成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            if (tasks.Count == 0) return; // タスクがない場合は描画をスキップ

            var groupedTasks = tasks.GroupBy(t => t.Category);
            var graphics = e.Graphics;

            int startY = 130; // 描画開始位置（Y座標）
            int rowHeight = 30; // 1行の高さ
            float cellWidth = 20f; // 1時間あたりの幅
            int startX = taskListBox.Width + 20; // ListBox の幅を考慮して描画開始位置を設定
                                                 

            taskBounds.Clear(); // 描画位置とタスクの対応をリセット

            using (Pen thickPen = new Pen(Color.Black, 2)) // 線の太さを指定
            {
                foreach (var group in groupedTasks)
                {
                    string category = group.Key;
                    Color categoryColor = categoryColors.ContainsKey(category)
                        ? categoryColors[category]
                        : Color.Gray;

                    Brush brush = new SolidBrush(categoryColor);

                    // 分類名を表示
                    graphics.DrawString(category, Font, Brushes.Black, startX, startY - 20);

                    float x = startX; // X方向の描画開始位置
                    foreach (var task in group)
                    {
                        var rect = new RectangleF(x, startY, task.EstimatedHours * cellWidth, rowHeight - 5);
                        graphics.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);

                        graphics.DrawRectangle(thickPen, rect.X, rect.Y, rect.Width, rect.Height);

                        taskBounds[new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height)] = task;

                        graphics.DrawString(task.Content, Font, Brushes.White, rect.X + 5, rect.Y + 5);

                        x += task.EstimatedHours * cellWidth;
                    }

                    startY += rowHeight + 20;
                }

                float totalHours = tasks.Sum(t => t.EstimatedHours);
                if (totalHours > 0)
                {
                    startY += 20;
                    graphics.DrawString("全体の工数", Font, Brushes.Black, startX, startY - 20);
                    var totalRect = new RectangleF(startX, startY, totalHours * cellWidth, rowHeight - 5);
                    graphics.FillRectangle(Brushes.Green, totalRect.X, totalRect.Y, totalRect.Width, totalRect.Height);
                    graphics.DrawRectangle(thickPen, totalRect.X, totalRect.Y, totalRect.Width, totalRect.Height);

                    graphics.DrawString($"合計: {totalHours:F1}時間", Font, Brushes.White, totalRect.X + 5, totalRect.Y + 5);

                    totalHoursRect = new Rectangle((int)totalRect.X, (int)totalRect.Y, (int)totalRect.Width, (int)totalRect.Height);
                }
            }
        }

        private void DrawPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (totalHoursRect.Contains(e.Location))
            {
                ShowAllTasks();
                return;
            }

            foreach (var kvp in taskBounds)
            {
                if (kvp.Key.Contains(e.Location))
                {
                    var task = kvp.Value;
                    MessageBox.Show($"タスク詳細:\n\n内容: {task.Content}\n分類: {task.Category}\n工数: {task.EstimatedHours:F1}時間",
                        "タスク詳細",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }
        }

        private void ShowAllTasks()
        {
            if (tasks.Count == 0)
            {
                MessageBox.Show("タスクがありません。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var taskDetails = string.Join("\n", tasks.Select(t =>
                $"内容: {t.Content}, 分類: {t.Category}, 工数: {t.EstimatedHours:F1}時間"));

            MessageBox.Show(taskDetails, "すべてのタスク", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateTaskListBox()
        {
            taskListBox.Items.Clear();
            foreach (var task in tasks)
            {
                taskListBox.Items.Add(task);
            }

            drawPanel.Invalidate();
        }

        private void RemovePlaceholder(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.ForeColor == Color.Gray)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        }

        private void SetPlaceholder(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox == taskTextBox) textBox.Text = "タスク内容";
                else if (textBox == categoryTextBox) textBox.Text = "分類";
                textBox.ForeColor = Color.Gray;
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.Run(new Form1());
        }
    }
}
