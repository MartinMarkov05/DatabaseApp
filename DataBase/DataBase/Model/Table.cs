using System;
using DataBase.Utils;


namespace DataBase.Model
{
    public class Table
    {
        public string Name { get; set; }
        public Utils.LinkedList<Column> Columns { get; set; }
        public int RecordCount { get; set; } = 0;
        public long DataOffset { get; set; } = 0;
        public long RecordSize { get; set; }


        public Table(string name, Utils.LinkedList<Column> columns)
        {
            Name = name;
            Columns = columns;

        }

    }
}
