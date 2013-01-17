namespace SendGridMail
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.Serialization.Json;
	using System.Text;

	public class Utils
	{
		#region Public Methods and Operators

		public static string Serialize<T>(T obj)
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
			using (MemoryStream stream = new MemoryStream())
			{
				serializer.WriteObject(stream, obj);
				string jsonData = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Length);
				return jsonData;
			}
		}

		public static string SerializeDictionary(IDictionary<string, string> dic)
		{
			return "{" + string.Join(",", dic.Select(kvp => Serialize(kvp.Key) + ":" + Serialize(kvp.Value))) + "}";
		}

		#endregion
	}
}