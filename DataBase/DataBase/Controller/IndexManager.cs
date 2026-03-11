using DataBase.Model;
using DataBase.Utils;

namespace DataBase.Controller
{
    public class IndexManager
    {
        private readonly TableFileManager tableFile;
        private readonly IndexFileManager indexFile;

        public IndexManager(TableFileManager tableFile, IndexFileManager indexFile)
        {
            this.tableFile = tableFile;
            this.indexFile = indexFile;
        }

        public QueryResult CreateIndex(string indexName, string tableName, string colName)
        {
            var result = new QueryResult();
            var table = tableFile.GetTableByName(tableName);
            if (table == null)
            {
                result.Message = $"Table '{tableName}' does not exist.";
                return result;
            }

            int colIndex = tableFile.GetColumnIndex(table, colName);

            if (colIndex == -1)
            {
                result.Message = $"Column {colName} does not exist.";
                return result;
            }

            var index = indexFile.FindIndexByName(indexName);
            if(index != null)
            {
                result.Message = "The index already exists.";
                    return result;
            }
           

            Utils.HashIndex hashIndex = tableFile.CreateIndex(indexName, table, colIndex);
            indexFile.WriteIndex(tableName, colName, hashIndex);
            result.Message = $"Index '{indexName}' created successfully for column '{colName}' in table '{tableName}'.";
            return result;

        }

       public QueryResult DropIndex(string indexName)
{
    QueryResult result = new QueryResult();
    bool isDeleted = false;
    HashIndex index = indexFile.FindIndexByName(indexName);
            if (index != null)
            {
               isDeleted = indexFile.DropIndex(index);
            }
            else
            {
                result.Message = $"THese is no index named: {indexName}";
                return result;
            }

    if(isDeleted)
    result.Message = "Deleted sucessfully";
    else
    result.Message = "Delete unsuccesfull";
    
    return result;
   
}

        public void UpdateTableIndexes(Table table, object[] parsedValues, long recordOffset)
        {
            var colNode = table.Columns.Head;
            int colIndex = 0;

            while (colNode != null)
            {
                HashIndex idx = indexFile.GetIndexForColumn(table.Name, colNode.Value.Name);

                if (idx != null)
                {
                    string valToInsert = TextUtilities.FormatValue(parsedValues[colIndex], colNode.Value.Type);
                    idx.Add(valToInsert, recordOffset);
                    indexFile.WriteIndex(table.Name, colNode.Value.Name, idx);
                }

                colNode = colNode.Next;
                colIndex++;
            }
        }
    }
}

