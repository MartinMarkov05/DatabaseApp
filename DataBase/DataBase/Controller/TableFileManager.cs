using DataBase.Model;
using DataBase.Utils;

namespace DataBase.Controller
{
    public class TableFileManager
    {
        private readonly string _path;
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private IndexFileManager indexFile;



        public TableFileManager(string path, IndexFileManager indexFile)
        {
            _path = path;
            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
            this.indexFile = indexFile;

        }

        public Utils.LinkedList<Table> ReadAllTables()
        {
            if (_stream.Length < sizeof(long) + sizeof(int)) return new Utils.LinkedList<Table>();

            try
            {
                DataCheck(_stream);
            }
            catch (Exception ex)
            {
                throw new Exception("Error: Meta.data is corrupted! " + ex.Message);
            }

            _stream.Seek(sizeof(long), SeekOrigin.Begin);
            int tableCount = _reader.ReadInt32();
            var tables = new Utils.LinkedList<Table>();

            for (int t = 0; t < tableCount; t++)
            {
                string name = _reader.ReadString();
                int colCount = _reader.ReadInt32();

                var table = new Table(name, new Utils.LinkedList<Column>());


                for (int i = 0; i < colCount; i++)
                {
                    string colName = _reader.ReadString();
                    string colType = _reader.ReadString();
                    string defVal = _reader.ReadString();
                    table.Columns.Add(new Column(colName, colType, defVal));
                }

                table.RecordSize = _reader.ReadInt32();
                table.DataOffset = _reader.ReadInt64();
                table.RecordCount = _reader.ReadInt32();
                string directoryPath = Path.GetDirectoryName(_path);
                string dataFilePath = Path.Combine(directoryPath, $"{table.Name}.dat");
                if (!File.Exists(dataFilePath))
                {
                    table.RecordCount = 0;
                }

                tables.Add(table);
            }
            return tables;
        }


        private void DataCheck(FileStream _stream)
        {

            if (_stream.Length < sizeof(long)) return;

            _stream.Seek(0, SeekOrigin.Begin);
            long storedChecksum = _reader.ReadInt64();

            long calculatedChecksum = CalculateChecksum(_stream, _stream.Length - sizeof(long), offset: sizeof(long));

            if (storedChecksum != calculatedChecksum)
                throw new Exception("Database integrity check failed.");
        }




        public bool DeleteTable(string tableName)
        {
            var tables = ReadAllTables();
            Table tableToDelete = null;

            var currentNode = tables.Head;
            while (currentNode != null)
            {
                if (currentNode.Value.Name == tableName)
                {
                    tableToDelete = currentNode.Value;
                    tables.Remove(tableToDelete);
                    break;
                }
                currentNode = currentNode.Next;
            }

            if (tableToDelete == null) return false;

            WriteAllTables(tables);

            string directory = Path.GetDirectoryName(_path);
            string dataFilePath = Path.Combine(directory, $"{tableName}.dat");

            try
            {
                if (File.Exists(dataFilePath))
                {
                    File.Delete(dataFilePath);
                }
            }
            catch (IOException)
            {

            }
            return true;
        }



        public Table GetTableByName(string tableName)
        {
            try
            {
                var tables = ReadAllTables();

                foreach (var item in tables)
                {
                    if (item.Name == tableName)
                    {
                        return item;
                    }
                }
                return null;
            }
            catch (EndOfStreamException)
            {
                throw new Exception("Database file is corrupted (unexpected end).");
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading tables: " + ex.Message);
            }

        }

        public void WriteAllTables(Utils.LinkedList<Table> tables)
        {
            try
            {
                if (_stream.Length >= 12)
                {
                    DataCheck(_stream);
                }
                _stream.SetLength(0);

                _writer.Write(0L);

                _writer.Write(tables.Count());

                var tableNode = tables.Head;
                while (tableNode != null)
                {
                    Table table = tableNode.Value;

                    _writer.Write(table.Name);
                    _writer.Write(table.Columns.Count());

                    var colNode = table.Columns.Head;
                    while (colNode != null)
                    {
                        Column col = colNode.Value;
                        _writer.Write(col.Name);
                        _writer.Write(col.Type);
                        _writer.Write(col.DefaultValue ?? "NULL");
                        colNode = colNode.Next;
                    }

                    _writer.Write(table.RecordSize);
                    _writer.Write(table.DataOffset);
                    _writer.Write(table.RecordCount);

                    tableNode = tableNode.Next;
                }

                _writer.Flush();
                UpdateMetadataChecksum(_stream);
            }
            catch (Exception ex)
            {
                throw new Exception("Грешка при презаписване на мета-данните: " + ex.Message);
            }
        }
        private long CalculateChecksum(FileStream stream, long length, long offset = 8)
        {
            long sum = 0;
            long originalPos = stream.Position;
            stream.Seek(offset, SeekOrigin.Begin);

            byte[] buffer = new byte[8192];
            long remaining = length;

            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = stream.Read(buffer, 0, toRead);
                if (read <= 0) break;

                for (int i = 0; i < read; i++)
                    sum += buffer[i];

                remaining -= read;
            }

            stream.Seek(originalPos, SeekOrigin.Begin);
            return sum;
        }


        public Utils.LinkedList<Record> GetRecord(int[] rowIndices, Table table)
{
    Utils.LinkedList<Record> matchingRecords = new Utils.LinkedList<Record>();
    string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");

    if (!File.Exists(dataFilePath)) return matchingRecords;

    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    using (var reader = new BinaryReader(fs))
    {
        int currentLiveIndex = 0; 

        for (int i = 0; i < table.RecordCount; i++)
        {
            long offset = (long)i * table.RecordSize;
            fs.Seek(offset, SeekOrigin.Begin);

            int id = reader.ReadInt32();

            if (id != -1) 
            {
                currentLiveIndex++; 

                bool isRequested = false;
                foreach (int requestedIdx in rowIndices)
                {
                    if (requestedIdx == currentLiveIndex)
                    {
                        isRequested = true;
                        break;
                    }
                }

                if (isRequested)
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    string[] recordValues = new string[table.Columns.Count()];
                    var currentNode = table.Columns.Head;

                    for (int j = 0; j < recordValues.Length; j++)
                    {
                        recordValues[j] = ReadValueByType(reader, currentNode.Value.Type);
                        currentNode = currentNode.Next;
                    }
                    matchingRecords.Add(new Record(recordValues, offset));
                }
            }
        }
    }
    return matchingRecords;
}
      


        private string ReadValueByType(BinaryReader reader, string type)
        {
            switch (type.ToLower())
            {
                case "int":
                    return $"{reader.ReadInt32()}";
                case "double":
                    return $"{reader.ReadDouble()}";
                case "date":
                    return new DateTime(reader.ReadInt64()).ToString("dd.MM.yyyy");
                case "string":
                    byte[] buffer = reader.ReadBytes(64);

                    int actualLength = 64;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] == 0)
                        {
                            actualLength = i;
                            break;
                        }
                    }
                    string s = System.Text.Encoding.UTF8.GetString(buffer, 0, actualLength);
                    return s.MyTrim(' ');

                default:
                    return "NULL";
            }
        }



        public bool DeleteRows(Table table, int[] sortedIndices)
{
    string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");
    if (!File.Exists(dataFilePath)) return false;

    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
    using (var writer = new BinaryWriter(fs))
    {
        foreach (int index in sortedIndices)
        {
            if (index < 1 || index > table.RecordCount) continue;

            long offset = (long)(index - 1) * table.RecordSize;
            fs.Seek(offset, SeekOrigin.Begin);

            writer.Write(-1); 
            
             writer.Write(new byte[table.RecordSize - 4]); 
        }
    }
    return true;
}

       
        public Utils.LinkedList<Record> Where(Table table, Utils.LinkedList<string> conditionals)
{
    Utils.LinkedList<Record> matchingRecords = new Utils.LinkedList<Record>();
    string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");

    if (!File.Exists(dataFilePath)) return matchingRecords;

    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    using (var reader = new BinaryReader(fs))
    {
        HashIndex hashIndex = null;
        string key = "";


        for (int i = 0; i < conditionals.Count(); i += 4)
        {
            if (i > 0 && conditionals.GetAt(i - 1) == "OR") continue;

            string col = conditionals.GetAt(i);
            string op = conditionals.GetAt(i + 1);
            
            if (op == "=")
            {
                hashIndex = indexFile.GetIndexForColumn(table.Name, col);
                if (hashIndex != null)
                {
                    key = conditionals.GetAt(i + 2);
                    break; 
                }
            }
        }

        if (hashIndex != null)
        {
            Utils.LinkedList<long> offsets = hashIndex.Find(key);
            if (offsets != null)
            {
                var offsetNode = offsets.Head;
                while (offsetNode != null)
                {
                    Record rec = ReadRecordAtOffset(reader, table, offsetNode.Value);

                    if (rec!= null && IsRecordValid(0, conditionals, rec.Values, table))
                    {
                        matchingRecords.Add(rec);
                    }
                    offsetNode = offsetNode.Next;
                }
            }
            return matchingRecords; 
        }

        for (int i = 0; i < table.RecordCount; i++)
        {
            long currentOffset = i * table.RecordSize;
            Record rec = ReadRecordAtOffset(reader, table, currentOffset);

            if (rec != null && IsRecordValid(0, conditionals, rec.Values, table))
            {
                matchingRecords.Add(rec);
            }
        }
    }

    return matchingRecords;
}

        private Record ReadRecordAtOffset(BinaryReader reader, Table table, long offset)
        {
            if (offset < 0 || offset + table.RecordSize > reader.BaseStream.Length)
                return null;

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            int firstValue = reader.ReadInt32();
            if (firstValue == -1)
            {
                return null; 
            }
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            string[] values = new string[table.Columns.Count()];
            var colNode = table.Columns.Head;

            for (int j = 0; j < values.Length; j++)
            {
                if (colNode == null) break;
                values[j] = ReadValueByType(reader, colNode.Value.Type);
                colNode = colNode.Next;
            }
            return new Record(values, offset);
        }

        private bool IsRecordValid(int index, Utils.LinkedList<string> conditionals, string[] allValues, Table table)
        {
            bool finalResult = EvaluateNextCondition(ref index, conditionals, allValues, table);

            while (index < conditionals.Count())
            {
                string logicOp = conditionals.GetAt(index++);

                bool nextConditionResult = EvaluateNextCondition(ref index, conditionals, allValues, table);

                if (logicOp == "AND")
                {
                    finalResult = finalResult && nextConditionResult;
                }
                else if (logicOp == "OR")
                {
                    finalResult = finalResult || nextConditionResult;
                }
            }

            return finalResult;
        }

        private bool EvaluateNextCondition(ref int index, Utils.LinkedList<string> conditionals, string[] allValues, Table table)
        {
            bool isNegated = false;

            if (conditionals.GetAt(index) == "NOT")
            {
                isNegated = true;
                index++;
            }

            string colName = conditionals.GetAt(index++);
            string op = conditionals.GetAt(index++);
            string right = conditionals.GetAt(index++);

            int colIndex = GetColumnIndex(table, colName);
            string left = allValues[colIndex];
            string cleanedRight = right.MyManualTrim(); 

            bool result = Compare(left, op, cleanedRight);

            return isNegated ? !result : result;
        }

        private bool Compare(string left, string op, string right)
        {
            DateTime leftDate, rightDate;
            if (DateTime.TryParseExact(left, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out leftDate) &&
                DateTime.TryParseExact(right, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out rightDate))
            {
                int cmp = leftDate.CompareTo(rightDate);
                return ApplyOperator(cmp, op);
            }

            double leftNum, rightNum;
            if (double.TryParse(left, out leftNum) &&
                double.TryParse(right, out rightNum))
            {
                int cmp = leftNum.CompareTo(rightNum);
                return ApplyOperator(cmp, op);
            }

            int stringCmp = string.Compare(left, right, StringComparison.Ordinal);
            return ApplyOperator(stringCmp, op);
        }


        private bool ApplyOperator(int cmp, string op)
        {
            if (op == "=") return cmp == 0;
            if (op == "<>") return cmp != 0;
            if (op == ">") return cmp > 0;
            if (op == "<") return cmp < 0;
            if (op == ">=") return cmp >= 0;
            if (op == "<=") return cmp <= 0;

            return false;
        }


        public int GetColumnIndex(Table table, string colName)
        {
            for (int i = 0; i < table.Columns.Count(); i++)
                if (table.Columns.GetAt(i).Name == colName)
                    return i;

            return -1;
        }





        public HashIndex CreateIndex(string indexName, Table table, int colIndex)
        {
            string directory = Path.GetDirectoryName(_path);
            string dataFilePath = Path.Combine(directory, $"{table.Name}.dat");

            using (FileStream dataStream = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader dataReader = new BinaryReader(dataStream))
            {
                Utils.HashIndex index = new HashIndex(1000);
                index.Name = indexName;
                index.TableName = table.Name;
                long columnOffset = 0;
                var currentNode = table.Columns.Head;

                for (int i = 0; i < colIndex; i++)
                {
                    string type = currentNode.Value.Type.ToLower();

                    if (type == "int") columnOffset += 4;
                    else if (type == "double") columnOffset += 8;
                    else if (type == "date") columnOffset += 8;
                    else if (type == "string") columnOffset += 64;

                    currentNode = currentNode.Next;
                }
                index.ColumnName = currentNode.Value.Name;
                string targetType = currentNode.Value.Type;
                for (int i = 0; i < table.RecordCount; i++)
                {
                    long recordStart = i * table.RecordSize;
                    dataStream.Seek(recordStart + columnOffset, SeekOrigin.Begin);
                    string key = ReadValueByType(dataReader, targetType);
                    index.Add(key, recordStart);
                }
                return index;
            }
        }

        public void AddMetaDataToFile(Table table)
        {
            string directory = Path.GetDirectoryName(_path);

            string dataFilePath = Path.Combine(directory, $"{table.Name}.dat");
            if (!File.Exists(dataFilePath))
            {
                using (var fs = File.Create(dataFilePath)) { }
            }
            int tableCount = 0;

            if (_stream.Length >= 12)
            {
                DataCheck(_stream);
                _stream.Seek(8, SeekOrigin.Begin);
                tableCount = _reader.ReadInt32();
            }
            else
            {
                _stream.Seek(0, SeekOrigin.Begin);
                _writer.Write(0L);
                _writer.Write(0);
            }

            _stream.Seek(0, SeekOrigin.End);
            _writer.Write(table.Name);
            _writer.Write(table.Columns.Count());


            int calculatedRowSize = 0;
            var currentNode = table.Columns.Head;
            while (currentNode != null)
            {
                var col = currentNode.Value;
                _writer.Write(col.Name);
                _writer.Write(col.Type);
                _writer.Write(col.DefaultValue ?? "NULL");
                calculatedRowSize += GetTypeSize(col.Type);
                currentNode = currentNode.Next;
            }

            _writer.Write(calculatedRowSize);
            _writer.Write(0L);
            _writer.Write(0);

            _stream.Seek(8, SeekOrigin.Begin);
            _writer.Write(tableCount + 1);
            _writer.Flush();

            UpdateMetadataChecksum(_stream);
        }

        private int GetTypeSize(string type)
        {
            return type.ToLower() switch
            {
                "int" => 4,
                "date" => 8,
                "string" => 64,
                "double" => 8,
                _ => 0
            };
        }

        public bool CheckIfIdExists(Table table, object newId)
        {
            string directory = Path.GetDirectoryName(_path);
            string dataFilePath = Path.Combine(directory, $"{table.Name}.dat");

            if (!File.Exists(dataFilePath)) return false;

            using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(fs))
            {
                for (int i = 0; i < table.RecordCount; i++)
                {
                    fs.Seek((long)i * table.RecordSize, SeekOrigin.Begin);

                    string currentId = ReadValueByType(reader, table.Columns.Head.Value.Type);

                    if (currentId == $"{newId}")
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public long InsertRecord(Table table, object[] values)
{
    string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");
    long targetOffset = -1;

    using (var fs = new FileStream(dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
    using (var reader = new BinaryReader(fs))
    {
        for (int i = 0; i < table.RecordCount; i++)
        {
            fs.Seek(i * table.RecordSize, SeekOrigin.Begin);
            if (reader.ReadInt32() == -1) 
            {
                targetOffset = i * table.RecordSize;
                break;
            }
        }

 
        if (targetOffset == -1)
        {
            targetOffset = fs.Length;
            table.RecordCount++;
        }


        fs.Seek(targetOffset, SeekOrigin.Begin);
        BinaryWriter writer = new BinaryWriter(fs);
         var colNode = table.Columns.Head;
                    for (int i = 0; i < values.Length; i++)
                    {
                        var value = values[i];
                        string type = colNode.Value.Type.ToLower();

                        if (type == "int")
                        {
                            writer.Write(value != null ? Convert.ToInt32(value) : 0);
                        }
                        else if (type == "double")
                        {
                            writer.Write(value != null ? Convert.ToDouble(value) : 0.0);
                        }
                        else if (type == "date")
                        {
                            long ticks = (value is DateTime dt) ? dt.Ticks : 0L;
                            writer.Write(ticks);
                        }
                        else if (type == "string")
                        {
                            string sVal = $"{value}" ?? "";
                            byte[] buffer = new byte[64];
                            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(sVal);
                            Array.Copy(stringBytes, buffer, Math.Min(stringBytes.Length, 64));
                            writer.Write(buffer);
                        }
                        colNode = colNode.Next;

                    }
                    writer.Flush();
                
    }
    DataCheck(_stream);
    UpdateRecordCount(table.Name, table.RecordCount);
    UpdateMetadataChecksum(_stream);
    return targetOffset;
}

       

        private void UpdateMetadataChecksum(FileStream stream)
        {
            long newChecksum = CalculateChecksum(stream, stream.Length - sizeof(long), offset: sizeof(long));

            stream.Seek(0, SeekOrigin.Begin);
            _writer.Write(newChecksum);
            _writer.Flush();
        }
        public void UpdateRecordCount(string tableName, int newCount)
        {
            if (_stream.Length < 12) return;

            _stream.Seek(8, SeekOrigin.Begin);
            int totalTables = _reader.ReadInt32();

            for (int i = 0; i < totalTables; i++)
            {
                string name = _reader.ReadString();
                int colCount = _reader.ReadInt32();

                if (name == tableName)
                {
                    for (int j = 0; j < colCount; j++)
                    {
                        _reader.ReadString();
                        _reader.ReadString();
                        _reader.ReadString();
                    }

                    _stream.Seek(4 + 8, SeekOrigin.Current);
                    _writer.Write(newCount);
                    _writer.Flush();
                    return;
                }
                else
                {
                    for (int j = 0; j < colCount; j++)
                    {
                        _reader.ReadString();
                        _reader.ReadString();
                        _reader.ReadString();
                    }
                    _stream.Seek(16, SeekOrigin.Current);
                }
            }
        }
        public Table GetTableMetadata(string targetName)
        {

            if (_stream.Length < 12) return null;

            _stream.Seek(8, SeekOrigin.Begin);

            int totalTables = _reader.ReadInt32();

            for (int i = 0; i < totalTables; i++)
            {
                string tableName = _reader.ReadString();
                int colCount = _reader.ReadInt32();

                if (tableName == targetName)
                {
                    var columns = new Utils.LinkedList<Column>();
                    for (int j = 0; j < colCount; j++)
                    {
                        string colName = _reader.ReadString();
                        string colType = _reader.ReadString();
                        string defValue = _reader.ReadString();
                        columns.Add(new Column(colName, colType, defValue));
                    }

                    int rowSize = _reader.ReadInt32();
                    long offset = _reader.ReadInt64();
                    int recordCount = _reader.ReadInt32();

                    return new Table(tableName, columns)
                    {
                        RecordSize = rowSize,
                        DataOffset = offset,
                        RecordCount = recordCount
                    };
                }
                else
                {
                    for (int j = 0; j < colCount; j++)
                    {
                        _reader.ReadString();
                        _reader.ReadString();
                        _reader.ReadString();
                    }
                    _stream.Seek(16, SeekOrigin.Current);
                }
            }

            return null;
        }

        internal Utils.LinkedList<Record> GetRecord(Table table)
        {
            Utils.LinkedList<Record> records = new Utils.LinkedList<Record>();
            string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");

            if (!File.Exists(dataFilePath)) return records;

            using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(fs))
            {
                for (int i = 0; i < table.RecordCount; i++)
                {
                    long currentOffset = (long)i * table.RecordSize;
                    Record rec = ReadRecordAtOffset(reader, table, currentOffset);
                    if (rec != null)
            {
                records.Add(rec);
            }
                }
            }
            return records;
        }

        public int CountLiveRecords(Table table)
        {
            int count = 0;
    string dataFilePath = Path.Combine(Path.GetDirectoryName(_path), $"{table.Name}.dat");
    if (!File.Exists(dataFilePath)) return 0;

    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read))
    using (var reader = new BinaryReader(fs))
    {
        for (int i = 0; i < table.RecordCount; i++)
        {
            fs.Seek(i * table.RecordSize, SeekOrigin.Begin);
            if (reader.ReadInt32() != -1) 
            {
                count++;
            }
        }
    }
    return count;
        }
    }
}

