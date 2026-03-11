
using DataBase.Model;
using DataBase.Utils;

namespace DataBase.Controller
{
    public class TableManager
    {

        private readonly TableFileManager tableFile;

        private readonly IndexManager indexManager;
        public TableManager(TableFileManager tableFile, IndexManager indexManager)
        {
            this.tableFile = tableFile;
            this.indexManager = indexManager;
        }

        public QueryResult CreateTable(string name, string[] command)
        {
            QueryResult result = new QueryResult();
            var columns = new Utils.LinkedList<Column>();

            for (int i = 0; i < command.Length; i++)
            {
                string[] parts = TextUtilities.SplitByString(command[i], ":");
                string columnName = parts[0];
                string columnType = "";
                string columnDefaultValue = "";
                string[] typeParts = TextUtilities.SplitByString(parts[1], " ");
                if (typeParts.Length > 1)
                {
                    columnType = typeParts[0];
                    columnDefaultValue = typeParts[2];
                    string newDefValue = "";
                    for (int j = 0; j < columnDefaultValue.Length; j++)
                    {
                        if (columnDefaultValue[j] != '‘' && columnDefaultValue[j] != '’')
                        {
                            newDefValue += columnDefaultValue[j];
                        }
                    }

                    columns.Add(new Column(columnName, columnType, newDefValue));
                }
                else
                {
                    columnType = parts[1];
                    columns.Add(new Column(columnName, columnType));
                }

            }

            var table = new Table(name, columns);
            tableFile.AddMetaDataToFile(table);
            result.Message = "Created Succesfully";
            return result;
        }



        public QueryResult DropTable(string tableName)
        {
            QueryResult result = new QueryResult();
            var isIsDeleted = tableFile.DeleteTable(tableName);
            if (isIsDeleted)
            {
                result.Message = "The table is deleted.";
            }

            else
            {
                result.Message = "Operation unsuccessful";
            }
            return result;

        }



        public QueryResult ShowInfo(string tableName)
        {
            QueryResult result = new QueryResult();
            string info = "";
            var table = tableFile.GetTableByName(tableName);
            if (table == null)
            {
                result.Message = "No table found";
                return result;
            }
            int liveRecords = tableFile.CountLiveRecords(table);
            long logicalSize = liveRecords * table.RecordSize;

            info += $"{table.Name} Info: \n";
            for (int i = 0; i < table.Columns.Count(); i++)
            {
                var column = table.Columns.GetAt(i);
                info += $"- {column.Name}:{column.Type} default:{column.DefaultValue}\n";
            }
            info += $"RecordCount: {liveRecords}\n";

            info += $"Size: {logicalSize} bytes\n";
            result.Message = info;
            return result;
        }


        public QueryResult Insert(string tableName, string[] columns, string[] values)
        {
            QueryResult result = new QueryResult();
            Table table = tableFile.GetTableMetadata(tableName);
            if (table == null)
            {
                result.Message = "Error: Table not found.";
                return result;
            }

            foreach (var item in columns)
            {
                bool found = ColumnExist(table.Columns, item);
                if (found)
                {
                    continue;
                }
                else
                {
                    result.Message = $"Column {item} does not exist in {table.Name}";
                    return result;
                }

            }

            for (int i = 0; i < values.Length; i++)
            {
                string cleanedValue = values[i].MyTrim('\'');
                cleanedValue = cleanedValue.MyTrim('\"');
                if (TextUtilities.MyIsNullOrWhiteSpace(cleanedValue))
                {
                    result.Message = "Values are not filled";
                    return result;
                }
            }

            object[] parsedValues = new object[table.Columns.Count()];

            var currentNode = table.Columns.Head;
            int colIndex = 0;

            while (currentNode != null)
            {
                Column col = currentNode.Value;
                int inputIndex = -1;

       
                for (int i = 0; i < columns.Length; i++)
                {
                    if (col.Name == columns[i])
                    {
                        inputIndex = i;
                        break;
                    }
                }

                if (inputIndex != -1)
                {
                 
                    try
                    {
                        parsedValues[colIndex] = ParseValue(values[inputIndex], col.Type);
               
                        if (col.Name == "Id")
                        {
                            
                            bool idExists = tableFile.CheckIfIdExists(table, parsedValues[colIndex]);

                            if (idExists)
                            {
                                result.Message = $"Error: A record with Id = {parsedValues[colIndex]} already exists in {table.Name}.";
                                return result;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        result.Message = "Error: " + e.Message;
                        return result;
                    }

                    if (parsedValues[colIndex] == null)
                    {
                        result.Message = "Error: Invalid data";
                        return result;
                    }
                }
                else
                {
                   
                    if (col.DefaultValue == "NULL")
                    {
                        result.Message = $"Error: Column '{col.Name}' has no default value. You must insert one!";
                        return result;
                    }

                    try
                    {
                        parsedValues[colIndex] = ParseValue(col.DefaultValue, col.Type);
                    }
                    catch (Exception e)
                    {
                        result.Message = "Error: " + e.Message;
                        return result;
                    }
                }

                currentNode = currentNode.Next;
                colIndex++;
            }

            long recordOffset = tableFile.InsertRecord(table, parsedValues);
            indexManager.UpdateTableIndexes(table, parsedValues, recordOffset);
            result.Message = $"Record was succesfully inserted in {table.Name}. Record count: {table.RecordCount}";
            return result;
        }



        private object ParseValue(string rawValue, string type)
        {

            if (string.IsNullOrEmpty(rawValue)) return null;

            switch (type.ToLower())
            {
                case "int":
                    if (!int.TryParse(rawValue, out int intResult))
                        throw new Exception($"Value '{rawValue}' is not a valid Integer.");
                    return intResult;

                case "double":
                    if (!double.TryParse(rawValue, out double doubleResult))
                        throw new Exception($"Value '{rawValue}' is not a valid Double.");
                    return doubleResult;

                case "date":
                    if (!DateTime.TryParseExact(rawValue, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dateResult))
                        throw new Exception($"Value '{rawValue}' is not a valid Date (expected dd.MM.yyyy).");
                    return dateResult;

                case "string":
                    return rawValue.MyTrim('\'');

                default:
                    return rawValue;
            }
        }


        public QueryResult GetRows(string[] rows, string tableName)
        {
            var result = new QueryResult();
            var table = tableFile.GetTableByName(tableName);
            if (table == null)
            {
                result.Message = "Table not found";
                return result;
            }

            int[] indices = new int[rows.Length];
            int id = 0;
            for (int i = 0; i < rows.Length; i++)
            {

                indices[i] = Convert.ToInt32(rows[i]);
                if (id == indices[i])
                {
                    result.Message = "Equal Ids can't be writen in this query!";
                    return result;

                }
                id = indices[i];
            }

            var colNode = table.Columns.Head;
            while (colNode != null)
            {
                result.ColumnNames.Add(colNode.Value.Name);
                colNode = colNode.Next;
            }

            Utils.LinkedList<Record> records = tableFile.GetRecord(indices, table);
            if (records == null || records.IsEmpty())
            {
                result.Message = "No records found";
                return result;
            }

            var recordNode = records.Head;
            while (recordNode != null)
            {
                result.Rows.Add(recordNode.Value.Values);
                recordNode = recordNode.Next;
            }
            
            return result;

        }





        public QueryResult Delete(string tableName, string[] rows)
        {
            var result = new QueryResult();
            var table = tableFile.GetTableByName(tableName);
            if (table == null)
            {
                result.Message = "No table found";
                return result;

            }

            int[] _rowsIndexes = new int[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                try
                {
                    _rowsIndexes[i] = Convert.ToInt32(rows[i]);
                    if (_rowsIndexes[i] < 1 || _rowsIndexes[i] > table.RecordCount)
                    {
                        result.Message = $"Error: Row {_rowsIndexes[i]} is out of bounds.";
                        return result;
                    }
                }
                catch (Exception e)
                {
                    result.Message = "Error: " + e.Message;
                    return result;

                }
            }
            for (int i = 0; i < _rowsIndexes.Length - 1; i++)
            {
                for (int j = 0; j < _rowsIndexes.Length - i - 1; j++)
                {
                    if (_rowsIndexes[j] < _rowsIndexes[j + 1])
                    {
                        int temp = _rowsIndexes[j];
                        _rowsIndexes[j] = _rowsIndexes[j + 1];
                        _rowsIndexes[j + 1] = temp;
                    }
                }
            }

            for (int i = 0; i < _rowsIndexes.Length - 1; i++)
            {
                if (_rowsIndexes[i] == _rowsIndexes[i + 1])
                {
                    result.Message = "Error: Duplicate row indices in query!";
                    return result;
                }
            }

            bool isSuccessful = tableFile.DeleteRows(table, _rowsIndexes);

            if (isSuccessful)
            {
                result.Message = "Deleted succesfully";
                return result;
            }
            else
            {
                result.Message = "Unsuccesful";
                return result;
            }
        }




        public QueryResult SelectWhere(string tableName, Utils.LinkedList<string> conditionals)
        {
            var result = new QueryResult();

            var table = tableFile.GetTableByName(tableName);
            if (table == null) return new QueryResult { Message = "No table found" };

            bool isDistinct = RemoveToken(conditionals, "DISTINCT");
            bool hasWhere = FindToken(conditionals, "WHERE");

            ParseAndRemoveOrderBy(conditionals, out string orderByColumn, out bool isAscending, table);

            Utils.LinkedList<string> selectedColumns = ExtractSelectedColumns(table, conditionals);
            if (selectedColumns == null || selectedColumns.IsEmpty())
            {
                result.Message = "Error: Invalid or empty column section";
                return result;
            }

            

            Utils.LinkedList<Record> records;
            if (hasWhere)
            {
                 var wherePart = ExtractWherePart(conditionals);
                records = tableFile.Where(table, wherePart);
            }
            else
            records = tableFile.GetRecord(table);
            

            if (records == null || records.IsEmpty())
            {
                result.Message = "No records found";
                result.ColumnNames = selectedColumns;
                return result;
            }

            if (isDistinct)
                records = ApplyDistinct(records, selectedColumns.GetAt(0), table);
            

            if (orderByColumn != null)
                ApplyOrderBy(records,table, orderByColumn, isAscending);
            

            foreach (var rec in records) 
            {
                result.Rows.Add(ExtractValues(rec, table, selectedColumns));
            }

            result.ColumnNames = selectedColumns;
            return result;

        }

        private Utils.LinkedList<string> ExtractWherePart(Utils.LinkedList<string> conditionals)
        {
            var wherePart = new Utils.LinkedList<string>();
            bool foundWhere = false;

            for (int i = 0; i < conditionals.Count(); i++)
            {
                string currentToken = conditionals.GetAt(i).MyToUpper();

                if (foundWhere)
                {
                    wherePart.Add(conditionals.GetAt(i));
                }
                else if (currentToken == "WHERE")
                {
                    foundWhere = true;
                }
            }
            return wherePart;
        }

        private void ParseAndRemoveOrderBy(Utils.LinkedList<string> conditionals, out string? orderByCol, out bool isAscending, Table table)
        {
            orderByCol = null;
            isAscending = true;

            for (int i = 0; i < conditionals.Count() - 2; i++)
            {
                if (conditionals.GetAt(i).MyToUpper() == "ORDER" && conditionals.GetAt(i + 1).MyToUpper() == "BY")
                {
                     orderByCol = conditionals.GetAt(i + 2);

                    if (ColumnExist(table.Columns, orderByCol))
                    {
                        int itemsToRemove = 3; 

                        if (i + 3 < conditionals.Count())
                        {
                            string direction = conditionals.GetAt(i + 3).MyToUpper();
                            if (direction == "DESC")
                            {
                                isAscending = false;
                                itemsToRemove = 4; 
                            }
                            else if (direction == "ASC")
                            {
                                isAscending = true;
                                itemsToRemove = 4; 
                            }
                        }

                        for (int j = 0; j < itemsToRemove; j++)
                        {
                            conditionals.RemoveAt(i);
                        }

                        return; 
                    }
                }
            }
        }

        private bool FindToken(Utils.LinkedList<string> conditionals, string token)
        {
            bool found = false;
            for (int i = 0; i < conditionals.Count(); i++)
            {
                if (conditionals.GetAt(i).MyToUpper() == token)
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        private Utils.LinkedList<string> ExtractSelectedColumns(Table table, Utils.LinkedList<string> conditionals)
        {
            Utils.LinkedList<string> selectedColumns = new Utils.LinkedList<string>();
            for (int i = 1; i < conditionals.Count(); i++)
            {
                string currentToken = conditionals.GetAt(i);
                if (currentToken.MyToUpper() == "FROM")
                {
                    break;
                }
                var colName = TextUtilities.MyTrim(currentToken, ',');
                if (colName == "*")
                {
                    var colNode = table.Columns.Head;
                    while (colNode != null)
                    {
                        selectedColumns.Add(colNode.Value.Name);
                        colNode = colNode.Next;
                    }
                }
                else if (ColumnExist(table.Columns, colName))
                {
                    selectedColumns.Add(colName);

                }
                else
                {
                    return null;
                }

            }
            return selectedColumns;
        }

        private bool RemoveToken(Utils.LinkedList<string> conditionals, string token)
        {
            for (int i = 0; i < conditionals.Count(); i++)
            {
                if (conditionals.GetAt(i).MyToUpper() == token.MyToUpper())
                {
                    conditionals.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private string[] ExtractValues(Record record, Table table, Utils.LinkedList<string> columns)
        {
            string[] filteredRow = new string[columns.Count()];

            var selectedColNode = columns.Head;
            int i = 0;
            while (selectedColNode != null)
            {
                string targetColName = selectedColNode.Value;

                int originalIndex = tableFile.GetColumnIndex(table, targetColName);

                if (originalIndex != -1)
                {
                    filteredRow[i] = record.Values[originalIndex];
                }

                selectedColNode = selectedColNode.Next;
                i++;
            }
            return filteredRow;
        }

        private void ApplyOrderBy(Utils.LinkedList<Record> records, Table table,string orederByColumn,bool isAsc)
        {
            if (records.Count() < 2) return;

              int colIndex = -1;
                string colType = "string";

                var colNode = table.Columns.Head;
                int currentIndex = 0;
                while (colNode != null)
                {
                    if (colNode.Value.Name == orederByColumn)
                    {
                        colIndex = currentIndex;
                        colType = colNode.Value.Type;
                        break;
                    }
                    colNode = colNode.Next;
                    currentIndex++;
                }
                if(colIndex == -1)
            {
                return;
            }

            bool swapped;
            do
            {
                swapped = false;
                var currentRec = records.Head;
                while (currentRec != null && currentRec.Next != null)
                {
                    int comparison = CompareValues(
                        currentRec.Value.Values[colIndex],
                        currentRec.Next.Value.Values[colIndex],
                        colType
                    );

                    if ((isAsc && comparison > 0) || (!isAsc && comparison < 0))
                    {
                        var temp = currentRec.Value;
                        currentRec.Value = currentRec.Next.Value;
                        currentRec.Next.Value = temp;
                        swapped = true;
                    }
                    currentRec = currentRec.Next;
                }
            } while (swapped);
        }

        private int CompareValues(string v1, string v2, string type)
        {
            try
            {
                if (type == "int") return int.Parse(v1).CompareTo(int.Parse(v2));
                if (type == "double") return double.Parse(v1).CompareTo(double.Parse(v2));

                if (type == "date")
                {
                    string format = "dd.MM.yyyy";
                    DateTime d1 = DateTime.ParseExact(v1.MyManualTrim(), format, System.Globalization.CultureInfo.InvariantCulture);
                    DateTime d2 = DateTime.ParseExact(v2.MyManualTrim(), format, System.Globalization.CultureInfo.InvariantCulture);
                    return d1.CompareTo(d2);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return string.Compare(v1, v2);
        }

        private Utils.LinkedList<Record> ApplyDistinct(Utils.LinkedList<Record> records, string colName, Table table)
        {
            var result = new Utils.LinkedList<Record>();
            if (records == null || records.Head == null) return result;

            var currentRecord = records.Head;
            int colIndex = tableFile.GetColumnIndex(table, colName);
            while (currentRecord != null)
            {
                bool isDuplicate = false;
                var currentResult = result.Head;

                while (currentResult != null)
                {
                    if (currentRecord.Value.Values[colIndex] == currentResult.Value.Values[colIndex])
                    {
                        isDuplicate = true;
                        break; 
                    }
                    currentResult = currentResult.Next;
                }

                if (!isDuplicate)
                {
                    result.Add(currentRecord.Value);
                }

                currentRecord = currentRecord.Next;
            }

            return result;
        }

        public QueryResult DeleteWhere(string tableName, Utils.LinkedList<string> deleteConditionals)
        {
            var result = new QueryResult();
            var table = tableFile.GetTableByName(tableName);
            if (table == null)
            {
                result.Message = "No table found";
                return result;

            }

            Utils.LinkedList<Record> records = tableFile.Where(table, deleteConditionals);
            int[] indices = new int[records.Count()];
            var currentNode = records.Head;
            int k = 0;
            while (currentNode != null)
            {
                long offset = currentNode.Value.Offset;
                indices[k] = (int)(offset / table.RecordSize) + 1;

                currentNode = currentNode.Next;
                k++;
            }

            for (int i = 0; i < indices.Length - 1; i++)
            {
                for (int j = 0; j < indices.Length - i - 1; j++)
                {
                    if (indices[j] < indices[j + 1])
                    {
                        int temp = indices[j];
                        indices[j] = indices[j + 1];
                        indices[j + 1] = temp;
                    }
                }
            }
            bool isSuccessful = tableFile.DeleteRows(table, indices);

            if (isSuccessful)
            {
                result.Message = "Deleted succesfully";
            }
            else
            {
                result.Message = "Unsuccesful";
            }
            return result;
        }

        public Utils.LinkedList<Table> GetAllTables()
        {

            var tables = tableFile.ReadAllTables();
            if (tables == null)
            {
                return null;
            }
            return tables;

        }

        private bool ColumnExist(Utils.LinkedList<Column> columns, string colName)
        {
            bool exists = false;
            for (int c = 0; c < columns.Count(); c++)
            {
                if (columns.GetAt(c).Name == colName)
                {
                    exists = true;
                    break;
                }
            }

            return exists;

        }
    }
}

