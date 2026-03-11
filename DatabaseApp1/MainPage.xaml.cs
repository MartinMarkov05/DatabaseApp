using DataBase;
using DataBase.Utils;

namespace DatabaseApp1;
//using DataDase;

public partial class MainPage : ContentPage
{

    private readonly DatabaseEngine _databaseEngine;
    public MainPage()
    {
        InitializeComponent();
        _databaseEngine = new DatabaseEngine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory);
        LoadTables();
    }


    private void OnExecuteClicked(object sender, EventArgs e)
    {
        string input = CommandEntry.Text;
        var label = new Label();
        if (TextUtilities.MyIsNullOrWhiteSpace(input))
        {
            label = new Label
            {
                Text = "Empty command",
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap
            };
            ResultGrid.Add(label, 0, 0);
        }


        string[] command = TextUtilities.SplitByString(input, " ");
        string mainCommand = command[0].MyToUpper();


        QueryResult result = _databaseEngine.ExecuteCommand(input);

        switch (mainCommand)
        {
            case "CREATE":
                string secondCommand = command[1].MyToUpper();

                switch (secondCommand)
                {
                    case "TABLE":
                        LoadTables();
                        break;

                    case "INDEX":
                        ShowMessage(label, result.Message);
                        break;
                }
                break;

            case "INSERT":

                ShowMessage(label, result.Message);
                break;

            case "GET":
                if (result.Message != null)
                {
                    ShowMessage(label, result.Message);
                }
                else
                    PopulateResultGrid(result.Rows, result.ColumnNames);
                break;

            case "DROP":
                string secondCommandd = command[1].MyToUpper();
                switch (secondCommandd)
                {
                    case "TABLE":
                        ShowMessage(label, result.Message);
                        LoadTables();
                        break;

                    case "INDEX":
                        ShowMessage(label, result.Message);
                        break;
                }
                break;

            case "DELETE":
                switch (command[3])
                {
                    case "ROW":
                        ShowMessage(label, result.Message);
                        break;

                    case "WHERE":
                        ShowMessage(label, result.Message);
                        break;
                }
                break;
            case "SELECT":

                if (result.Message != null)
                    ShowMessage(label, result.Message);
                else
                    PopulateResultGrid(result.Rows, result.ColumnNames);
                break;

            default:
                result.Message = "Unknown command. Please use CREATE, SELECT, INSERT, DELETE, GET or DROP";
                ShowMessage(label, result.Message);
                break;
        }
    }

    private void ShowMessage(Label label, string result)
    {
        ResultGrid.Children.Clear();
        ResultGrid.RowDefinitions.Clear();
        ResultGrid.ColumnDefinitions.Clear();

        label.Text = result;
        ResultGrid.Add(label, 0, 0);
    }
    private void LoadTables()
    {
        TablesStack.Children.Clear();

        var tables = _databaseEngine.GetAllTables();
        if (tables == null || tables.IsEmpty())
            return;

        Color lightBlue = Color.FromArgb("#abb0d8ff"); 
        Color hoverBlue = Color.FromArgb("#caceecff");
        for (int i = 0; i < tables.Count(); i++)
        {
            string tableName = tables.GetAt(i).Name;
            var button = new Button
            {
                Text = tableName,
                TextColor = Colors.Black,
                HeightRequest = 45
            };

            var groups = VisualStateManager.GetVisualStateGroups(button);
            groups.Clear();

            var commonGroup = new VisualStateGroup { Name = "CommonStates" };

            // 1. NORMAL - Трябва да е дефиниран ПЪРВИ
            var normalState = new VisualState { Name = "Normal" };
            normalState.Setters.Add(new Setter { Property = Button.BackgroundColorProperty, Value = lightBlue });

            // 2. POINTER OVER - Когато мишката е отгоре
            var pointerOverState = new VisualState { Name = "PointerOver" };
            pointerOverState.Setters.Add(new Setter { Property = Button.BackgroundColorProperty, Value = hoverBlue });

            // 3. PRESSED - Когато се натисне
            var pressedState = new VisualState { Name = "Pressed" };
            pressedState.Setters.Add(new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Gray });

            // РЕДЪТ НА ДОБАВЯНЕ Е ВАЖЕН
            commonGroup.States.Add(normalState);
            commonGroup.States.Add(pointerOverState);
            commonGroup.States.Add(pressedState);

            groups.Add(commonGroup);

            // ВАЖНО: Форсираме началното състояние
            VisualStateManager.GoToState(button, "Normal");

            button.Clicked += OnTableButtonClicked;
            TablesStack.Children.Add(button);
        }
        Console.WriteLine($"Buttons count: {TablesStack.Children.Count}");

    }

    private void OnTableButtonClicked(object sender, EventArgs e)
    {
        var tableButton = sender as Button;
        if (tableButton == null)
            return;

        string tableName = tableButton.Text;

        QueryResult info = _databaseEngine.ExecuteCommand($"TABLEINFO {tableName}");

        ResultGrid.Children.Clear();
        ResultGrid.RowDefinitions.Clear();
        ResultGrid.ColumnDefinitions.Clear();

        ResultGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new Label
        {
            Text = info.Message,
            TextColor = Colors.Black,
            LineBreakMode = LineBreakMode.WordWrap
        };

        ResultGrid.Add(label, 0, 0);
    }

    public void PopulateResultGrid(DataBase.Utils.LinkedList<string[]> records,
    DataBase.Utils.LinkedList<string> columns)
    {
        ResultGrid.Children.Clear();
        ResultGrid.RowDefinitions.Clear();
        ResultGrid.ColumnDefinitions.Clear();

        int columnCount = columns.Count();
        int rowCount = records.Count() + 1; 


        for (int c = 0; c < columnCount; c++)
            ResultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });


        for (int r = 0; r < rowCount; r++)
            ResultGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });


        for (int c = 0; c < columnCount; c++)
        {
            var headerLabel = new Label
            {
                Text = columns.GetAt(c),
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                BackgroundColor = Colors.LightGray,
                Padding = new Thickness(5),
                HorizontalTextAlignment = TextAlignment.Center
            };
            ResultGrid.Add(headerLabel, c, 0);
        }


        for (int r = 0; r < records.Count(); r++)
        {
            var record = records.GetAt(r);
            for (int c = 0; c < columnCount; c++)
            {
                var cellLabel = new Label
                {
                    Text = record[c],
                    TextColor = Colors.Black,
                    Padding = new Thickness(5),
                    LineBreakMode = LineBreakMode.NoWrap
                };
                ResultGrid.Add(cellLabel, c, r + 1);
            }
        }
    }
}

