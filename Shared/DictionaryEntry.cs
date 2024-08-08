using System;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Globalization;


public class DictionaryEntry : IComparable
{
    public enum GenderEnum
    { 
        [EnumMember(Value = "none")]
        none,
        [EnumMember(Value = "der")]
        der,
        [EnumMember(Value = "die")]
        die,
        [EnumMember(Value = "das")]
        das
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GenderEnum Gender { get; set; }

    public string Word { get; set; }
    
    [JsonConstructor]
    public DictionaryEntry(GenderEnum Gender, string Word)
    {
        this.Gender = Gender;
        this.Word = Word;
    }

    public int CompareTo(object? obj)
    {
        if (obj is not null)
        {
            DictionaryEntry entry = (DictionaryEntry)obj;
            //int value = Reversed().CompareTo(entry.Reversed());
            int value = String.Compare(Reversed(), entry.Reversed(), true, new CultureInfo("de-DE"));
            return value;
        }
        else return 0;
    }
    public override string ToString()
    {
        return Word;
    }
    public string Reversed()
    {
        char[] charArray = Word.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
}