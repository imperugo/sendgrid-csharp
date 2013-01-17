namespace SendGridMail
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Mail;

	public class Header : IHeader
	{
		#region Constants

		private const string SendgridHeader = "X-Smtpapi";

		#endregion

		#region Fields

		private readonly HeaderSettingsNode settings;

		#endregion

		#region Constructors and Destructors

		public Header()
		{
			this.settings = new HeaderSettingsNode();
		}

		#endregion

		#region Public Properties

		public IEnumerable<string> To
		{
			get
			{
				return this.settings.GetArray("to");
			}
		}

		#endregion

		#region Public Methods and Operators

		public void AddFilterSetting(string filter, IEnumerable<string> settings, string value)
		{
			List<string> keys = new List<string> { "filters", filter, "settings" }.Concat(settings).ToList();
			this.settings.AddSetting(keys, value);
		}

		public void AddHeader(MailMessage mime)
		{
			mime.Headers.Add(SendgridHeader, this.AsJson());
		}

		public void AddSubVal(string tag, IEnumerable<string> substitutions)
		{
			List<string> keys = new List<string> { "sub", tag };
			this.settings.AddArray(keys, substitutions);
		}

		public void AddTo(IEnumerable<string> addresses)
		{
			this.settings.AddArray(new List<string> { "to" }, addresses);
		}

		public void AddUniqueIdentifier(IDictionary<string, string> identifiers)
		{
			foreach (string key in identifiers.Keys)
			{
				List<string> keys = new List<string> { "unique_args", key };
				string value = identifiers[key];
				this.settings.AddSetting(keys, value);
			}
		}

		public string AsJson()
		{
			if (this.settings.IsEmpty())
			{
				return string.Empty;
			}

			return this.settings.ToJson();
		}

		public void Disable(string filter)
		{
			this.AddFilterSetting(filter, new List<string> { "enable" }, "0");
		}

		public void Enable(string filter)
		{
			this.AddFilterSetting(filter, new List<string> { "enable" }, "1");
		}

		public void SetCategory(string category)
		{
			List<string> keys = new List<string> { "category" };
			this.settings.AddSetting(keys, category);
		}

		#endregion

		internal class HeaderSettingsNode
		{
			#region Fields

			private readonly Dictionary<string, HeaderSettingsNode> branches;

			private IEnumerable<string> array;

			private string leaf;

			#endregion

			#region Constructors and Destructors

			public HeaderSettingsNode()
			{
				this.branches = new Dictionary<string, HeaderSettingsNode>();
			}

			#endregion

			#region Public Methods and Operators

			public void AddArray(List<string> keys, IEnumerable<string> value)
			{
				if (keys.Count == 0)
				{
					this.array = value;
				}
				else
				{
					if (this.leaf != null || this.array != null)
					{
						throw new ArgumentException("Attempt to overwrite setting");
					}

					string key = keys.First();
					if (!this.branches.ContainsKey(key))
					{
						this.branches[key] = new HeaderSettingsNode();
					}

					List<string> remainingKeys = keys.Skip(1).ToList();
					this.branches[key].AddArray(remainingKeys, value);
				}
			}

			public void AddSetting(List<string> keys, string value)
			{
				if (keys.Count == 0)
				{
					this.leaf = value;
				}
				else
				{
					if (this.leaf != null || this.array != null)
					{
						throw new ArgumentException("Attempt to overwrite setting");
					}

					string key = keys.First();
					if (!this.branches.ContainsKey(key))
					{
						this.branches[key] = new HeaderSettingsNode();
					}

					List<string> remainingKeys = keys.Skip(1).ToList();
					this.branches[key].AddSetting(remainingKeys, value);
				}
			}

			public IEnumerable<string> GetArray(params string[] keys)
			{
				return this.GetArray(keys.ToList());
			}

			public IEnumerable<string> GetArray(List<string> keys)
			{
				if (keys.Count == 0)
				{
					return this.array;
				}

				string key = keys.First();
				if (!this.branches.ContainsKey(key))
				{
					throw new ArgumentException("Bad key path!");
				}

				List<string> remainingKeys = keys.Skip(1).ToList();
				return this.branches[key].GetArray(remainingKeys);
			}

			public string GetLeaf()
			{
				return this.leaf;
			}

			public string GetSetting(params string[] keys)
			{
				return this.GetSetting(keys.ToList());
			}

			public string GetSetting(List<string> keys)
			{
				if (keys.Count == 0)
				{
					return this.leaf;
				}

				string key = keys.First();
				if (!this.branches.ContainsKey(key))
				{
					throw new ArgumentException("Bad key path!");
				}

				List<string> remainingKeys = keys.Skip(1).ToList();
				return this.branches[key].GetSetting(remainingKeys);
			}

			public bool IsEmpty()
			{
				if (this.leaf != null)
				{
					return false;
				}

				return this.branches == null || this.branches.Keys.Count == 0;
			}

			public string ToJson()
			{
				if (this.branches.Count > 0)
				{
					return "{" + string.Join(",", this.branches.Keys.Select(k => Utils.Serialize(k) + " : " + this.branches[k].ToJson())) + "}";
				}

				if (this.leaf != null)
				{
					return Utils.Serialize(this.leaf);
				}

				if (this.array != null)
				{
					return "[" + string.Join(", ", this.array.Select(Utils.Serialize)) + "]";
				}

				return "{}";
			}

			#endregion
		}
	}
}