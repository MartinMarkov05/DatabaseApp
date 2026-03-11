
namespace DataBase.Utils
{
    public static class TextUtilities
    {
        public static string[] SplitByString(string text, string separator)
        {
            string[] result = new string[200];
            int count = 0;
            string current = "";
            int sepIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == separator[sepIndex])
                {
                    sepIndex++;


                    if (sepIndex == separator.Length)
                    {

                        if (current != "")
                        {
                            result[count++] = current.MyManualTrim();
                            current = "";
                        }

                        sepIndex = 0;
                    }
                }
                else
                {

                    if (sepIndex > 0)
                    {
                        for (int j = 0; j < sepIndex; j++)
                            current += separator[j];
                        sepIndex = 0;
                    }

                    current += c;
                }
            }


            if (current != "")
                result[count++] = current.MyManualTrim();

            string[] final = new string[count];
            for (int i = 0; i < count; i++)
                final[i] = result[i];

            return final;
        }

       
       public static bool MyIsNullOrWhiteSpace(string input)
{
    if (input == null || input.Length == 0)
        return true;

    for (int i = 0; i < input.Length; i++)
    {
        char c = input[i];
        if (c != ' ' && c != '\t' && c != '\n' && c != '\r' && 
            c != '\f' && c != '\v' && c != '\0')
        {
            return false; 
        }
    }

    return true;
}


    public static string MyManualTrim(this string input)
    {
        if (input == null) return null;


    int start = 0;
    while (start < input.Length && (input[start] == ' ' || input[start] == '\0' || input[start] == '\'' || input[start] == '\"'))
    {
        start++;
    }

    int end = input.Length - 1;
    while (end >= start && (input[end] == ' ' || input[end] == '\0' || input[end] == '\'' || input[end] == '\"'))
    {
        end--;
    }


    int targetLength = end - start + 1;
    if (targetLength <= 0) return "";


    char[] resultChars = new char[targetLength];
    for (int i = 0; i < targetLength; i++)
    {
        resultChars[i] = input[start + i];
    }

    return new string(resultChars);
    }


        public static string MyToUpper(this string input)
        {
            if (input == null)
                return null;

            char[] result = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c >= 'a' && c <= 'z')
                    result[i] = (char)(c - 32);
                else
                    result[i] = c;
            }

            return new string(result);
        }

        public static string MyTrim(this string value, char trimChar)
        {
            if (value == null) return null;

            int start = 0;
            int end = value.Length - 1;

            while (start <= end && value[start] == trimChar)
                start++;

            while (end >= start && value[end] == trimChar)
                end--;

            if (start > end)
                return "";

            char[] result = new char[end - start + 1];
            int index = 0;

            for (int i = start; i <= end; i++)
            {
                result[index++] = value[i];
            }

            return new string(result);
        }

        public static string FormatValue(object value, string type)
        {
            if (value == null) return "NULL";

            switch (type.ToLower())
            {
                case "int":
                    return $"{((int)value)}"; 

                case "double":
                    return $"{((double)value)}"; 

                case "date":
                    if (value is DateTime dt)
                    {
                        string day = dt.Day < 10 ? "0" + dt.Day : $"{dt.Day}" ;
                        string month = dt.Month < 10 ? "0" + dt.Month : $"{dt.Month}";
                        return day + "." + month + "." + dt.Year;
                    }
                    return "00.00.0000";

                case "string":
                    return (string)value;

                default:
                    return "";

            }

        }
    }
}


