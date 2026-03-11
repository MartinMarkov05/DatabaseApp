using System;
namespace DataBase.Model
{
    public class Column
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }

        public Column(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public Column(string name, string type, string defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }
    }
}

