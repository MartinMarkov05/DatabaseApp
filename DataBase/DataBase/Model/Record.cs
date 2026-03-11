using System;
namespace DataBase.Model
{
	public class Record
	{
		public int Id { get; set; }

		public string[] Values { get; set; }

		public long Offset { get; set; }

		public Record(string[] columnValues, long offset)
		{
			Offset = offset;
			Values = columnValues;
			Id = Convert.ToInt32(columnValues[0]);
		}
	}
}

