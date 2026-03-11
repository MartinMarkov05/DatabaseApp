using System;
namespace DataBase.Utils
{
    public class HashIndex
    {
        public class Bucket
        {
            public string Key;
            public Utils.LinkedList<long> Offsets;
            public Bucket Next;


        }

        public string TableName { get; set; }
        public string ColumnName { get; set; }
        private Bucket[] table;
        private int size;
        public int TableSize => size;
        public string Name { get; set; }



        public HashIndex(int size)
        {
            this.size = size;
            table = new Bucket[size];
        }

        public Bucket GetBucket(int index)
        {
            return table[index];
        }


        private int Hash(string key)
        {
            int hash = 0;
            for (int i = 0; i < key.Length; i++)
            {
                hash = (hash * 31 + key[i]) % size;
            }
            return hash;
        }

        public void Add(string key, long recordOffset)
        {
            int h = Hash(key);
            Bucket current = table[h];

            while (current != null)
            {
                if (current.Key == key)
                {
                    current.Offsets.Add(recordOffset);
                    return;
                }
                current = current.Next;
            }

            Bucket newBucket = new Bucket();
            newBucket.Key = key;
            newBucket.Offsets = new Utils.LinkedList<long>();
            newBucket.Offsets.Add(recordOffset);
            newBucket.Next = table[h];
            table[h] = newBucket;
        }

        public Utils.LinkedList<long> Find(string key)
        {
            int h = Hash(key);
            Bucket current = table[h];
            while (current != null)
            {
                if (current.Key == key)
                    return current.Offsets;
                current = current.Next;
            }
            return null;
        }
    }

}

