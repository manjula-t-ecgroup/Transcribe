﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;

namespace ReactShared
{
	public class Util
	{
		public static string Folder { get; set; }
		public static string DataFolder =
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		public static readonly List<string> SupportFile = new List<string>();
		private static readonly Regex TaskIdPattern = new Regex(@"(.*)v[0-9]{2}\.(mp3|wav)$", RegexOptions.Compiled);

		public static string ApiFolder()
		{
			var apiFolder = Path.Combine(Folder, "api");
			if (!Directory.Exists(apiFolder))
				Directory.CreateDirectory(apiFolder);
			return apiFolder;
		}

		public static string ToXmlTaskId(string taskId)
		{
			var match = TaskIdPattern.Match(taskId);
			return match.Success ? match.Groups[1].Value : taskId;
		}

		public static XmlDocument LoadXmlData(string name)
		{
			var fullName = XmlFullName(name);
			if (!File.Exists(fullName))
				GetResource.DefaultData(name);
			var xDoc = new XmlDocument();
			using (var xr = XmlReader.Create(fullName))
			{
				xDoc.Load(xr);
			}

			return xDoc;
		}

		public static string XmlFullName(string name)
		{
			return Path.Combine(DataFolder, name + ".xml");
		}

		public static void NewAttr(XmlNode node, string name, string val)
		{
			Debug.Assert(node.OwnerDocument != null, "node.OwnerDocument != null");
			var idAttr = node.OwnerDocument.CreateAttribute(name);
			idAttr.InnerText = val;
			node.Attributes.Append(idAttr);
		}

		public static void AsArray(XmlNodeList nodes)
		{
			if (nodes.Count != 1)
				return;
			var node = nodes[0];
			Debug.Assert(node.OwnerDocument != null, "node.OwnerDocument != null");
			var jsonConvertAttr =
				node.OwnerDocument.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
			jsonConvertAttr.InnerText = "true";
			Debug.Assert(node.Attributes != null, "node.Attributes != null");
			node.Attributes.Append(jsonConvertAttr);
		}

		public static XmlNode UserNode(string user)
		{
			var usersDoc = !string.IsNullOrEmpty(user) ? LoadXmlData("users") : null;
			var userNode = usersDoc?.SelectSingleNode($"//*[local-name() = 'user' and username/@id='{user}']");
			return userNode;
		}

		public static XmlNode GetUserProjectNode(XmlNode userNode, string project, XmlDocument usersDoc, string user)
		{
			var userProjectNode = userNode.SelectSingleNode($"project[@id = '{project}']");
			if (userProjectNode == null)
			{
				userProjectNode = usersDoc.CreateElement("project");
				NewAttr(userProjectNode, "id", project);
				var roleNodes = userNode.SelectNodes("role");
				Debug.Assert(roleNodes?.Count > 0, $"user {user} missing role");
				userNode.InsertAfter(userProjectNode, roleNodes[roleNodes.Count - 1]);
			}

			return userProjectNode;
		}

		public static XmlElement FindPreceding(XmlNode userNode, List<string> list)
		{
			var nodes = userNode.SelectNodes(list[0]);
			Debug.Assert(nodes != null, nameof(nodes) + " != null");
			if (nodes.Count > 0)
				return nodes[nodes.Count - 1] as XmlElement;
			if (list.Count > 1)
				return FindPreceding(userNode, list.GetRange(1, list.Count - 1));
			return null;
		}

		public static string GetRequestElement(byte[] data, string tag)
		{
			var text = string.Empty;
			using (var ms = new MemoryStream(data))
			{
				using (var str = new StreamReader(ms))
				{
					try
					{
						var xml = JsonConvert.DeserializeXmlNode(@"{""state"":" + str.ReadToEnd() + "}");
						text = xml.SelectSingleNode($@"//{tag}").InnerText;
					}
					catch (Exception err)
					{
						Debug.Print(err.Message);
					}
				}
			}

			return text;
		}

		public static void DeleteFolder(string fullPath)
		{
			var folder = Path.GetDirectoryName(fullPath);
			try
			{
				Directory.Delete(folder);
				DeleteFolder(folder);
			}
			catch
			{
				// if not empty, ignore delete directory
			}
		}

	}
}