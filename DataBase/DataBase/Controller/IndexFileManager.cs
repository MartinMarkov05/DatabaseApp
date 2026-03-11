using System;
using DataBase.Utils;


namespace DataBase.Controller
{
    public class IndexFileManager
    {
        private readonly string _path;
        private FileStream _stream;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        public Utils.LinkedList<HashIndex> loadedIndexes = new Utils.LinkedList<HashIndex>();
        public IndexFileManager(string path)
        {
            _path = path;
            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);
        }

        public void WriteIndex(string tableName, string colName, Utils.HashIndex hashIndex)
        {
            string directory = Path.GetDirectoryName(_path);
            string indexFileName = $"{tableName}_{colName}.idx";
            string indexPath = Path.Combine(directory, indexFileName);

            try
            {
                using (FileStream fs = new FileStream(indexPath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
                {
                    writer.Write("HASHIDX"); 
                    writer.Write(hashIndex.Name);
                    writer.Write(tableName);
                    writer.Write(colName);
                    writer.Write(hashIndex.TableSize);

                    for (int i = 0; i < hashIndex.TableSize; i++)
                    {
                        var currentBucket = hashIndex.GetBucket(i);

                        while (currentBucket != null)
                        {
                            writer.Write((byte)1); 
                            writer.Write(currentBucket.Key);

                            int offsetCount = currentBucket.Offsets.Count();
                            writer.Write(offsetCount);

                            var offsetNode = currentBucket.Offsets.Head;
                            while (offsetNode != null)
                            {
                                writer.Write(offsetNode.Value);
                                offsetNode = offsetNode.Next;
                            }
                            currentBucket = currentBucket.Next;
                        }

                        writer.Write((byte)0); 
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write index file {indexFileName}: {ex.Message}");
            }
        }


        private HashIndex LoadIndexFromFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs, System.Text.Encoding.UTF8))
            {
                string signature = reader.ReadString();
                if (signature != "HASHIDX")
                    throw new Exception("Not a valid index file.");

                string indexName = reader.ReadString();
                string tableName = reader.ReadString();
                string colName = reader.ReadString();
                int tableSize = reader.ReadInt32();

                HashIndex hashIndex = new HashIndex(tableSize);
                hashIndex.Name = indexName;

                for (int i = 0; i < tableSize; i++)
                {
                    byte marker = reader.ReadByte();

                    while (marker == 1) 
                    {
                        string key = reader.ReadString();
                        int offsetCount = reader.ReadInt32();

                        for (int j = 0; j < offsetCount; j++)
                        {
                            long offset = reader.ReadInt64();
                            hashIndex.Add(key, offset);
                        }
                        marker = reader.ReadByte();
                    }
                }
                hashIndex.TableName = tableName;
                hashIndex.ColumnName = colName;
                return hashIndex;
            }
        }

        public HashIndex GetIndexForColumn(string tableName, string colName)
        {
            string directory = Path.GetDirectoryName(_path);
            string indexFileName = $"{tableName}_{colName}.idx";
            string indexPath = Path.Combine(directory, indexFileName);

            if (File.Exists(indexPath))
            {
                try
                {
                    HashIndex loadedIdx = LoadIndexFromFile(indexPath);

                    loadedIdx.TableName = tableName;
                    loadedIdx.ColumnName = colName;

    
                    return loadedIdx;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading index from file: {ex.Message}");
                    return null;
                }
            }
            return null;
        }

        public bool DropIndex(HashIndex index)
        {
            string directory = Path.GetDirectoryName(_path);
            string indexFileName = $"{index.TableName}_{index.ColumnName}.idx";
            string indexPath = Path.Combine(directory, indexFileName);

            try
            {
                if (File.Exists(indexPath))
                {
                    File.Delete(indexPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting index file: {ex.Message}");
                return false;
            }
            return false;
        }

   public HashIndex FindIndexByName(string indexName)
{

    string directory = Path.GetDirectoryName(_path);
    

    string[] files = Directory.GetFiles(directory, "*.idx");

    foreach (var file in files)
    {
        HashIndex tempIdx = LoadIndexFromFile(file); 

        if (tempIdx != null && tempIdx.Name == indexName)
        {
 
            return tempIdx;
        }
    }

    return null;
}

    }

}

