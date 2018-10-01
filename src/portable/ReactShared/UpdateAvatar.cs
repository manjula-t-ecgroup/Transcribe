﻿using System.Diagnostics;
using System.IO;
using System.Web;
using System.Xml;

namespace ReactShared
{
	public class UpdateAvatar
	{
		public delegate void SaveImage(string data, string file);

		public UpdateAvatar(string query, byte[] requestBody, SaveImage saveImage)
		{
			var parsedQuery = HttpUtility.ParseQueryString(query);
			var user = parsedQuery["user"];
			var avatarBase64 = Util.GetRequestElement(requestBody, "preview");
			Debug.Print($"{user}:{avatarBase64}");
			var imageFileName = user + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".png";
			var sourceFolder = Path.Combine(Util.DataFolder, "images");
			var sourceInfo = new DirectoryInfo(sourceFolder);
			sourceInfo.Create();
			saveImage(avatarBase64, Path.Combine(sourceFolder, imageFileName));
			var usersDoc = Util.LoadXmlData("users");
			var userNode = usersDoc.SelectSingleNode($"//user[username/@id = '{user}']");
			if (userNode == null)
				return;
			var usernameNode = userNode.SelectSingleNode("username") as XmlElement;
			Debug.Assert(usernameNode != null, nameof(usernameNode) + " != null");
			AddAvatarUri("images/" + imageFileName, usernameNode, usersDoc);
			using (var xw = XmlWriter.Create(Util.XmlFullName("users"), new XmlWriterSettings { Indent = true }))
			{
				usersDoc.Save(xw);
			}
		}

		private static void AddAvatarUri(string avatarUri, XmlElement usernameNode, XmlDocument usersDoc)
		{
			if (avatarUri == null)
				return;
			if (!(usernameNode.SelectSingleNode("avatarUri") is XmlElement avatarNode))
			{
				avatarNode = usersDoc.CreateElement("avatarUri");
				usernameNode.AppendChild(avatarNode);
			}
			avatarNode.InnerText = avatarUri;
		}

	}
}
