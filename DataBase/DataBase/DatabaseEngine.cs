
using System.Runtime.InteropServices;
using DataBase.Controller;
using DataBase.Model;
using DataBase.Utils;

namespace DataBase
{
    public class DatabaseEngine
    {
        private readonly IndexFileManager indexFile;
        private readonly TableFileManager tableFile;
        private readonly IndexManager indexManager;
        private readonly TableManager tableManager;
        public DatabaseEngine(string basePath)
        {

            indexFile = new IndexFileManager(Path.Combine(basePath, "index.data"));
            tableFile = new TableFileManager(Path.Combine(basePath, "meta.data"), indexFile);
            indexManager = new IndexManager(tableFile, indexFile);
            tableManager = new TableManager(tableFile, indexManager);


        }

        public QueryResult ExecuteCommand(string input)
        {
            QueryResult result = new QueryResult();
            if (TextUtilities.MyIsNullOrWhiteSpace(input))
            {
                result.Message = "Empty command";
                return result;
            }


            int openIndex = -1;
            int closeIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '(' && openIndex == -1)
                    openIndex = i;
                else if (input[i] == ')')
                {
                    closeIndex = i;
                    break;
                }
            }
            string[] command = TextUtilities.SplitByString(input, " ");


            string mainCommand = command[0].MyToUpper();

            string tableName = "";

            switch (mainCommand)
            {
                case "CREATE":
                    string secondCommand = command[1].MyToUpper();

                    switch (secondCommand)
                    {
                        case "TABLE":
                            string[] beforeBrackets = GetCommandBeforeBrackets(input, openIndex);
                            string[] insideBrackets = GetCommandInsideBrackets(input, openIndex, closeIndex);
                            return tableManager.CreateTable(beforeBrackets[2], insideBrackets);



                        case "INDEX":
                            string indexName = command[2];
                            tableName = command[4];
                            string colName = command[5];
                            string newColName = "";
                            for (int i = 0; i < colName.Length; i++)
                            {
                                if(colName[i]!='(' && colName[i]!= ')')
                                newColName += colName[i]; 
                            }
                            return indexManager.CreateIndex(indexName, tableName, newColName);


                    }


                    break;
                case "TABLEINFO":
                    tableName = command[1];
                    return tableManager.ShowInfo(tableName);



                case "INSERT":
                    string[] beforeBrackets1 = GetCommandBeforeBrackets(input, openIndex);
                    string[] columnNames = GetCommandInsideBrackets(input, openIndex, closeIndex);

                    for (int i = closeIndex + 1; i < input.Length; i++)
                    {
                        if (input[i] == '(')
                        {
                            openIndex = i;
                        }
                        else if (input[i] == ')')
                        {
                            closeIndex = i;
                            break;
                        }
                    }

                    string[] columnValues = GetCommandInsideBrackets(input, openIndex, closeIndex);
                    tableName = beforeBrackets1[2];
                    return tableManager.Insert(tableName, columnNames, columnValues);

                case "GET":
                    if (command[1] != "ROW")
                    {
                        result.Message = "Invalid command!";
                        return result;
                    }
                    string[] rows = TextUtilities.SplitByString(command[2], ",");
                    return tableManager.GetRows(rows, command[4]);

                case "DROP":
                    string secondCommandd = command[1].MyToUpper();
                    switch (secondCommandd)
                    {

                        case "TABLE":
                            tableName = command[2];
                            return tableManager.DropTable(tableName);


                        case "INDEX":
                            string indexName = command[2];
                            return indexManager.DropIndex(indexName);

                    }

                    break;

                case "DELETE":
                    tableName = command[2];
                    switch (command[3])
                    {
                        case "ROW":
                            string[] _rows = TextUtilities.SplitByString(command[4], ",");
                            return tableManager.Delete(tableName, _rows);


                        case "WHERE":
                            DataBase.Utils.LinkedList<string> deleteConditionals = new DataBase.Utils.LinkedList<string>();
                            for (int i = 4; i < command.Length; i++)
                            {
                                deleteConditionals.Add(command[i]);
                            }
                            return tableManager.DeleteWhere(tableName, deleteConditionals);
                    }
                    break;


                case "SELECT":

                    DataBase.Utils.LinkedList<string> conditionals = new DataBase.Utils.LinkedList<string>();
                    for (int i = 0; i < command.Length; i++)
                    {
                        string trimmedValue = command[i].MyManualTrim();
                        if (!TextUtilities.MyIsNullOrWhiteSpace(trimmedValue))
                        {
                            conditionals.Add(command[i].MyManualTrim());
                        }
                    }


                    for (int i = 1; i < conditionals.Count(); i++)
                    {
                        if (conditionals.GetAt(i).MyToUpper() == "FROM")
                        {
                            tableName = conditionals.GetAt(i + 1);

                            break;

                        }

                    }
                    return tableManager.SelectWhere(tableName, conditionals);
                default:
                    Console.WriteLine("Unknown command. Try CREATE, SHOW, DROP or EXIT.");
                    break;
            }
            return null;
        }


        public Utils.LinkedList<Table> GetAllTables()
        {
            return tableManager.GetAllTables();
        }
        private static string[] GetCommandInsideBrackets(string input, int openIndex, int closeIndex)
        {
            string inside = "";
            for (int i = openIndex + 1; i < closeIndex; i++)
                inside += input[i];
            return TextUtilities.SplitByString(inside, ",");
        }

        private static string[] GetCommandBeforeBrackets(string input, int openIndex)
        {
            string before = "";
            for (int i = 0; i < openIndex; i++)
                before += input[i];


            return TextUtilities.SplitByString(before, " ");
        }

    }
}

