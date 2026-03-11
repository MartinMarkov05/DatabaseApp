public class QueryResult
{
    public DataBase.Utils.LinkedList<string> ColumnNames { get; set; } = new DataBase.Utils.LinkedList<string>();
    public DataBase.Utils.LinkedList<string[]> Rows { get; set; } = new DataBase.Utils.LinkedList<string[]>();
    public string Message { get; set; }
}